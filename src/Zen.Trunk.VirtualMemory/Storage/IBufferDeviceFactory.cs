namespace Zen.Trunk.Storage
{
    public interface IBufferDeviceFactory
    {
        IMultipleBufferDevice CreateMultipleBufferDevice(bool enableScatterGatherIo);

        ISingleBufferDevice CreateSingleBufferDevice(string name, string pathname, bool isPrimary, bool enableScatterGatherIo);
    }
}