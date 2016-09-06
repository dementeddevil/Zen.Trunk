using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Threading.SynchronizationContext" />
    public class TransactionSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object state)
        {
            base.Post(d, state);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            base.Send(d, state);
        }

        public override SynchronizationContext CreateCopy()
        {
            return new TransactionSynchronizationContext();
        }
    }

    public class TransactionalSpinLock
    {
        private ConcurrentQueue<TransactionId> _waiting = new ConcurrentQueue<TransactionId>();

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
