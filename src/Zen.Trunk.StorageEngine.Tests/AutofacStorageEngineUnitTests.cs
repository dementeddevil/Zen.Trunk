using Autofac;
using Zen.Trunk.Storage.Data;

namespace Zen.Trunk.Storage
{
    public class AutofacStorageEngineUnitTests : AutofacContainerUnitTests
    {
        public static readonly DatabaseId PrimaryDatabaseId = new DatabaseId(1);

        public CachingPageBufferDeviceSettings CachingPageBufferDeviceSettings { get; } =
            new CachingPageBufferDeviceSettings();

        protected override void InitializeContainerBuilder(ContainerBuilder builder)
        {
            base.InitializeContainerBuilder(builder);

            builder
                .WithVirtualBufferFactory()
                .WithBufferDeviceFactory()
                .WithGlobalLockManager()
                .WithDatabaseLockManager(PrimaryDatabaseId);
            builder.RegisterInstance(CachingPageBufferDeviceSettings)
                .AsSelf();
        }
    }
}
