using System;
using Autofac;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    public class AutofacStorageEngineUnitTests : AutofacContainerUnitTests
    {
        private IDisposable _ambientSessionScope;

        public static readonly DatabaseId PrimaryDatabaseId = new DatabaseId(1);

        public AutofacStorageEngineUnitTests()
        {
            _ambientSessionScope = AmbientSessionContext.SwitchSessionContext(
                new AmbientSession(new SessionId(11002), TimeSpan.FromSeconds(60)));
        }

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ambientSessionScope.Dispose();
                _ambientSessionScope = null;
            }

            base.Dispose(disposing);
        }
    }
}
