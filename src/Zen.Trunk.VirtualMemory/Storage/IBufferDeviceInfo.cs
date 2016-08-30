namespace Zen.Trunk.Storage
{
    public interface IBufferDeviceInfo
    {
        DeviceId DeviceId
        {
            get;
        }

        string Name
        {
            get;
        }

        uint PageCount
        {
            get;
        }
    }
}