using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Locking
{
    internal interface ITrunkSessionPrivate : ITrunkSession
    {
        TransactionLockOwnerBlock GetTransactionLockOwnerBlock(IDatabaseLockManager lockManager);

        Task<bool> CommitAsync();

        Task<bool> RollbackAsync();
    }
}