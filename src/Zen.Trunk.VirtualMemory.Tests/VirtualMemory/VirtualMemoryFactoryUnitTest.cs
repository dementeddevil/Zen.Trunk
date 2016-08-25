using Autofac;
using Zen.Trunk.Storage;

namespace Zen.Trunk.VirtualMemory
{
    public class VirtualMemoryFactoryUnitTest : AutofacContainerUnitTest
    {
        protected override void InitializeContainerBuilder(ContainerBuilder builder)
        {
            base.InitializeContainerBuilder(builder);

            builder
                .WithVirtualBufferFactory()
                .WithBufferDeviceFactory();
        }
    }
}