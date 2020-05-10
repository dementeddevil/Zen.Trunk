using System;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>TrunkSessionContext</c> is an object that tracks the current
    /// session for the current execution context.
    /// </summary>
	/// <remarks>
	/// <para>
	/// This object makes it possible for any method in a call chain to update
	/// the transaction with dirty pages.
	/// </para>
	/// </remarks>
    public static class TrunkSessionContext
    {
        private class TrunkSessionScope : IDisposable
        {
            private ITrunkSession _oldContext;
            private bool _disposed;

            public TrunkSessionScope(ITrunkSession newContext)
            {
                _oldContext = Current;
                Current = newContext;

                TraceSession("Enter", _oldContext, Current);
            }

            public void Dispose()
            {
                Dispose(true);
            }

            private void Dispose(bool disposing)
            {
                if (!_disposed && disposing)
                {
                    _disposed = true;

                    var prevContext = Current;
                    Current = _oldContext;

                    TraceSession("Leave", prevContext, Current);
                }

                _oldContext = null;
            }

            private void TraceSession(string action, ITrunkSession prev, ITrunkSession next)
            {
                if (prev?.SessionId == next?.SessionId)
                {
                    return;
                }

                var threadId = Thread.CurrentThread.ManagedThreadId;
                var prevSessionId = prev?.SessionId.ToString() ?? "N/A";
                var nextSessionId = next?.SessionId.ToString() ?? "N/A";
                Serilog.Log.Information(
                    "{Action} session scope on thread {ThreadId} switching session from {PrevSessionId} to {NextSessionId}",
                    action, threadId, prevSessionId, nextSessionId);
            }
        }

        private const string LogicalContextName = "TrunkSessionContext";
        private static readonly TimeSpan DefaultProcessTransactionTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets the current trunk transaction.
        /// </summary>
        /// <value>
        /// An instance of <see cref="ITrunkTransaction"/> representing the current transaction;
        /// otherwise <c>null</c> if no transaction is in progress.
        /// </value>
        public static ITrunkSession Current
        {
            get => (ITrunkSession)CallContext.LogicalGetData(LogicalContextName);
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

        /// <summary>
        /// Begins the session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="defaultTransactionTimeout">The default transaction timeout.</param>
        public static void BeginSession(SessionId sessionId, TimeSpan? defaultTransactionTimeout)
        {
            BeginSession(new TrunkSession(
                sessionId,
                defaultTransactionTimeout ?? DefaultProcessTransactionTimeout));
        }

        /// <summary>
        /// Commits the session.
        /// </summary>
        /// <returns></returns>
        public static async Task CommitAsync()
        {
            if (Current is ITrunkSessionPrivate session)
            {
                var result = await session
                    .CommitAsync()
                    .WithTimeout(session.DefaultTransactionTimeout)
                    .ConfigureAwait(false);
                if (result)
                {
                    Current = null;
                }
            }
        }

        /// <summary>
        /// Rollbacks the session.
        /// </summary>
        /// <returns></returns>
        public static async Task RollbackAsync()
        {
            if (Current is ITrunkSessionPrivate session)
            {
                var result = await session
                    .RollbackAsync()
                    .WithTimeout(session.DefaultTransactionTimeout)
                    .ConfigureAwait(false);
                if (result)
                {
                    Current = null;
                }
            }
        }

        internal static IDisposable SwitchSessionContext(ITrunkSession newSession)
        {
            return new TrunkSessionScope(newSession);
        }

        private static void BeginSession(ITrunkSession session)
        {
            if (Current != null)
            {
                throw new InvalidOperationException("Session is already associated with caller.");
            }
            Current = session;
        }
    }
}