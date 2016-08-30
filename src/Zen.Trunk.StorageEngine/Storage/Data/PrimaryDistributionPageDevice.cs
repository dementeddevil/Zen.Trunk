namespace Zen.Trunk.Storage.Data
{
    public class PrimaryDistributionPageDevice : DistributionPageDevice
    {
        public PrimaryDistributionPageDevice(DeviceId deviceId)
            : base(deviceId)
        {
        }

        public override uint DistributionPageOffset => 1;
    }
}