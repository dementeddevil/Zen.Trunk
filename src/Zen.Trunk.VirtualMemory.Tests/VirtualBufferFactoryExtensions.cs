using Zen.Trunk.Storage;
using Zen.Trunk.Storage.IO;

public static class VirtualBufferFactoryExtensions
{
    public static VirtualBuffer AllocateAndFill(this IVirtualBufferFactory bufferFactory, byte value)
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