using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using Zen.Trunk.Extensions;

namespace Zen.Trunk.Storage.Locking
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global    
    /// <summary>
    /// <c>TrunkSession</c> object maintains the context of a session within the
    /// database.
    /// </summary>
    /// <seealso cref="System.MarshalByRefObject" />
    /// <seealso cref="Zen.Trunk.Storage.Locking.ITrunkSessionPrivate" />
    internal class TrunkSession : MarshalByRefObject, ITrunkSessionPrivate
    {
        private static readonly ILogger Logger = Serilog.Log.ForContext<TrunkTransaction>();
        private readonly Dictionary<DatabaseId, TransactionLockOwnerBlock> _transactionLockOwnerBlocks = new Dictionary<DatabaseId, TransactionLockOwnerBlock>();
        private bool _isCompleting;
        private bool _isCompleted;

        public TrunkSession(SessionId sessionId, TimeSpan transactionTimeout)
        {
            SessionId = sessionId;
            TransactionTimeout = transactionTimeout;
        }

        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        /// <value>
        /// The session identifier.
        /// </value>
        public SessionId SessionId { get; }

        /// <summary>
        /// Gets the transaction timeout.
        /// </summary>
        /// <value>
        /// The transaction timeout.
        /// </value>
        public TimeSpan TransactionTimeout { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the transaction lock owner block associated with the given lock manager.
        /// </summary>
        /// <param name="lockManager">The lock manager.</param>
        /// <returns></returns>
        public TransactionLockOwnerBlock GetTransactionLockOwnerBlock(IDatabaseLockManager lockManager)
        {
            if (!_transactionLockOwnerBlocks.TryGetValue(lockManager.DatabaseId, out var block))
            {
                block = new TransactionLockOwnerBlock(lockManager);
                _transactionLockOwnerBlocks.Add(lockManager.DatabaseId, block);
            }
            return block;
        }

        /// <summary>
        /// Switches the shared database lock.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="lockTimeout">The lock timeout.</param>
        /// <returns></returns>
        public async Task SwitchSharedDatabaseLockAsync(IDatabaseDevice from, IDatabaseDevice to, TimeSpan lockTimeout)
        {
            if (from == to)
            {
                return;
            }

            if (to != null)
            {
                await to
                    .UseDatabaseAsync(lockTimeout)
                    .ConfigureAwait(false);
            }

            if (from != null)
            {
                await from
                    .UnuseDatabaseAsync()
                    .ConfigureAwait(false);
            }
        }


        public async Task<bool> CommitAsync()
        {
            CheckNotCompleted();

            await ReleaseAsync()
                .WithTimeout(TransactionTimeout)
                .ConfigureAwait(false);
            return true;
        }

        public async Task<bool> RollbackAsync()
        {
            CheckNotCompleted();

            await ReleaseAsync()
                .WithTimeout(TransactionTimeout)
                .ConfigureAwait(false);
            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isCompleted && !_isCompleting)
            {
                Logger.Warning(
                    "{SessionId} => In-progress session disposed - performing implicit rollback",
                    SessionId);
                
                // Force rollback of the current transaction
                RollbackAsync().GetAwaiter().GetResult();
            }
        }

        private void CheckNotCompleted()
        {
            if (_isCompleted)
            {
                throw new InvalidOperationException("Session already completed.");
            }
        }

        private async Task ReleaseAsync()
        {
            _isCompleting = true;

            // Release all locks
            foreach (var transactionLock in _transactionLockOwnerBlocks.Values)
            {
                await transactionLock.ReleaseAllAsync().ConfigureAwait(false);
            }

            _transactionLockOwnerBlocks.Clear();

            _isCompleted = true;
            _isCompleting = false;
        }
    }
}