using System;

namespace Zen.Trunk.Storage.Data
{
    public class SecondaryDistributionPageDevice : DistributionPageDevice
    {
        public SecondaryDistributionPageDevice(DeviceId deviceId)
            : base(deviceId)
        {
            if (deviceId == DeviceId.Zero ||
                deviceId == DeviceId.Primary)
            {
                throw new ArgumentException("deviceId");
            }
        }

        public override uint DistributionPageOffset => 1;
    }
}