using System;
using Autofac;
using Serilog;
using Serilog.Enrichers;
using Serilog.Events;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.VirtualMemory;
using Zen.Trunk.VirtualMemory.Tests;

namespace Zen.Trunk.Storage
{
    public class StorageEngineTestFixture : TestFixture
    {
        private IDisposable _ambientSessionScope;

        public static readonly DatabaseId PrimaryDatabaseId = new DatabaseId(1);

        public StorageEngineTestFixture()
        {
            var config = new LoggerConfiguration();
            Serilog.Log.Logger = config
                .Enrich.With<ThreadIdEnricher>()
                .Enrich.With<TransactionEnricher>()
                .MinimumLevel.Verbose()
                .WriteTo.Debug(
                    LogEventLevel.Verbose,
                    "[{Timestamp:HH:mm:ss} {Level:u3} {SessionId} {TransactionId}] {Message:lj}")
                .CreateLogger();
            _ambientSessionScope = TrunkSessionContext.SwitchSessionContext(
                new TrunkSession(new SessionId(11002), TimeSpan.FromSeconds(60)));
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
