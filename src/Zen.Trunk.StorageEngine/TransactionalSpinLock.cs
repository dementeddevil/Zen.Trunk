using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
    public class TransactionalSpinLock
    {
        private class TransactionSpinLockLogEventEnricher : ILogEventEnricher
        {
            private readonly TransactionalSpinLock _owner;

            public TransactionSpinLockLogEventEnricher(TransactionalSpinLock owner)
            {
                _owner = owner;
            }

            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                _owner.EnrichLogEvent(logEvent, propertyFactory);
            }
        }

        private static int NextTransactionLockId = 1;
        private readonly SpinLock _spinLock = new SpinLock(true);
        private readonly int _transactionLockId;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionalSpinLock"/> class.
        /// </summary>
        public TransactionalSpinLock()
        {
            _transactionLockId = Interlocked.Increment(ref NextTransactionLockId);
        }

        /// <summary>
        /// Runs the specified delegate under the lock.
        /// </summary>
        /// <param name="runUnderLock">The delegate to be executed while holding the lock.</param>
        public async Task ExecuteAsync(Func<Task> runUnderLock)
        {
            using (var context = FromLockContext())
            {
                var transactionId = TrunkTransactionContext.Current?.TransactionId ?? TransactionId.Zero;
                var lockTaken = false;
                try
                {
                    Serilog.Log.Debug("Spinlock -> Enter {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    _spinLock.Enter(ref lockTaken);

                    await runUnderLock().ConfigureAwait(true);
                }
                finally
                {
                    if (lockTaken)
                    {
                        Serilog.Log.Debug("Spinlock -> Exit {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                        _spinLock.Exit(true);
                    }
                }
            }
        }

        protected IDisposable FromLockContext()
        {
            return Serilog.Context.LogContext.Push(new TransactionSpinLockLogEventEnricher(this));
        }

        protected virtual void EnrichLogEvent(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(propertyFactory
                .CreateProperty("TransactionLockType", GetType().Name));
            logEvent.AddOrUpdateProperty(propertyFactory
                .CreateProperty("TransactionSpinLockId", _transactionLockId.ToString("X8")));
        }
    }
}
