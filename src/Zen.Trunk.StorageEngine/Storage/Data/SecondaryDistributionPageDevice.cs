using System;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Data.DistributionPageDevice" />
    public class SecondaryDistributionPageDevice : DistributionPageDevice
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecondaryDistributionPageDevice"/> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <exception cref="ArgumentException">deviceId</exception>
        public SecondaryDistributionPageDevice(DeviceId deviceId)
            : base(deviceId)
        {
            if (deviceId == DeviceId.Zero ||
                deviceId == DeviceId.Primary)
            {
                throw new ArgumentException("deviceId");
            }
        }

        /// <summary>
        /// Gets the distribution page offset.
        /// </summary>
        /// <value>
        /// The distribution page offset.
        /// </value>
        public override uint DistributionPageOffset => 1;
    }
}