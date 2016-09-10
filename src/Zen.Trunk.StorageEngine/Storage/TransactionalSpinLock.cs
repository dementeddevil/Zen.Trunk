using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
    public class TransactionalSpinLock
    {
        private readonly ConcurrentQueue<TransactionId> _waiting = new ConcurrentQueue<TransactionId>();

        /// <summary>
        /// Enters lock.
        /// </summary>
        /// <param name="lockTaken">
        /// Set to <c>false</c> before calling this method.
        /// If set to <c>true</c> when this function returns then the lock was taken and
        /// a corresponding call to <see cref="Exit"/> must be made.</param>
        public void Enter(ref bool lockTaken)
        {
            var transactionId = TrunkTransactionContext.Current?.TransactionId ?? TransactionId.Zero;
            if (transactionId == TransactionId.Zero || _waiting.Contains(transactionId))
            {
                return;
            }

            _waiting.Enqueue(transactionId);

            while (true)
            {
                TransactionId lockOwner;
                if (_waiting.TryPeek(out lockOwner) && lockOwner == transactionId)
                {
                    lockTaken = true;
                    return;
                }
            }
        }

        /// <summary>
        /// Exits this instance.
        /// </summary>
        public void Exit()
        {
            var transactionId = TrunkTransactionContext.Current?.TransactionId ?? TransactionId.Zero;
            if (transactionId == TransactionId.Zero)
            {
                return;
            }

            TransactionId lockOwner;
            if (_waiting.TryPeek(out lockOwner) && lockOwner == transactionId)
            {
                _waiting.TryDequeue(out lockOwner);
            }
            else
            {
                Console.WriteLine("Lock not held by caller.");
            }
        }

        /// <summary>
        /// Runs the specified delegate under the lock.
        /// </summary>
        /// <param name="runUnderLock">The delegate to be executed while holding the lock.</param>
        public async Task ExecuteAsync(Func<Task> runUnderLock)
        {
            var lockTaken = false;
            try
            {
                Enter(ref lockTaken);
                await runUnderLock().ConfigureAwait(true);
            }
            finally
            {
                if (lockTaken) Exit();
            }
        }
    }
}
