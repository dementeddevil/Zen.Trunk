using System;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="FlushParameters" />
    [CLSCompliant(false)]
    public class FlushDeviceParameters : FlushParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initialises an instance of <see cref="T:FlushDeviceParameters" />.
        /// </summary>
        public FlushDeviceParameters()
        {
        }

        /// <summary>
        /// Initialises an instance of <see cref="T:FlushDeviceParameters" />.
        /// </summary>
        /// <param name="reads">if set to <c>true</c> [reads].</param>
        /// <param name="writes">if set to <c>true</c> [writes].</param>
        public FlushDeviceParameters(bool reads, bool writes)
            : base(reads, writes)
        {
        }

        /// <summary>
        /// Initialises an instance of <see cref="T:FlushDeviceParameters" />.
        /// </summary>
        /// <param name="reads">if set to <c>true</c> [reads].</param>
        /// <param name="writes">if set to <c>true</c> [writes].</param>
        /// <param name="deviceId">The device id.</param>
        public FlushDeviceParameters(bool reads, bool writes, DeviceId deviceId)
            : base(reads, writes)
        {
            DeviceId = deviceId;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets a value indicating whether all devices are to be flushed.
        /// </summary>
        /// <value>
        /// <c>true</c> to flush all devices; otherwise, <c>false</c>.
        /// </value>
        public bool AllDevices => (DeviceId == DeviceId.Zero);

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        /// <value>The device id.</value>
        public DeviceId DeviceId { get; }
        #endregion
    }
}