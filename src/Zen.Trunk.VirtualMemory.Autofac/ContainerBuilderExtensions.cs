using Autofac;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// Registers a virtual buffer factory with the container.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static ContainerBuilder WithVirtualBufferFactory(this ContainerBuilder builder)
        {
            builder.RegisterType<VirtualBufferFactory>()
                .As<IVirtualBufferFactory>()
                .SingleInstance();
            return builder;
        }

        /// <summary>
        /// Registers a virtual buffer factory with the container.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="settings">The settings.</param>
        /// <returns></returns>
        public static ContainerBuilder WithVirtualBufferFactory(
            this ContainerBuilder builder, VirtualBufferFactorySettings settings)
        {
            builder.RegisterType<VirtualBufferFactory>()
                .WithParameter("settings", settings)
                .As<IVirtualBufferFactory>()
                .SingleInstance();
            return builder;
        }

        /// <summary>
        /// Registers a buffer device factory with the container.
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

        /// <summary>
        /// Registers the default system reference clock with the container.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder WithDefaultSystemClock(this ContainerBuilder builder)
        {
            builder.RegisterType<DefaultSystemClock>()
                .As<ISystemClock>()
                .SingleInstance();
            return builder;
        }
    }
}
