﻿using Autofac;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;

namespace Zen.Trunk.StorageEngine.Service
{
    /// <summary>
    /// <c>TrunkStorageEngineService</c> implements the interface between
    /// our service and the Windows Service Control Manager (SCM)
    /// </summary>
    /// <seealso cref="Zen.Trunk.StorageEngine.Service.InstanceServiceBase" />
    public partial class TrunkStorageEngineService : InstanceServiceBase
    {
        private Logger _globalLogger;
        private ILifetimeScope _globaLifetimeScope;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkStorageEngineService"/> class.
        /// </summary>
        public TrunkStorageEngineService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Executes when a Start command is sent to the service by the Service
        /// Control Manager (SCM) or when the operating system starts (for a
        /// service that starts automatically).
        /// Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            // Initialise our custom configuration system (registry based)
            var config = new TrunkConfigurationManager(ServiceName);
            var loggingSection = config.Root["Logging"];

            // Initialise logging framework
            var globalLoggingSwitch = new LoggingLevelSwitch(loggingSection.GetValue("Global", LogEventLevel.Warning));
            var virtualMemoryLoggingSwitch = new LoggingLevelSwitch(loggingSection.GetValue("VirtualMemory", LogEventLevel.Information));
            var dataMemoryLoggingSwitch = new LoggingLevelSwitch(loggingSection.GetValue("Data", LogEventLevel.Information));
            var lockingLoggingSwitch = new LoggingLevelSwitch(loggingSection.GetValue("Locking", LogEventLevel.Error));
            var logWriterLoggingSwitch = new LoggingLevelSwitch(loggingSection.GetValue("LogWriter", LogEventLevel.Warning));
            var loggerConfig = new LoggerConfiguration()
                .Enrich.WithProperty("ServiceName", ServiceName)
                .MinimumLevel.ControlledBy(globalLoggingSwitch)
                .MinimumLevel.Override(typeof(VirtualPageId).Namespace, virtualMemoryLoggingSwitch)
                .MinimumLevel.Override(typeof(DataPage).Namespace, dataMemoryLoggingSwitch)
                .MinimumLevel.Override(typeof(IGlobalLockManager).Namespace, lockingLoggingSwitch)
                .MinimumLevel.Override(typeof(LogPage).Namespace, logWriterLoggingSwitch);
            _globalLogger = loggerConfig.CreateLogger();

            // Initialise IoC container
            InitializeAutofacContainer(config);

            // TODO: Start up database recovery thread
        }

        /// <summary>
        /// Executes when a Stop command is sent to the service by the Service
        /// Control Manager (SCM).
        /// Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
            // TODO: Shutdown all devices

            // Teardown IoC
            _globaLifetimeScope.Dispose();
            _globaLifetimeScope = null;
        }

        private void InitializeAutofacContainer(ITrunkConfigurationManager configurationManager)
        {
            var builder = new ContainerBuilder();

            // Register configuration manager instance
            builder.RegisterInstance(configurationManager).As<ITrunkConfigurationManager>();

            // Register virtual memory and buffer device support
            builder
                .WithVirtualBufferFactory(
                    8192,
                    configurationManager.Root["VirtualMemory"].GetValue("ReservationInMegaBytes", 1024))
                .WithBufferDeviceFactory();

            // Register master database device
            builder.RegisterType<MasterDatabaseDevice>()
                .SingleInstance()
                .AsSelf();

            // Register network support
            builder.RegisterModule<AutofacNetworkModule>();

            // Finally build the container
            _globaLifetimeScope = builder.Build();
        }
    }
}
