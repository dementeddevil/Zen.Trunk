using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zen.Trunk.Logging;

namespace Zen.Trunk.Storage.Locking
{
    internal class AmbientSession : MarshalByRefObject, IAmbientSessionPrivate
    {
        private static readonly ILog Logger = LogProvider.For<TrunkTransaction>();
        private readonly Dictionary<DatabaseId, TransactionLockOwnerBlock> _transactionLockOwnerBlocks = new Dictionary<DatabaseId, TransactionLockOwnerBlock>();
        private bool _isCompleting;
        private bool _isCompleted;

        public AmbientSession(SessionId sessionId, TimeSpan defaultTransactionTimeout)
        {
            SessionId = sessionId;
            DefaultTransactionTimeout = defaultTransactionTimeout;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public SessionId SessionId { get; }

        public TimeSpan DefaultTransactionTimeout { get; }

        /// <summary>
        /// Gets the transaction lock owner block associated with the given lock manager.
        /// </summary>
        /// <param name="lockManager">The lock manager.</param>
        /// <returns></returns>
        public TransactionLockOwnerBlock GetTransactionLockOwnerBlock(IDatabaseLockManager lockManager)
        {
            TransactionLockOwnerBlock block;
            if (!_transactionLockOwnerBlocks.TryGetValue(lockManager.DatabaseId, out block))
            {
                block = new TransactionLockOwnerBlock(lockManager);
                _transactionLockOwnerBlocks.Add(lockManager.DatabaseId, block);
            }
            return block;
        }

        public async Task<bool> CommitAsync()
        {
            CheckNotCompleted();

            await ReleaseAsync().ConfigureAwait(false);
            return true;
        }

        public async Task<bool> RollbackAsync()
        {
            CheckNotCompleted();

            await ReleaseAsync().ConfigureAwait(false);
            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isCompleted && !_isCompleting)
            {
                if (Logger.IsWarnEnabled())
                {
                    Logger.Warn($"{SessionId} => In-progress session disposed - performing implicit rollback");
                }

                // Force rollback of the current transaction
                RollbackAsync().Wait(DefaultTransactionTimeout);
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