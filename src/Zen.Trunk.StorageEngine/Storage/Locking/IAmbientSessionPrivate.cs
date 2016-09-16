using System;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Locking
{
    internal interface IAmbientSessionPrivate : IAmbientSession
    {
        TransactionLockOwnerBlock GetTransactionLockOwnerBlock(IDatabaseLockManager lockManager);

        Task<bool> CommitAsync();

        Task<bool> RollbackAsync();
    }
}