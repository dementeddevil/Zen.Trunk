using System;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// <c>StreamScatterGatherRequestQueueSettings</c> contains settings used
    /// to configure a <see cref="StreamScatterGatherRequestQueue"/>.
    /// </summary>
    public class StreamScatterGatherRequestQueueSettings
    {
        /// <summary>
        /// Gets or sets the maximum request age.
        /// </summary>
        /// <remarks>
        /// Once this age is exceeded, the block containing the request will
        /// be flushed.
        /// </remarks>
        public TimeSpan MaximumRequestAge { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets how often the request block coalescing logic is run.
        /// </summary>
        public TimeSpan CoalesceRequestsPeriod { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets the maximum number of requests that can exist in a
        /// single block before it is flushed.
        /// </summary>
        public int MaximumRequestBlockLength { get; set; } = 16;
        
        /// <summary>
        /// Gets or sets the maximum number of blocks that are tracked by the
        /// queue.
        /// </summary>
        public int MaximumRequestBlocks { get; set; } = 5;
    }
}