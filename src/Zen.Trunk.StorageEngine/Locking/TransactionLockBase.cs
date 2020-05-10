using System;
using System.Threading;
using Serilog.Core;
using Serilog.Events;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>TransactionLockBase</c> serves as the base class for all transaction
    /// lock objects.
    /// </summary>
    public abstract class TransactionLockBase
    {
        private class TransactionLockLogEventEnricher : ILogEventEnricher
        {
            private readonly TransactionLockBase _owner;

            public TransactionLockLogEventEnricher(TransactionLockBase owner)
            {
                _owner = owner;
            }

            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                _owner.EnrichLogEvent(logEvent, propertyFactory);
            }
        }

        private static int _nextTransactionLockId;
        private readonly int _transactionLockId;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionLockBase"/> class.
        /// </summary>
        protected TransactionLockBase()
        {
            _transactionLockId = Interlocked.Increment(ref _nextTransactionLockId);
        }

        protected IDisposable FromLockContext()
        {
            return Serilog.Context.LogContext.Push(new TransactionLockLogEventEnricher(this));
        }

        protected virtual void EnrichLogEvent(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(propertyFactory
                .CreateProperty("TransactionLockType", GetType().Name));
            logEvent.AddOrUpdateProperty(propertyFactory
                .CreateProperty("TransactionLockId", _transactionLockId.ToString("X8")));
        }
    }
}
