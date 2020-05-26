namespace Zen.Trunk.Storage.Logging
{
    /// <summary>
    /// Identifies the valid types of <see cref="T:LogEntry"/> records
    /// that can be written or read from a transaction log.
    /// </summary>
    public enum LogEntryType
    {
        /// <summary>
        /// The no op
        /// </summary>
        NoOp = 0,
        /// <summary>
        /// The begin checkpoint
        /// </summary>
        BeginCheckpoint = 1,
        /// <summary>
        /// The end checkpoint
        /// </summary>
        EndCheckpoint = 2,
        /// <summary>
        /// The begin xact
        /// </summary>
        BeginXact = 3,
        /// <summary>
        /// The commit xact
        /// </summary>
        CommitXact = 4,
        /// <summary>
        /// The rollback xact
        /// </summary>
        RollbackXact = 5,
        /// <summary>
        /// The create page
        /// </summary>
        CreatePage = 6,
        /// <summary>
        /// The modify page
        /// </summary>
        ModifyPage = 7,
        /// <summary>
        /// The delete page
        /// </summary>
        DeletePage = 8,
    }
}