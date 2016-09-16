using System;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Locking
{
    internal interface IAmbientPropertiesPrivate : IAmbientProperties
    {
        void BeginNestedSession(SessionId sessionId, TimeSpan defaultTransactionTimeout);

        Task<bool> CommitAsync();

        Task<bool> RollbackAsync();
    }
}