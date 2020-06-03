using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
        /// Runs the specified delegate under the lock.
        /// </summary>
        /// <param name="runUnderLock">The delegate to be executed while holding the lock.</param>
        public async Task ExecuteAsync(Func<Task> runUnderLock)
        {
            var transactionId = TrunkTransactionContext.Current?.TransactionId ?? TransactionId.Zero;
            var lockTaken = false;
            try
            {
                if (transactionId != TransactionId.Zero)
                {
                    if (!_waiting.Contains(transactionId))
                    {
                        _waiting.Enqueue(transactionId);
                    }

                    while (true)
                    {
                        if (_waiting.TryPeek(out TransactionId lockOwner) && lockOwner == transactionId)
                        {
                            lockTaken = true;
                            break;
                        }
                    }
                }

                await runUnderLock().ConfigureAwait(true);
            }
            finally
            {
                if (lockTaken)
                {
                    if (_waiting.TryPeek(out TransactionId lockOwner) && lockOwner == transactionId)
                    {
                        _waiting.TryDequeue(out lockOwner);
                    }
                    else
                    {
                        Serilog.Log.Warning("Lock not held by caller.");
                    }
                }
            }
        }
    }
}
