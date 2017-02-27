using System;
using System.Threading.Tasks;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Query;

namespace Zen.Trunk.Network
{
    /// <summary>
    /// <c>Connection</c> defines an connection with the database system.
    /// </summary>
    /// <seealso cref="IConnection" />
    public class Connection : IConnection
    {
        private QueryExecutionContext _executionContext;
        private ISession _session;
        private ITrunkSession _trunkSession;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="masterDatabase">The master database.</param>
        public Connection(ISession session, MasterDatabaseDevice masterDatabase)
        {
            _executionContext = new QueryExecutionContext(masterDatabase);
            _session = session;

            _trunkSession = new TrunkSession(_session.SessionId, TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Cancels the current batch.
        /// </summary>
        public void CancelExecution()
        {
            _executionContext.CancelExecution();
        }

        /// <summary>
        /// Resets this instance and optionally change the active database to
        /// the master database.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task ResetAsync(bool switchToMasterDatabase = false)
        {
            return _executionContext.ResetAsync(switchToMasterDatabase);
        }

        /// <summary>
        /// Executes the specified action the under session context associated
        /// with this connection instance.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task ExecuteUnderSessionAsync(Func<QueryExecutionContext, Task> action)
        {
            await EnsureActiveDatabaseOnExecutionContextAsync().ConfigureAwait(false);

            using (TrunkSessionContext.SwitchSessionContext(_trunkSession))
            {
                await action(_executionContext).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes the specified action the under session context associated
        /// with this connection instance.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="action">The action to be executed.</param>
        /// <returns>
        /// A <see cref="Task{TResult}" /> representing the asynchronous operation.
        /// </returns>
        public async Task<TResult> ExecuteUnderSessionAsync<TResult>(Func<QueryExecutionContext, Task<TResult>> action)
        {
            await EnsureActiveDatabaseOnExecutionContextAsync().ConfigureAwait(false);

            using (TrunkSessionContext.SwitchSessionContext(_trunkSession))
            {
                return await action(_executionContext).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _isDisposed)
            {
                _isDisposed = true;

                // Unlock active database if defined - this must be synchronous
                if (_executionContext.ActiveDatabase != null)
                {
                    _executionContext.SetActiveDatabaseAsync(null).Wait();
                }

                _trunkSession?.Dispose();
                _session?.Dispose();
            }

            _executionContext = null;
            _trunkSession = null;
            _session = null;
        }

        private async Task EnsureActiveDatabaseOnExecutionContextAsync()
        {
            if (_executionContext.ActiveDatabase == null)
            {
                await _executionContext
                    .SetActiveDatabaseAsync(_executionContext.MasterDatabase)
                    .ConfigureAwait(false);
            }
        }
    }
}
