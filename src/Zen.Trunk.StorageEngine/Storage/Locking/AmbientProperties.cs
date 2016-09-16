using System;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Locking
{
    internal class AmbientProperties : MarshalByRefObject, IAmbientPropertiesPrivate
    {
        public AmbientProperties(ProcessId processId, TimeSpan defaultTransactionTimeout)
        {
            ProcessId = processId;
            DefaultTransactionTimeout = defaultTransactionTimeout;
        }

        public AmbientProperties(ProcessId processId, SessionId sessionId, TimeSpan defaultTransactionTimeout)
        {
            ProcessId = processId;
            SessionId = sessionId;
            DefaultTransactionTimeout = defaultTransactionTimeout;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ProcessId ProcessId { get; }

        public SessionId SessionId { get; }

        public VirtualSessionId VirtualSessionId { get; private set; }

        public TimeSpan? DefaultTransactionTimeout { get; }

        public void BeginNestedSession(SessionId sessionId, TimeSpan defaultTransactionTimeout)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CommitAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> RollbackAsync()
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            
        }
    }
}