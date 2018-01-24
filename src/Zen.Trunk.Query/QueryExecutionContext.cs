using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// 
    /// </summary>
    public class QueryExecutionContext
    {
        private CancellationTokenSource _cancelSource =
            new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryExecutionContext"/> class.
        /// </summary>
        /// <param name="masterDatabase">The master database.</param>
        public QueryExecutionContext(MasterDatabaseDevice masterDatabase)
        {
            MasterDatabase = masterDatabase;
        }

        /// <summary>
        /// Gets the master database.
        /// </summary>
        /// <value>
        /// The master database.
        /// </value>
        public MasterDatabaseDevice MasterDatabase { get; }

        /// <summary>
        /// Gets or sets the active database.
        /// </summary>
        /// <value>
        /// The active database.
        /// </value>
        public DatabaseDevice ActiveDatabase { get; private set; }

        /// <summary>
        /// Gets or sets the current transaction isolation level.
        /// </summary>
        /// <value>
        /// The current transaction isolation level.
        /// </value>
        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

        /// <summary>
        /// Sets the active database.
        /// </summary>
        /// <param name="newActiveDatabase">The new active database.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method will attempt to obtain a shared database lock on the
        /// new active database before releasing the lock on the previous one.
        /// </remarks>
        /// <exception cref="LockTimeoutException">
        /// Thrown if a shared lock on the new database cannot be acquired.
        /// In this case the active database will be unchanged.
        /// </exception>
        public async Task SetActiveDatabaseAsync(DatabaseDevice newActiveDatabase)
        {
            var ambientSession = TrunkSessionContext.Current;
            if (ambientSession != null)
            {
                await ambientSession
                    .SwitchSharedDatabaseLockAsync(
                        ActiveDatabase, newActiveDatabase, TimeSpan.FromSeconds(10))
                    .ConfigureAwait(false);
            }

            ActiveDatabase = newActiveDatabase;
        }

        /// <summary>
        /// Requests cancellation of the currently executing batch.
        /// </summary>
        public void CancelExecution()
        {
            _cancelSource.Cancel();
        }

        /// <summary>
        /// Throws if cancellation requested.
        /// </summary>
        public void ThrowIfCancellationRequested()
        {
            _cancelSource.Token.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Resets the execution context.
        /// </summary>
        /// <param name="switchToMasterDatabase">if set to <c>true</c> [switch to master database].</param>
        /// <returns></returns>
        public async Task ResetAsync(bool switchToMasterDatabase)
        {
            _cancelSource = new CancellationTokenSource();
            if (switchToMasterDatabase && ActiveDatabase != MasterDatabase)
            {
                await SetActiveDatabaseAsync(MasterDatabase).ConfigureAwait(false);
            }
        }
    }
}