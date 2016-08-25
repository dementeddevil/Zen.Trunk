using Autofac;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Storage
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder WithVirtualBufferFactory(
            this ContainerBuilder builder, int bufferSize = 8192, int reservationMB = 32)
        {
            builder.RegisterType<VirtualBufferFactory>()
                .WithParameter("bufferSize", bufferSize)
                .WithParameter("reservationMB", reservationMB)
                .As<IVirtualBufferFactory>()
                .SingleInstance();
            return builder;
        }

        public static ContainerBuilder WithBufferDeviceFactory(this ContainerBuilder builder)
        {
            builder.RegisterType<BufferDeviceFactory>()
                .As<IBufferDeviceFactory>()
                .SingleInstance();
            return builder;
        }
    }
}
