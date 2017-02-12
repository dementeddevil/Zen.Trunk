using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.RollingFile;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using Zen.Trunk.Network;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.Configuration;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Service
{
    /// <summary>
    /// <c>TrunkStorageEngineService</c> implements the interface between
    /// our service and the Windows Service Control Manager (SCM)
    /// </summary>
    /// <seealso cref="InstanceServiceBase" />
    public partial class TrunkStorageEngineService : InstanceServiceBase
    {
        private Logger _globalLogger;
        private ILifetimeScope _globaLifetimeScope;
        private string _masterDataPathname;
        private string _masterLogPathname;
        private string _errorLogPathname;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkStorageEngineService"/> class.
        /// </summary>
        public TrunkStorageEngineService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <value>
        /// The command line arguments.
        /// </value>
        protected override IEnumerable<CommandLineArgument> CommandLineArguments
        {
            get
            {
                // TODO: Add our special command line arguments to support
                //  alternate startup options (so we can init DB from command line
                //  without starting the service)
                return base.CommandLineArguments.Concat(
                    new[] 
                    {
                        new CommandLineArgument("D", additionalArgumentCount: 1),
                        new CommandLineArgument("L", additionalArgumentCount: 1),
                        new CommandLineArgument("E", additionalArgumentCount: 1)
                    });
            }
        }

        /// <summary>
        /// Processes the command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="value">The value.</param>
        /// <param name="additionalParameters">The additional parameters.</param>
        protected override void ProcessCommand(CommandLineArgument command, string value, string[] additionalParameters)
        {
            base.ProcessCommand(command, value, additionalParameters);

            if (command.ShortName == "D")
            {
                _masterDataPathname = additionalParameters[0];
            }
            else if (command.ShortName == "L")
            {
                _masterLogPathname = additionalParameters[0];
            }
            else if (command.ShortName == "E")
            {
                _errorLogPathname = additionalParameters[0];
            }
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
            var config = new TrunkConfigurationManager(ServiceName, false);
            var loggingSection = config.Root[ConfigurationNames.Logging.Section];

            // Determine locations for our base files
            if(string.IsNullOrEmpty(_masterDataPathname))
            {
                _masterDataPathname = config.Root.GetInstanceValue(
                    ConfigurationNames.MasterDataPathname, string.Empty);
            }
            if (string.IsNullOrEmpty(_masterLogPathname))
            {
                _masterLogPathname = config.Root.GetInstanceValue(
                    ConfigurationNames.MasterLogPathname, string.Empty);
            }
            if (string.IsNullOrEmpty(_errorLogPathname))
            {
                _errorLogPathname = config.Root.GetInstanceValue(
                    ConfigurationNames.ErrorLogPathname, string.Empty);
            }

            // Initialise logging framework
            var errorLogFolder = Path.GetDirectoryName(_errorLogPathname);
            var errorFilename = Path.GetFileNameWithoutExtension(_errorLogPathname);
            var errorExtension = Path.GetExtension(_errorLogPathname);
            var errorLogPattern = $"{errorLogFolder}{errorFilename}-" + "{Date}" + errorExtension;
            var rollingFileSink = new RollingFileSink(errorLogPattern, new JsonFormatter(), null, null);
            var globalLoggingSwitch = new LoggingLevelSwitch(loggingSection.GetValue(
                ConfigurationNames.Logging.GlobalLoggingSwitch, LogEventLevel.Warning));
            var virtualMemoryLoggingSwitch = new LoggingLevelSwitch(loggingSection.GetValue(
                ConfigurationNames.Logging.VirtualMemoryLoggingSwitch, LogEventLevel.Information));
            var dataMemoryLoggingSwitch = new LoggingLevelSwitch(loggingSection.GetValue(
                ConfigurationNames.Logging.DataLoggingSwitch, LogEventLevel.Information));
            var lockingLoggingSwitch = new LoggingLevelSwitch(loggingSection.GetValue(
                ConfigurationNames.Logging.LockingLoggingSwitch, LogEventLevel.Error));
            var logWriterLoggingSwitch = new LoggingLevelSwitch(loggingSection.GetValue(
                ConfigurationNames.Logging.LogWriterLoggingSwitch, LogEventLevel.Warning));
            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Sink(rollingFileSink)
                .Enrich.WithProperty("ServiceName", ServiceName)
                .MinimumLevel.ControlledBy(globalLoggingSwitch)
                .MinimumLevel.Override(typeof(VirtualPageId).Namespace, virtualMemoryLoggingSwitch)
                .MinimumLevel.Override(typeof(DataPage).Namespace, dataMemoryLoggingSwitch)
                .MinimumLevel.Override(typeof(IGlobalLockManager).Namespace, lockingLoggingSwitch)
                .MinimumLevel.Override(typeof(LogPage).Namespace, logWriterLoggingSwitch);
            _globalLogger = loggerConfig.CreateLogger();

            // Initialise IoC container
            InitializeAutofacContainer(config);

            // Initiate deferred service startup
            Task.Run(DeferredServiceStartupAsync);
        }

        /// <summary>
        /// Executes when a Stop command is sent to the service by the Service
        /// Control Manager (SCM).
        /// Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
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
            _globaLifetimeScope.Dispose();
            _globaLifetimeScope = null;
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
            var temporaryDatabase = _globaLifetimeScope.Resolve<TemporaryDatabaseDevice>();
            var tempDbFolder = Path.GetDirectoryName(_masterDataPathname);
            //await temporaryDatabase.

            // Mount master DB
            var masterDatabase = _globaLifetimeScope.Resolve<MasterDatabaseDevice>();
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
            var server = _globaLifetimeScope.Resolve<TrunkSocketAppServer>();
            server.Setup(
                rootConfig, serverConfig,
                logFactory: new SuperSocketLogFactory());

            // Start the server
            server.Start();
        }

        private void StopNetworkProtocolServer()
        {
            // Shutdown the server
            var server = _globaLifetimeScope.Resolve<TrunkSocketAppServer>();
            server.Stop();
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
                    configurationManager.Root[ConfigurationNames.VirtualMemory.Section]
                        .GetValue(ConfigurationNames.VirtualMemory.ReservationInMegaBytes, 1024))
                .WithBufferDeviceFactory();

            // Register master database device
            builder.RegisterType<MasterDatabaseDevice>()
                .SingleInstance()
                .AsSelf();

            // Register temporary database device
            builder.RegisterType<TemporaryDatabaseDevice>()
                .SingleInstance()
                .AsSelf();

            // Register network support
            builder.RegisterModule<AutofacNetworkModule>();

            // Finally build the container
            _globaLifetimeScope = builder.Build();
        }
    }
}
