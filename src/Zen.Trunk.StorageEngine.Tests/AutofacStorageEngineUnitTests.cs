using Autofac;

namespace Zen.Trunk.Storage
{
    public class AutofacStorageEngineUnitTests : AutofacContainerUnitTests
    {
        public static readonly DatabaseId PrimaryDatabaseId = new DatabaseId(1);

        protected override void InitializeContainerBuilder(ContainerBuilder builder)
        {
            base.InitializeContainerBuilder(builder);

            builder
                .WithVirtualBufferFactory()
                .WithBufferDeviceFactory()
                .WithGlobalLockManager()
                .WithDatabaseLockManager(PrimaryDatabaseId);
        }
    }
}
