using System;
using Autofac;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Services;
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
            _ambientSessionScope = TrunkSessionContext.SwitchSessionContext(
                new TrunkSession(new SessionId(11002), TimeSpan.FromSeconds(60)));
        }

        public CachingPageBufferDeviceSettings CachingPageBufferDeviceSettings { get; } =
            new CachingPageBufferDeviceSettings();

        protected override void InitializeContainerBuilder(ContainerBuilder builder)
        {
            base.InitializeContainerBuilder(builder);

            builder
                .Register(
                    serviceProvider =>
                    {
                        var eventLogger = new LoggerConfiguration()
                            .Enrich.FromLogContext()
                            .Enrich.WithThreadId()
                            .Enrich.WithThreadName()
                            .Enrich.With<TransactionEnricher>()
                            .WriteTo.ApplicationInsights(
                                serviceProvider.Resolve<TelemetryConfiguration>(),
                                TelemetryConverter.Events)
                            .CreateLogger();
                        return eventLogger;
                    })
                .SingleInstance()
                .Named("EventLogger", typeof(Serilog.Core.Logger));

            builder.RegisterInstance(new VirtualBufferFactorySettings(StorageConstants.PageBufferSize, 4096, 8));
            builder
                .WithVirtualBufferFactory()
                .WithBufferDeviceFactory()
                .WithGlobalLockManager()
                .WithDatabaseLockManager(PrimaryDatabaseId);
            builder.WithDefaultSystemClock();
            builder.RegisterInstance(CachingPageBufferDeviceSettings)
                .AsSelf();
            builder.RegisterType<StorageEngineEventService>().As<IStorageEngineEventService>();
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

        protected override LoggerConfiguration CreateLoggerConfiguration(TelemetryConfiguration telemetryConfiguration)
        {
            return base
                .CreateLoggerConfiguration(telemetryConfiguration)
                .Enrich.With<TransactionEnricher>();
        }
    }
}
