using System;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// <c>CachingPageBufferDeviceSettings</c> define configurable settings
    /// that dictate how the <see cref="CachingPageBufferDevice"/> operates.
    /// </summary>
    public class CachingPageBufferDeviceSettings
    {
        /// <summary>
        /// Gets or sets the maximum size of the cache.
        /// </summary>
        /// <value>
        /// The maximum size of the cache.
        /// By default this is set to 2048
        /// </value>
        public int MaximumCacheSize { get; set; } = 2048;

        /// <summary>
        /// Gets or sets the cache scavenge off threshold.
        /// </summary>
        /// <value>
        /// The cache scavenge off threshold.
        /// </value>
        public int CacheScavengeOffThreshold { get; set; } = 1500;

        /// <summary>
        /// Gets or sets the cache scavenge on threshold.
        /// </summary>
        /// <value>
        /// The cache scavenge on threshold.
        /// </value>
        public int CacheScavengeOnThreshold { get; set; } = 1800;

        /// <summary>
        /// Gets or sets the cache flush interval.
        /// </summary>
        /// <value>
        /// The cache flush interval.
        /// </value>
        public TimeSpan CacheFlushInterval { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets the minimum size of the free pool.
        /// </summary>
        /// <value>
        /// The minimum size of the free pool.
        /// </value>
        public int MinimumFreePoolSize { get; set; } = 50;

        /// <summary>
        /// Gets or sets the maximum size of the free pool.
        /// </summary>
        /// <value>
        /// The maximum size of the free pool.
        /// </value>
        public int MaximumFreePoolSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the free pool monitor interval.
        /// </summary>
        /// <value>
        /// The free pool monitor interval.
        /// </value>
        public TimeSpan FreePoolMonitorInterval { get; set; } = TimeSpan.FromMilliseconds(1000);

        /// <summary>
        /// Gets or sets the minimum size of the block flush.
        /// </summary>
        /// <value>
        /// The minimum size of the block flush.
        /// </value>
        public int MinimumBlockFlushSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum size of the block flush.
        /// </summary>
        /// <value>
        /// The maximum size of the block flush.
        /// </value>
        public int MaximumBlockFlushSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the block flush thread count.
        /// </summary>
        /// <value>
        /// The block flush thread count.
        /// </value>
        public int BlockFlushThreadCount { get; set; } = 4;

        /// <summary>
        /// Gets or sets the initialize buffer thread count.
        /// </summary>
        /// <value>
        /// The initialize buffer thread count.
        /// </value>
        public int InitBufferThreadCount { get; set; } = 10;

        /// <summary>
        /// Gets or sets the load buffer thread count.
        /// </summary>
        /// <value>
        /// The load buffer thread count.
        /// </value>
        public int LoadBufferThreadCount { get; set; } = 10;
    }
}