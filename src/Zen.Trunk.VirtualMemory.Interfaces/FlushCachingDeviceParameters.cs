using System;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="FlushDeviceParameters" />
    [CLSCompliant(false)]
    public class FlushCachingDeviceParameters : FlushDeviceParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initialises an instance of <see cref="FlushCachingDeviceParameters" />.
        /// </summary>
        public FlushCachingDeviceParameters()
        {
        }

        /// <summary>
        /// Initialises an instance of <see cref="T:FlushDeviceBuffers"/>.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="reads">if set to <c>true</c> [reads].</param>
        /// <param name="writes">if set to <c>true</c> [writes].</param>
        public FlushCachingDeviceParameters(bool reads, bool writes, DeviceId deviceId)
            : base(reads, writes, deviceId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlushCachingDeviceParameters"/> class.
        /// </summary>
        /// <param name="isForCheckPoint">if set to <c>true</c> [is for check point].</param>
        public FlushCachingDeviceParameters(bool isForCheckPoint)
            : base(false, true)
        {
            IsForCheckPoint = isForCheckPoint;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets a value indicating whether this instance is for check point.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is for check point; otherwise, <c>false</c>.
        /// </value>
        public bool IsForCheckPoint
        {
            get;
            private set;
        }
        #endregion
    }
}