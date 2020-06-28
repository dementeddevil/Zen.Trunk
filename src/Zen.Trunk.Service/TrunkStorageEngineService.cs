using System;
using System.Threading.Tasks;
using Autofac;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using Topshelf.Runtime;
using Zen.Trunk.Network;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.Configuration;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Service
{
    /// <summary>
    /// <c>TrunkStorageEngineService</c> implements the interface between
    /// our service and the Windows Service Control Manager (SCM)
    /// </summary>
    public class TrunkStorageEngineService
    {
        private readonly HostSettings _hostSettings;
        private readonly string _masterDataPathname;
        private readonly string _masterLogPathname;

        private ILifetimeScope _globalLifetimeScope;
        private ILifetimeScope _serviceLifetimeScope;
        private IDisposable _serviceNameEnricher;

        private Logger _globalLogger;

        private string _errorLogPathname;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkStorageEngineService"/> class.
        /// </summary>
        public TrunkStorageEngineService(
            ILifetimeScope container,
            HostSettings hostSettings,
            DatabasePathInformation pathInformation)
        {
            _hostSettings = hostSettings;
            _globalLifetimeScope = container;
            _masterDataPathname = pathInformation.DataPathName;
            _masterLogPathname = pathInformation.LogPathName;
            _errorLogPathname = pathInformation.ErrorPathName;

            _serviceLifetimeScope = _serviceLifetimeScope
                .BeginLifetimeScope(
                    builder =>
                    {
                        // Create and register configuration manager bound to this service
                        var configurationManager = new TrunkConfigurationManager(
                            _hostSettings.ServiceName, false);
                        builder.RegisterInstance(configurationManager)
                            .As<ITrunkConfigurationManager>();

                        // Register virtual memory and buffer device support
                        var reservationPageCount = configurationManager
                            .Root[ConfigurationNames.VirtualMemory.Section]
                            .GetValue(ConfigurationNames.VirtualMemory.ReservationPageCount, 4096);
                        var pagesPerCacheBlock = configurationManager
                            .Root[ConfigurationNames.VirtualMemory.Section]
                            .GetValue(ConfigurationNames.VirtualMemory.PagesPerCacheBlock, 8);
                        builder.RegisterInstance(new VirtualBufferFactorySettings(
                            StorageConstants.PageBufferSize, reservationPageCount, pagesPerCacheBlock));
                        builder
                            .WithVirtualBufferFactory()
                            .WithBufferDeviceFactory()
                            .WithDefaultSystemClock();
                    });
        }

        /// <summary>
        /// Executes when a Start command is sent to the service by the Service
        /// Control Manager (SCM) or when the operating system starts (for a
        /// service that starts automatically).
        /// Specifies actions to take when the service starts.
        /// </summary>
        public void Start()
        {
            _serviceNameEnricher = LogContext
                .Push(new PropertyEnricher("ServiceName", _hostSettings.ServiceName));

            // Initialise our custom configuration system (registry based)
            var config = _serviceLifetimeScope.Resolve<ITrunkConfigurationManager>();
            var loggingSection = config.Root[ConfigurationNames.Logging.Section];

            // Allow instance specific settings to override logging switches
            var globalLoggingSwitch = _serviceLifetimeScope
                .ResolveNamed<LoggingLevelSwitch>("GlobalLoggingSwitch");
            globalLoggingSwitch.MinimumLevel = loggingSection.GetValue(
                ConfigurationNames.Logging.GlobalLoggingSwitch, globalLoggingSwitch.MinimumLevel);

            var virtualMemoryLoggingSwitch = _serviceLifetimeScope
                .ResolveNamed<LoggingLevelSwitch>("VirtualMemoryLoggingSwitch");
            virtualMemoryLoggingSwitch.MinimumLevel = loggingSection.GetValue(
                ConfigurationNames.Logging.GlobalLoggingSwitch, virtualMemoryLoggingSwitch.MinimumLevel);

            var dataMemoryLoggingSwitch = _serviceLifetimeScope
                .ResolveNamed<LoggingLevelSwitch>("DataLoggingSwitch");
            dataMemoryLoggingSwitch.MinimumLevel = loggingSection.GetValue(
                ConfigurationNames.Logging.GlobalLoggingSwitch, dataMemoryLoggingSwitch.MinimumLevel);

            var lockingLoggingSwitch = _serviceLifetimeScope
                .ResolveNamed<LoggingLevelSwitch>("LockingLoggingSwitch");
            lockingLoggingSwitch.MinimumLevel = loggingSection.GetValue(
                ConfigurationNames.Logging.GlobalLoggingSwitch, lockingLoggingSwitch.MinimumLevel);

            var logWriterLoggingSwitch = _serviceLifetimeScope
                .ResolveNamed<LoggingLevelSwitch>("LogWriterLoggingSwitch");
            logWriterLoggingSwitch.MinimumLevel = loggingSection.GetValue(
                ConfigurationNames.Logging.GlobalLoggingSwitch, logWriterLoggingSwitch.MinimumLevel);

            // Initiate deferred service startup
            Task.Run(DeferredServiceStartupAsync);
        }

        /// <summary>
        /// Executes when a Stop command is sent to the service by the Service
        /// Control Manager (SCM).
        /// Specifies actions to take when a service stops running.
        /// </summary>
        public void Stop()
        {
            // TODO: Notify local "browser" service that this instance is offline

            // Shutdown network server
            StopNetworkProtocolServer();

            // TODO: Shutdown all devices
            // NOTE: Device shutdown must wait for log-writer to complete work
            //  and if the checkpoint writer is in progress then wait for it to
            //  finish (as this will result in faster startup if checkpoint is
            //  viable.

            // TODO: Based on amount of time taken thus far we may wish to wait for
            //  cached pages to be written to disk however this isn't necessary if
            //  the log writer/checkpoint writer processes have completed.

            // Teardown IoC
            _serviceLifetimeScope.Dispose();
            _serviceLifetimeScope = null;
            _globalLifetimeScope.Dispose();
            _globalLifetimeScope = null;
            _serviceNameEnricher.Dispose();
            _serviceNameEnricher = null;
        }

        private async Task DeferredServiceStartupAsync()
        {
            await MountAndOpenSystemDatabasesAsync().ConfigureAwait(false);
            StartNetworkProtocolServer();

            // TODO: Notify local "browser" service that this server is online
        }

        private async Task MountAndOpenSystemDatabasesAsync()
        {
            // Create temp DB
            //var temporaryDatabase = _serviceLifetimeScope.Resolve<TemporaryDatabaseDevice>();
            //var tempDbFolder = Path.GetDirectoryName(_masterDataPathname);
            //await temporaryDatabase.

            // Mount master DB
            var masterDatabase = _serviceLifetimeScope.Resolve<MasterDatabaseDevice>();
            var attachParams = new AttachDatabaseParameters("MASTER");
            attachParams.AddDataFile(
                "PRIMARY",
                new FileSpec
                {
                    FileName = _masterDataPathname
                });
            attachParams.AddLogFile(
                new FileSpec
                {
                    FileName = _masterLogPathname
                });
            await masterDatabase.AttachDatabaseAsync(attachParams).ConfigureAwait(false);
            await masterDatabase.OpenAsync(false).ConfigureAwait(false);
        }

        private void StartNetworkProtocolServer()
        {
            // Setup advanced network operation parameters
            var rootConfig =
                new RootConfig
                {
                    DefaultCulture = "en-gb",
                    Isolation = IsolationMode.None,
                    DisablePerformanceDataCollector = false,
                    PerformanceDataCollectInterval = 15,
                    MaxCompletionPortThreads = 32,
                    MinCompletionPortThreads = 4,
                    MaxWorkingThreads = 32,
                    MinWorkingThreads = 4
                };

            // Setup server configuration parameters
            var serverConfig =
                new ServerConfig
                {
                    ClearIdleSession = true,
                    ClearIdleSessionInterval = 60,      // 1 mins
                    DefaultCulture = "en-gb",
                    IdleSessionTimeOut = 900,           // 15 mins
                    LogAllSocketException = true,
                    LogBasicSessionActivity = true,
                    LogCommand = true,
                    Mode = SocketMode.Tcp,
                    SessionSnapshotInterval = 60,
                    TextEncoding = "UTF-8",
                    Ip = "Any",                         // TODO: Read from configuration
                    Port = 5976                         // TODO: Read from configuration
                };

            // Create and setup trunk socket app server
            var server = _serviceLifetimeScope.Resolve<TrunkSocketAppServer>();
            server.Setup(
                rootConfig, serverConfig,
                logFactory: new SuperSocketLogFactory());

            // Start the server
            server.Start();
        }

        private void StopNetworkProtocolServer()
        {
            // Shutdown the server
            var server = _serviceLifetimeScope.Resolve<TrunkSocketAppServer>();
            server.Stop();
        }
    }
}
