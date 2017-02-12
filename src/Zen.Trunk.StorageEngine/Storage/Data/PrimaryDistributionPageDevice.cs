using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Data.DistributionPageDevice" />
    public class PrimaryDistributionPageDevice : DistributionPageDevice
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrimaryDistributionPageDevice"/> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        public PrimaryDistributionPageDevice(DeviceId deviceId)
            : base(deviceId)
        {
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