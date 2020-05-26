using System.Threading.Tasks;
using Zen.Trunk.Storage.Logging;

namespace Zen.Trunk.Storage.Locking
{
    internal interface ITrunkTransactionPrivate : ITrunkTransaction
    {
        bool IsCompleted { get; }

        TransactionLockOwnerBlock GetTransactionLockOwnerBlock(IDatabaseLockManager lockManager);

        void BeginNestedTransaction();

        void Enlist(IPageEnlistmentNotification notify);

        Task WriteLogEntryAsync(TransactionLogEntry entry);

        Task<bool> CommitAsync();

        Task<bool> RollbackAsync();
    }
}