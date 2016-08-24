using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data
{
    public interface ITransactionContextTaskRequest
    {
        ITrunkTransaction TransactionContext
        {
            get;
            set;
        }
    }
}