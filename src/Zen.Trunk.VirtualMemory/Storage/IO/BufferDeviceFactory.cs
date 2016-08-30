namespace Zen.Trunk.Storage.IO
{
    public class BufferDeviceFactory : IBufferDeviceFactory
    {
        private readonly IVirtualBufferFactory _bufferFactory;

        public BufferDeviceFactory(IVirtualBufferFactory bufferFactory)
        {
            _bufferFactory = bufferFactory;
        }

        public ISingleBufferDevice CreateSingleBufferDevice(
            string name, string pathname, uint createPageCount, bool enableScatterGatherIo)
        {
            return new SingleBufferDevice(
                _bufferFactory, name, pathname, createPageCount, enableScatterGatherIo);
        }

        public IMultipleBufferDevice CreateMultipleBufferDevice(bool enableScatterGatherIo)
        {
            return new MultipleBufferDevice(_bufferFactory, this, enableScatterGatherIo);
        }
    }
}
