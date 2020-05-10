using Serilog.Core;
using Serilog.Events;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    public class TransactionEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(
                propertyFactory.CreateProperty(
                    "SessionId",
                    new ScalarValue(TrunkSessionContext.Current?.SessionId.Value ?? 0)));

            logEvent.AddOrUpdateProperty(
                propertyFactory.CreateProperty(
                    "TransactionId",
                    new ScalarValue(TrunkTransactionContext.Current?.TransactionId.Value ?? 0)));
        }
    }
}