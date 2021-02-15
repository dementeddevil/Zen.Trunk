using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        //private readonly ConcurrentQueue<TransactionId> _waiting = new ConcurrentQueue<TransactionId>();
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
                    //if (transactionId != TransactionId.Zero)
                    //{
                    //    if (!_waiting.Contains(transactionId))
                    //    {
                    //        _waiting.Enqueue(transactionId);
                    //    }

                    //    while (true)
                    //    {
                    //        if (_waiting.TryPeek(out TransactionId lockOwner) && lockOwner == transactionId)
                    //        {
                    //            lockTaken = true;
                    //            break;
                    //        }
                    //    }
                    //}
                    Serilog.Log.Debug("Spinlock -> Enter {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    _spinLock.Enter(ref lockTaken);

                    await runUnderLock().ConfigureAwait(true);
                }
                finally
                {
                    if (lockTaken)
                    {
                        //if (_waiting.TryPeek(out TransactionId lockOwner) && lockOwner == transactionId)
                        //{
                        //    _waiting.TryDequeue(out lockOwner);
                        //}
                        //else
                        //{
                        //    Serilog.Log.Warning("Lock not held by caller.");
                        //}
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
