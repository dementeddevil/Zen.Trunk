using System;

namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// Defines the end of a checkpoint operation.
    /// </summary>
    [Serializable]
    public class EndCheckPointLogEntry : CheckPointLogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EndCheckPointLogEntry"/> class.
        /// </summary>
        public EndCheckPointLogEntry()
            : base(LogEntryType.EndCheckpoint)
        {
        }
    }
}