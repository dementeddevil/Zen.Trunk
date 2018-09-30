using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Zen.Trunk.Extensions;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// Generic transaction-based lock implementation
    /// </summary>
    /// <typeparam name="TLockTypeEnum"></typeparam>
    public abstract class TransactionLock<TLockTypeEnum> :
        TransactionLockBase, ITransactionLock<TLockTypeEnum>
        where TLockTypeEnum : struct, IComparable, IConvertible, IFormattable // enum
    {
        #region Lock Messages
        /// <summary>
        /// <c>LockRequestBase</c> defines the base class for all lock request messages.
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.TransactionLockBase" />
        /// <seealso cref="Zen.Trunk.Storage.Locking.IReferenceLock" />
        protected class LockRequestBase : TaskCompletionSource<bool>
        {
            #region Internal Constructors
            internal LockRequestBase(TLockTypeEnum lockType, LockOwnerIdentity lockOwner)
            {
                Lock = lockType;
                LockOwner = lockOwner;
            }
            #endregion

            #region Internal Properties
            internal TLockTypeEnum Lock { get; set; }

            internal LockOwnerIdentity LockOwner { get; }
            #endregion
        }

        /// <summary>
        /// <c>AcquireLock</c> is a message sent when acquiring a lock.
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.TransactionLockBase" />
        /// <seealso cref="Zen.Trunk.Storage.Locking.IReferenceLock" />
        protected class AcquireLock : LockRequestBase
        {
            #region Internal Constructors
            internal AcquireLock(TLockTypeEnum lockType, LockOwnerIdentity lockOwner)
                : base(lockType, lockOwner)
            {
            }
            #endregion
        }

        /// <summary>
        /// <c>ReleaseLock</c> is a message sent when releasing a lock.
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.TransactionLockBase" />
        /// <seealso cref="Zen.Trunk.Storage.Locking.IReferenceLock" />
        protected class ReleaseLock : LockRequestBase
        {
            #region Internal Constructors
            internal ReleaseLock(TLockTypeEnum lockType, LockOwnerIdentity lockOwner)
                : base(lockType, lockOwner)
            {
            }
            #endregion
        }

        /// <summary>
        /// <c>QueryLock</c> is a message sent when querying lock state.
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.TransactionLockBase" />
        /// <seealso cref="Zen.Trunk.Storage.Locking.IReferenceLock" />
        protected class QueryLock : LockRequestBase
        {
            #region Internal Constructors
            internal QueryLock(TLockTypeEnum lockType, LockOwnerIdentity lockOwner)
                : base(lockType, lockOwner)
            {
            }
            #endregion
        }
        #endregion

        #region Lock State
        /// <summary>
        /// <c>State</c> defines the base lock object state.
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.TransactionLockBase" />
        /// <seealso cref="Zen.Trunk.Storage.Locking.IReferenceLock" />
        protected abstract class State
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>The lock.</value>
            public abstract TLockTypeEnum Lock
            {
                get;
            }

            /// <summary>
            /// Gets a boolean value indicating whether an exclusive lock can
            /// be acquired from this state.
            /// </summary>
            /// <value>
            /// <c>true</c> if an exclusive lock can be acquired from this
            /// state; otherwise, <c>false</c>. The default is <c>false</c>.
            /// </value>
            public virtual bool CanEnterExclusiveLock => false;

            /// <summary>
            /// Gets an array of lock types that this state allows.
            /// </summary>
            /// <value>The compatable locks.</value>
            protected abstract TLockTypeEnum[] AllowedLockTypes
            {
                get;
            }

            /// <summary>
            /// Determines whether the specified lock type is equivalent to an
            /// exclusive lock.
            /// </summary>
            /// <param name="lockType">Type of the lock.</param>
            /// <returns>
            /// <c>true</c> if the lock type is an exclusive lock; otherwise,
            /// <c>false</c>.
            /// </returns>
            public abstract bool IsExclusiveLock(TLockTypeEnum lockType);

            /// <summary>
            /// Called when this state is entered.
            /// </summary>
            /// <param name="owner">The owner.</param>
            /// <param name="oldState">The old state.</param>
            public virtual void OnEnterState(TransactionLock<TLockTypeEnum> owner, State oldState)
            {
            }

            /// <summary>
            /// Called when this state is exited.
            /// </summary>
            /// <param name="owner">The owner.</param>
            /// <param name="newState">The new state.</param>
            public virtual void OnExitState(TransactionLock<TLockTypeEnum> owner, State newState)
            {
            }

            /// <summary>
            /// Determines whether the specified request can acquire the lock.
            /// </summary>
            /// <param name="owner">The owner.</param>
            /// <param name="request">The request.</param>
            /// <returns>
            /// <c>true</c> if the request can acquire lock; otherwise,
            /// <c>false</c>.
            /// </returns>
            public virtual bool CanAcquireLock(TransactionLock<TLockTypeEnum> owner, AcquireLock request)
            {
                // We can acquire the lock if any of the following are true;
                //	1. Active request count is zero
                //	2. Active request count is one, the transaction id matches
                //		the request and the requested lock is different to that
                //		currently held.
                //	3. The requested lock is compatable and there are no other
                //		pending requests.
                //	4. The requested lock is an exclusive lock, we can enter an
                //		exclusive lock, the this transaction id is the only
                //		active transaction on the lock
                if ((owner._activeRequests.Count == 0) ||
                    /*(owner._activeRequests.Count == 1 &&
					owner._activeRequests.ContainsKey(request.TransactionId) &&
					!owner.IsEquivalentLock(owner._activeRequests[request.TransactionId].Lock, request.Lock)) ||*/
                    (owner._pendingRequests.Count == 0 && CanAcquireLock(request)) ||
                    (owner._activeRequests.Count == 1 && owner._activeRequests.ContainsKey(request.LockOwner) && CanEnterExclusiveLock && IsExclusiveLock(request.Lock)))
                {
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Determines whether the lock type specified in the request is
            /// allowed by this lock state instance.
            /// </summary>
            /// <param name="request">The request.</param>
            /// <returns>
            /// <c>true</c> if the specified request is allowed; otherwise,
            /// <c>false</c>.
            /// </returns>
            private bool CanAcquireLock(AcquireLock request)
            {
                if (AllowedLockTypes.Length == 0)
                {
                    return false;
                }

                return AllowedLockTypes.Any(lockType => Convert.ToInt32(lockType) == Convert.ToInt32(request.Lock));
            }
        }
        #endregion

        #region Private Fields
        private readonly ActionBlock<AcquireLock> _acquireLockAction;
        private readonly ActionBlock<ReleaseLock> _releaseLockAction;
        private readonly ActionBlock<QueryLock> _queryLockAction;
        private readonly Dictionary<LockOwnerIdentity, AcquireLock> _activeRequests = new Dictionary<LockOwnerIdentity, AcquireLock>();
        private readonly Queue<AcquireLock> _pendingRequests = new Queue<AcquireLock>();
        private int _referenceCount;
        private bool _initialised;
        private AcquireLock _pendingExclusiveRequest;
        private State _currentState;
        #endregion

        #region Public Events
        /// <summary>
        /// Fired prior to the final disposal of the transaction lock.
        /// </summary>
        public event EventHandler FinalRelease;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionLock{TLockTypeEnum}"/> class.
        /// </summary>
        protected TransactionLock()
        {
            // Initialise receiver arbiters
            var taskInterleave = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default);
            _acquireLockAction = new ActionBlock<AcquireLock>(
                request =>
                {
                    AcquireLockRequestHandler(request);
                },
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            _releaseLockAction = new ActionBlock<ReleaseLock>(
                request =>
                {
                    ReleaseLockRequestHandler(request);
                },
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            _queryLockAction = new ActionBlock<QueryLock>(
                request =>
                {
                    QueryLockRequestHandler(request);
                },
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler
                });
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets/sets the lock Id.
        /// </summary>
        public string Id { get; set; }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets enumeration value for the lock representing the "none" lock.
        /// </summary>
        /// <value>The type of the none lock.</value>
        protected abstract TLockTypeEnum NoneLockType { get; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialises the lock object.
        /// </summary>
        public void Initialise()
        {
            if (_initialised)
            {
                throw new InvalidOperationException("Lock already initialised.");
            }

            // Setup initial state object
            _currentState = GetStateFromType(NoneLockType);
            _currentState.OnEnterState(this, null);
            _initialised = true;
            TraceVerbose("Initialized");
        }

        /// <summary>
        /// Increases the reference count on this lock object.
        /// </summary>
        public void AddRefLock()
        {
            Interlocked.Increment(ref _referenceCount);
        }

        /// <summary>
        /// Decreases the reference count on this lock object.
        /// </summary>
        /// <returns>Returns true if this is the final release.</returns>
        public void ReleaseRefLock()
        {
            if (Interlocked.Decrement(ref _referenceCount) == 0)
            {
                OnFinalRelease();
            }
        }

        /// <summary>
        /// Determines whether the current transaction has the given lock type.
        /// </summary>
        /// <param name="lockType"></param>
        /// <returns>Boolean. True indicates lock is held.</returns>
        /// <remarks>
        /// This method will throw if the current thread does not have a
        /// transaction context.
        /// </remarks>
        public Task<bool> HasLockAsync(TLockTypeEnum lockType)
        {
            // Retrieve connection id and create request object.
            // Will throw if no connection information is available for the
            //  calling thread.
            var lockOwnerIdent = GetThreadLockOwnerIdentity(true);
            return HasLockAsync(lockOwnerIdent, lockType);
        }

        /// <summary>
        /// Acquires the specified lock type.
        /// </summary>
        /// <param name="lockType"></param>
        /// <param name="timeout"></param>
        /// <remarks>
        /// <para>
        /// This method will throw if the current thread does not have a
        /// transaction context.
        /// </para>
        /// <para>
        /// This method will throw if the lock is not acquired within the
        /// specified timeout period.
        /// </para>
        /// </remarks>
        public Task LockAsync(TLockTypeEnum lockType, TimeSpan timeout)
        {
            // Retrieve connection id and create request object.
            // Will throw if no connection information is available for the
            //  calling thread.
            var lockOwner = GetThreadLockOwnerIdentity(true);
            return LockAsync(lockOwner, lockType, timeout);
        }

        /// <summary>
        /// Releases the lock by downgrading to the none lock state
        /// </summary>
        public Task UnlockAsync()
        {
            return UnlockAsync(NoneLockType);
        }

        /// <summary>
        /// Releases the lock by downgrading to the given lock type.
        /// </summary>
        /// <param name="newLockType"></param>
        /// <remarks>
        /// The new lock type should be of a lower state than the
        /// current lock type or this method may throw an exception.
        /// The lock timeout period is fixed at 10 seconds so get it
        /// right!
        /// </remarks>
        public Task UnlockAsync(TLockTypeEnum newLockType)
        {
            // Retrieve connection id and create request object.
            // Will throw if no connection information is available for the
            //  calling thread.
            var lockOwner = GetThreadLockOwnerIdentity(true);
            return UnlockAsync(lockOwner, newLockType);
        }
        #endregion

        #region Internal Methods
        internal async Task LockAsync(LockOwnerIdentity lockOwner, TLockTypeEnum lockType, TimeSpan timeout)
        {
            // Post lock acquisition message
            var request = new AcquireLock(lockType, lockOwner);
            _acquireLockAction.Post(request);

            // Wait for task to complete
            bool addRefLock;
            try
            {
                addRefLock = await request.Task.WithTimeout(timeout).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Lock timeout occurred.");
            }

            // Increment reference count on lock
            if (addRefLock)
            {
                AddRefLock();
            }
        }

        internal async Task UnlockAsync(LockOwnerIdentity lockOwner, TLockTypeEnum newLockType)
        {
            // Post lock acquisition message
            var request = new ReleaseLock(newLockType, lockOwner);
            _releaseLockAction.Post(request);

            // Wait for task to complete
            var releaseLock = await request.Task.ConfigureAwait(false);

            // Release reference count on lock
            if (releaseLock)
            {
                ReleaseRefLock();
            }
        }
        #endregion

        #region Protected Methods
#if TRACE
        /// <summary>
        /// Gets the trace prefix.
        /// </summary>
        /// <returns></returns>
        protected override string GetTracePrefix()
        {
            return $"{base.GetTracePrefix()} ID: {Id} Rc: {_referenceCount} Txn:{GetThreadLockOwnerIdentity(false)}";
        }
#endif

        /// <summary>
        /// Determines whether the transaction associated with the specified
        /// transaction identifier has given lock.
        /// </summary>
        /// <param name="lockOwner">The lock owner identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <returns>
        ///   <c>true</c> if the specified transaction identifier has lock; otherwise, <c>false</c>.
        /// </returns>
        protected internal Task<bool> HasLockAsync(LockOwnerIdentity lockOwner, TLockTypeEnum lockType)
        {
            // Post lock acquisition message
            var request = new QueryLock(lockType, lockOwner);
            _queryLockAction.Post(request);
            return request.Task;
        }

        /// <summary>
        /// When overridden by derived class, gets the state object from
        /// the specified state type.
        /// </summary>
        /// <param name="lockType">Type of the lock.</param>
        /// <returns></returns>
        protected abstract State GetStateFromType(TLockTypeEnum lockType);

        /// <summary>
        /// Called when last reference to the lock is released.
        /// </summary>
        protected virtual void OnFinalRelease()
        {
            FinalRelease?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Called to determine whether the lock can be acquired by the
        /// specified <see cref="T:AcquireLock"/> request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        protected bool CanAcquireLock(AcquireLock request)
        {
            return _currentState.CanAcquireLock(this, request);
        }

        /// <summary>
        /// Sets the active request.
        /// </summary>
        /// <param name="request">The request.</param>
        protected void SetActiveRequest(AcquireLock request)
        {
            var activeLockOwner = GetActiveLockOwner(request.LockOwner);
            var addRefLock = false;
            if (activeLockOwner != null)
            {
                _activeRequests[activeLockOwner.Value] = request;
            }
            else
            {
                _activeRequests.Add(request.LockOwner, request);
                addRefLock = true;
            }
            if (!request.TrySetResult(addRefLock))
            {
                _activeRequests.Remove(request.LockOwner);
            }
        }
        #endregion

        #region Private Methods
        private void AcquireLockRequestHandler(AcquireLock request)
        {
            TraceVerbose("AcquireLock - Enter");

            var activeLockOwner = GetActiveLockOwner(request.LockOwner);
            if (!CanAcquireLock(request))
            {
                if (_currentState.IsExclusiveLock(request.Lock))
                {
                    // If we cannot enter exclusive lock from this state
                    //	or
                    // If the active request list does not contain this transaction
                    //	or
                    // If the active request for this transaction does not match the
                    //	current state lock
                    // then we cannot enter exclusive state from this transaction
                    if (!_currentState.CanEnterExclusiveLock ||
                        activeLockOwner == null ||
                        !IsEquivalentLock(_activeRequests[activeLockOwner.Value].Lock, _currentState.Lock))
                    {
                        request.TrySetException(new LockException(
                            "Cannot enter exclusive lock from current lock state."));
                        return;
                    }

                    // Save the request (we will try to enter exclusive mode
                    //	all other requests have been released)
                    System.Diagnostics.Debug.Assert(_pendingExclusiveRequest == null);
                    _pendingExclusiveRequest = request;
                }
                else
                {
                    // Ignore requests for downgraded lock here
                    if (activeLockOwner != null &&
                        _activeRequests.ContainsKey(activeLockOwner.Value) &&
                        IsDowngradedLock(_activeRequests[activeLockOwner.Value].Lock, request.Lock))
                    {
                        // Simply mark the request as satisfied but to not
                        //	add to the list of active requests
                        request.TrySetResult(false);
                        return;
                    }

                    // We can upgrade the lock only if there are no pending
                    //	requests
                    if (_activeRequests.Count == 1 &&
                        activeLockOwner != null &&
                        _pendingRequests.Count == 0 &&
                        _pendingExclusiveRequest == null)
                    {
                        // Replace the active request and return
                        _activeRequests[activeLockOwner.Value] = request;
                        request.TrySetResult(false);
                        return;
                    }

                    // Any lock acquisition request not satisfied by
                    //	the current state object must be queued.
                    _pendingRequests.Enqueue(request);
                }
            }
            else
            {
                try
                {
                    // Acquire the lock
                    var result = false;
                    if (activeLockOwner != null)
                    {
                        _activeRequests[activeLockOwner.Value] = request;
                    }
                    else
                    {
                        _activeRequests.Add(request.LockOwner, request);
                        result = true;
                    }

                    // Lock acquired.
                    request.TrySetResult(result);
                }
                finally
                {
                    UpdateActiveLockState();
                }
            }
        }

        private void ReleaseLockRequestHandler(ReleaseLock request)
        {
            TraceVerbose("ReleaseLock - Enter");

            try
            {
                var releaseLock = false;

                var activeLockOwner = GetActiveLockOwner(request.LockOwner);
                if (activeLockOwner == null)
                {
                    //throw new InvalidOperationException("Lock not held by transaction.");
                    TraceVerbose("Release lock called when lock not held.");
                }
                else
                {
                    if (IsEquivalentLock(request.Lock, NoneLockType))
                    {
                        _activeRequests.Remove(activeLockOwner.Value);
                        if (_activeRequests.Count == 0)
                        {
                            UpdateActiveLockState();
                        }

                        releaseLock = true;
                    }
                    else if (!IsDowngradedLock(_activeRequests[activeLockOwner.Value].Lock, request.Lock))
                    {
                        throw new InvalidOperationException(
                            "New lock type is not downgrade of current lock.");
                    }
                    else
                    {
                        _activeRequests[activeLockOwner.Value].Lock = request.Lock;
                    }
                }

                request.TrySetResult(releaseLock);
            }
            catch (Exception e)
            {
                request.TrySetException(e);
            }
            finally
            {
                ReleaseWaitingRequests();
            }
        }

        private void QueryLockRequestHandler(QueryLock request)
        {
            if (GetActiveLockOwner(request.LockOwner) == null)
            {
                // Current transaction is not in active list therefore
                //	lock is not held
                request.TrySetResult(false);
            }
            else if (IsEquivalentLock(request.Lock, NoneLockType))
            {
                // Using the NoneLockType means check if we have any kind
                //	of lock; since transaction id is in the active list
                //	this must be the case...
                request.TrySetResult(true);
            }
            else if (IsEquivalentLock(_currentState.Lock, request.Lock) ||
                IsDowngradedLock(_currentState.Lock, request.Lock))
            {
                // Current lock state is the same as or superior to the 
                //	request lock so lock must be held
                request.TrySetResult(true);
            }
            else
            {
                // If we get to this point then we don't have the lock
                request.TrySetResult(false);
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private LockOwnerIdentity GetThreadLockOwnerIdentity(bool throwIfMissing)
        {
            var sessionId = SessionId.Zero;
            var transactionId = TransactionId.Zero;
            if (TrunkSessionContext.Current != null)
            {
                sessionId = TrunkSessionContext.Current.SessionId;
            }
            if (TrunkTransactionContext.Current != null)
            {
                transactionId = TrunkTransactionContext.Current.TransactionId;
            }

            if (throwIfMissing && sessionId == SessionId.Zero && transactionId == TransactionId.Zero)
            {
                throw new LockException("Current thread has no transaction context!");
            }
            return new LockOwnerIdentity(sessionId, transactionId);
        }

        private LockOwnerIdentity? GetActiveLockOwner(LockOwnerIdentity lockOwner)
        {
            if (_activeRequests.ContainsKey(lockOwner))
            {
                return lockOwner;
            }

            lockOwner = lockOwner.SessionOnlyLockOwner;
            if (_activeRequests.ContainsKey(lockOwner))
            {
                return lockOwner;
            }

            return null;
        }

        /// <summary>
        /// Gets the current <typeparamref name="TLockTypeEnum"/> that
        /// represents the current state of the lock.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The current lock state is determined by the <see cref="T:AcquireLock"/>
        /// objects in the active request list.
        /// </remarks>
        private TLockTypeEnum GetActiveLockType()
        {
            var lockType = NoneLockType;
            foreach (var request in _activeRequests.Values)
            {
                if (Convert.ToInt32(request.Lock) > Convert.ToInt32(lockType))
                {
                    lockType = request.Lock;
                }
            }
            return lockType;
        }

        private void UpdateActiveLockState()
        {
            var lockType = GetActiveLockType();
            var newState = GetStateFromType(lockType);
            if (_currentState != newState)
            {
                var oldState = _currentState;
                oldState?.OnExitState(this, newState);
                _currentState = newState;
                newState?.OnEnterState(this, oldState);
            }
        }

        private void ReleaseWaitingRequests()
        {
            UpdateActiveLockState();

            if (_pendingExclusiveRequest != null)
            {
                if (!ReleaseWaitingRequest(_pendingExclusiveRequest))
                {
                    return;
                }

                // Discard exclusive request and update state
                _pendingExclusiveRequest = null;
                UpdateActiveLockState();
            }

            while (_pendingRequests.Count > 0)
            {
                // Attempt to release the next request from the queue
                var queuedRequest = _pendingRequests.Peek();
                if (!ReleaseWaitingRequest(queuedRequest))
                {
                    break;
                }

                // Remove element from queue and update active lock state
                _pendingRequests.Dequeue();
                UpdateActiveLockState();
            }
        }

        private bool ReleaseWaitingRequest(AcquireLock request)
        {
            // Skip aborted requests
            if (request.Task.IsCanceled ||
                request.Task.IsCompleted ||
                request.Task.IsFaulted)
            {
                return true;
            }

            // If this lock is compatable then add to active list
            //	and release waiting request.
            if (_currentState.CanAcquireLock(this, request))
            {
                SetActiveRequest(request);
                return true;
            }

            // Incompatable lock.
            return false;
        }

        private bool IsDowngradedLock(TLockTypeEnum currentLock, TLockTypeEnum newLock)
        {
            return (Convert.ToInt32(currentLock) > Convert.ToInt32(newLock));
        }

        private bool IsEquivalentLock(TLockTypeEnum currentLock, TLockTypeEnum newLock)
        {
            return (Convert.ToInt32(currentLock) == Convert.ToInt32(newLock));
        }
        #endregion

        #region IReferenceLock Members
        void IReferenceLock.AddRefLock()
        {
            AddRefLock();
        }

        void IReferenceLock.ReleaseRefLock()
        {
            ReleaseRefLock();
        }
        #endregion
    }
}
