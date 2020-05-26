using System;

namespace Zen.Trunk.Storage.Logging
{
    /// <summary>
    /// Defines a No-Op log entry.
    /// </summary>
    /// <remarks>
    /// These log records are used exclusively by the logging sub-system
    /// when truncating a transaction log. These ensure that a log-file is
    /// filled with valid records during the process.
    /// </remarks>
    [Serializable]
    public class NoOpLogEntry : LogEntry
    {
    }
}