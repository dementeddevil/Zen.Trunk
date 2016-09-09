namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// 
    /// </summary>
    public class ExecutionContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionContext"/> class.
        /// </summary>
        /// <param name="masterDatabase">The master database.</param>
        /// <param name="activeDatabase">The active database.</param>
        public ExecutionContext(MasterDatabaseDevice masterDatabase, DatabaseDevice activeDatabase = null)
        {
            MasterDatabase = masterDatabase;
            ActiveDatabase = activeDatabase ?? masterDatabase;
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
        public DatabaseDevice ActiveDatabase { get; set; }
    }
}