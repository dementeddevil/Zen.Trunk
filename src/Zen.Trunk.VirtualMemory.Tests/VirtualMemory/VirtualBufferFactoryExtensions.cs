using Zen.Trunk.Storage;

namespace Zen.Trunk.VirtualMemory
{
    public static class VirtualBufferFactoryExtensions
    {
        public static IVirtualBuffer AllocateAndFill(this IVirtualBufferFactory bufferFactory, byte value)
        {
            var buffer = bufferFactory.AllocateBuffer();
            using (var stream = buffer.GetBufferStream(0, bufferFactory.BufferSize, true))
            {
                for (var index = 0; index < bufferFactory.BufferSize; ++index)
                {
                    stream.WriteByte(value);
                }
            }
            return buffer;
        }
    }
}