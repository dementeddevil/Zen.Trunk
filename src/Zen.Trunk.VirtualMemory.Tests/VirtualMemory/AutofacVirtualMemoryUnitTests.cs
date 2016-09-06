using Autofac;
using Zen.Trunk.Storage;

namespace Zen.Trunk.VirtualMemory
{
    public class AutofacVirtualMemoryUnitTests : AutofacContainerUnitTests
    {
        public IVirtualBufferFactory BufferFactory => Scope.Resolve<IVirtualBufferFactory>();

        public IBufferDeviceFactory BufferDeviceFactory => Scope.Resolve<IBufferDeviceFactory>();

        protected override void InitializeContainerBuilder(ContainerBuilder builder)
        {
            base.InitializeContainerBuilder(builder);

            builder
                .WithVirtualBufferFactory()
                .WithBufferDeviceFactory();
        }
    }
}