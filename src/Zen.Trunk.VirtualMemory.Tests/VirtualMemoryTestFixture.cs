using Autofac;

namespace Zen.Trunk.VirtualMemory.Tests
{
    public class VirtualMemoryTestFixture : TestFixture
    {
        public IVirtualBufferFactory BufferFactory => Scope.Resolve<IVirtualBufferFactory>();

        public IBufferDeviceFactory BufferDeviceFactory => Scope.Resolve<IBufferDeviceFactory>();

        public bool UseDefaultSystemClock { get; set; } = true;

        protected override void InitializeContainerBuilder(ContainerBuilder builder)
        {
            base.InitializeContainerBuilder(builder);

            if (UseDefaultSystemClock)
            {
                builder.WithDefaultSystemClock();
            }

            builder
                .WithVirtualBufferFactory()
                .WithBufferDeviceFactory();
        }
    }
}