using System;
using System.Collections.Generic;
using System.Threading;
using Serilog;
using Zen.Trunk.CoordinationDataStructures;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// Implements a lock handler object.
    /// </summary>
    /// <typeparam name="TLockClass"></typeparam>
    /// <typeparam name="TLockTypeEnum"></typeparam>
    /// <remarks>
    /// <para>
    /// The lock handler tracks active locks by their lock key string.
    /// When locks are freed they are added to a free-lock pool to increase
    /// performance when new locks are requested. Currently the size of the
    /// free-lock pool is fixed.
    /// </para>
    /// <para>
    /// Methods to be added to allow free-pool to be managed by LockManager.
    /// </para>
    /// </remarks>
    internal class LockHandler<TLockClass, TLockTypeEnum> : ILockHandler
        where TLockTypeEnum : struct, IComparable, IConvertible, IFormattable // enum
        where TLockClass : TransactionLock<TLockTypeEnum>, ITransactionLock<TLockTypeEnum>, new()
    {
        #region Private Fields
        private static readonly ILogger Logger = Serilog.Log.ForContext<LockHandler<TLockClass, TLockTypeEnum>>();

        private int _maxFreeLocks;
        private readonly SpinLockClass _syncLocks = new SpinLockClass();
        private readonly Dictionary<string, TLockClass> _activeLocks = new Dictionary<string, TLockClass>();
        private readonly ObjectPool<TLockClass> _freeLocks;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LockHandler&lt;TLockClass, TLockTypeEnum&gt;"/> class.
        /// </summary>
        public LockHandler()
            : this(100)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockHandler&lt;TLockClass, TLockTypeEnum&gt;"/> class.
        /// </summary>
        public LockHandler(int maxFreeLocks)
        {
            _maxFreeLocks = maxFreeLocks;
            _freeLocks = new ObjectPool<TLockClass>(CreateLock);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets a lock object represented by the key.
        /// </summary>
        /// <param name="lockKey"></param>
        /// <returns>Lock object associated with key.</returns>
        /// <remarks>
        /// The lock object is add ref'ed before it is returned to ensure
        /// stability of lock manager.
        /// </remarks>
        public TLockClass GetOrCreateLock(string lockKey)
        {
            // Sanity check
            if (string.IsNullOrEmpty(lockKey))
            {
                throw new ArgumentNullException(nameof(lockKey));
            }

            // Lookup/create page lock
            TLockClass lockObject = null;
            _syncLocks.Execute(
                () =>
                {
                    // Attempt to obtain the lock with the specified identifier
                    //  from our active lock cache
                    if (!_activeLocks.TryGetValue(lockKey, out lockObject))
                    {
                        // Get lock from free lock pool and update identifier
                        lockObject = _freeLocks.GetObject();
                        lockObject.Id = lockKey;

                        // Add lock to the active lock cache
                        _activeLocks.Add(lockKey, lockObject);
                    }

                    // Update lock reference count inside the sync zone
                    lockObject.AddRefLock();
                });
            return lockObject;
        }
        #endregion

        #region Private Methods
        private TLockClass CreateLock()
        {
            var lockObject = new TLockClass();
            lockObject.Initialise();
            lockObject.FinalRelease += Lock_FinalRelease;
            return lockObject;
        }

        private void Lock_FinalRelease(object sender, EventArgs e)
        {
            var lockObject = (TLockClass)sender;
            _syncLocks.Execute(
                () =>
                {
                    Logger.Debug(
                        "Lock final release: {LockId}",
                        lockObject.Id);

                    if (!string.IsNullOrEmpty(lockObject.Id))
                    {
                        _activeLocks.Remove(lockObject.Id);
                        lockObject.Id = string.Empty;
                        if (_freeLocks.Count < _maxFreeLocks)
                        {
                            _freeLocks.PutObject(lockObject);
                        }
                    }
                });
        }
        #endregion

        #region ILockHandler Members
        int ILockHandler.MaxFreeLocks
        {
            get => _maxFreeLocks;
            set => Interlocked.Exchange(ref _maxFreeLocks, value);
        }

        int ILockHandler.ActiveLockCount => _activeLocks.Count;

        int ILockHandler.FreeLockCount => _freeLocks.Count;

        void ILockHandler.PopulateFreeLockPool(int maxLocks)
        {
            _syncLocks.Execute(
                () =>
                {
                    while (_freeLocks.Count < _maxFreeLocks && maxLocks > 0)
                    {
                        _freeLocks.PutObject(CreateLock());
                        --maxLocks;
                    }
                });
        }
        #endregion
    }
}
