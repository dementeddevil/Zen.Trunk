using Autofac;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// Registers a virtual buffer factory with the autofac container.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="reservationMb">The reservation mb.</param>
        /// <returns></returns>
        public static ContainerBuilder WithVirtualBufferFactory(
            this ContainerBuilder builder, int bufferSize = 8192, int reservationMb = 32)
        {
            builder.RegisterType<VirtualBufferFactory>()
                .WithParameter("bufferSize", bufferSize)
                .WithParameter("reservationMb", reservationMb)
                .As<IVirtualBufferFactory>()
                .SingleInstance();
            return builder;
        }

        /// <summary>
        /// Registers a buffer device factory with the autofac container.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static ContainerBuilder WithBufferDeviceFactory(this ContainerBuilder builder)
        {
            builder.RegisterType<BufferDeviceFactory>()
                .As<IBufferDeviceFactory>()
                .SingleInstance();
            return builder;
        }
    }
}
