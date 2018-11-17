using System;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// <c>ScatterGatherRequestQueueSettings</c> contains settings
    /// that control how the request queue will operate.
    /// </summary>
    public class ScatterGatherRequestQueueSettings
    {
        /// <summary>
        /// Get or sets the read request queue settings
        /// </summary>
        public StreamScatterGatherRequestQueueSettings ReadSettings { get; set; } = new StreamScatterGatherRequestQueueSettings();

        /// <summary>
        /// Get or sets the write request queue settings
        /// </summary>
        public StreamScatterGatherRequestQueueSettings WriteSettings { get; set; } = new StreamScatterGatherRequestQueueSettings();

        /// <summary>
        /// Gets or sets the automatic flush period used by the flusher thread
        /// </summary>
        public TimeSpan AutomaticFlushPeriod { get; set; } = TimeSpan.FromMilliseconds(200);
    }
}