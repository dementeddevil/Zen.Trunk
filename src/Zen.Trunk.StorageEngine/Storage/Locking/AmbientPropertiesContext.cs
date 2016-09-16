using System;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Locking
{
    public static class AmbientPropertiesContext
    {
        private class AmbientPropertiesScope : IDisposable
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }

        private const string LogicalContextName = "TrunkAmbientProperties";
        private static readonly TimeSpan DefaultProcessTransactionTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets the current trunk transaction.
        /// </summary>
        /// <value>
        /// An instance of <see cref="ITrunkTransaction"/> representing the current transaction;
        /// otherwise <c>null</c> if no transaction is in progress.
        /// </value>
        public static IAmbientProperties Current
        {
            get
            {
                return (IAmbientProperties)CallContext.LogicalGetData(LogicalContextName);
            }
            private set
            {
                if (value != null)
                {
                    CallContext.LogicalSetData(LogicalContextName, value);
                }
                else
                {
                    CallContext.FreeNamedDataSlot(LogicalContextName);
                }
            }
        }

        public static void BeginProcess(ProcessId processId, TimeSpan? defaultTransactionTimeout)
        {
            BeginAmbientProperties(new AmbientProperties(processId, defaultTransactionTimeout ?? DefaultProcessTransactionTimeout));
        }

        public static void BeginSession(SessionId sessionId, TimeSpan? defaultTransactionTimeout)
        {
            BeginAmbientProperties(new AmbientProperties(Current.ProcessId, sessionId,
                defaultTransactionTimeout ?? Current.DefaultTransactionTimeout ?? DefaultProcessTransactionTimeout));
        }

        public static Task CommitSessionAsync()
        {
            throw new NotImplementedException();
        }

        public static Task RollbackSessionAsync()
        {
            throw new NotImplementedException();
        }

        public static Task CommitProcessAsync()
        {
            throw new NotImplementedException();
        }

        public static Task RollbackProcessAsync()
        {
            throw new NotImplementedException();
        }

        private static void BeginAmbientProperties(IAmbientProperties properties)
        {
            if (Current == null)
            {
                Current = properties;
            }
            else
            {
                var priv = Current as IAmbientPropertiesPrivate;
                priv?.BeginNestedSession(
                    properties.SessionId,
                    properties.DefaultTransactionTimeout ?? Current.DefaultTransactionTimeout ?? DefaultProcessTransactionTimeout);
            }
        }
    }
}