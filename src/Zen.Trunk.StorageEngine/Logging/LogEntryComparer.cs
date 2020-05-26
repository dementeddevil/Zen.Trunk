using System.Collections.Generic;

namespace Zen.Trunk.Storage.Logging
{
    /// <summary>
    /// Comparison class for testing equality of <see cref="T:LogEntry"/>
    /// objects.
    /// </summary>
    public class LogEntryComparer : IComparer<LogEntry>
    {
        #region IComparer<LogEntry> Members
        int IComparer<LogEntry>.Compare(LogEntry x, LogEntry y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            if (x == null || y == null)
            {
                return -1;
            }
            return x.LogId.CompareTo(y.LogId);
        }
        #endregion
    }
}