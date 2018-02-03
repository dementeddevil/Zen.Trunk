using System;

namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// Defines the start of a checkpoint operation.
    /// </summary>
    [Serializable]
    public class BeginCheckPointLogEntry : CheckPointLogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BeginCheckPointLogEntry"/> class.
        /// </summary>
        public BeginCheckPointLogEntry()
            : base(LogEntryType.BeginCheckpoint)
        {
        }
    }
}