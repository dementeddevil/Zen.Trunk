using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage
{
    public class BufferDeviceFactory : IBufferDeviceFactory
    {
        private readonly IVirtualBufferFactory _bufferFactory;

        public BufferDeviceFactory(IVirtualBufferFactory bufferFactory)
        {
            _bufferFactory = bufferFactory;
        }

        public ISingleBufferDevice CreateSingleBufferDevice(
            string name, string pathname, bool isPrimary, bool enableScatterGatherIo)
        {
            return new SingleBufferDevice(
                _bufferFactory, isPrimary, name, pathname, enableScatterGatherIo);
        }

        public IMultipleBufferDevice CreateMultipleBufferDevice(bool enableScatterGatherIo)
        {
            return new MultipleBufferDevice(_bufferFactory, enableScatterGatherIo);
        }
    }
}
