using System.IO;
using Autofac;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.RollingFile;
using Topshelf;
using Topshelf.Autofac;
using Topshelf.ServiceConfigurators;
using Zen.Trunk.Network;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Service
{
    public class DatabasePathInformation
    {
        public string DataPathName { get; set; }

        public string LogPathName { get; set; }

        public string ErrorPathName { get; set; }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            var exitCodeObject = HostFactory
                .New(
                    serviceConfig =>
                    {
                        // We need to drive the command line processor and pull pathnames
                        var pathInfo = new DatabasePathInformation();
                        serviceConfig.AddCommandLineDefinition(
                            "D",
                            path =>
                            {
                                pathInfo.DataPathName = path;
                            });
                        serviceConfig.AddCommandLineDefinition(
                            "L",
                            path =>
                            {
                                pathInfo.LogPathName = path;
                            });
                        serviceConfig.AddCommandLineDefinition(
                            "E",
                            path =>
                            {
                                pathInfo.ErrorPathName = path;
                            });
                        serviceConfig.ApplyCommandLine();
                        serviceConfig.EnablePauseAndContinue();
                        serviceConfig.SetServiceName("ZENTRUNKSERVER");

                        // Initialise logging framework
                        var errorLogFolder = Path.GetDirectoryName(pathInfo.ErrorPathName);
                        var errorFilename = Path.GetFileNameWithoutExtension(pathInfo.ErrorPathName);
                        var errorExtension = Path.GetExtension(pathInfo.ErrorPathName);
                        var errorLogPattern = $"{errorLogFolder}{errorFilename}-" + "{Date}" + errorExtension;
                        var rollingFileSink = new RollingFileSink(errorLogPattern, new JsonFormatter(), null, null);

                        var containerBuilder = new ContainerBuilder();
                        var globalLoggingSwitch = new LoggingLevelSwitch(LogEventLevel.Warning);
                        var virtualMemoryLoggingSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
                        var dataMemoryLoggingSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
                        var lockingLoggingSwitch = new LoggingLevelSwitch(LogEventLevel.Error);
                        var logWriterLoggingSwitch = new LoggingLevelSwitch(LogEventLevel.Warning);
                        containerBuilder
                            .RegisterInstance(globalLoggingSwitch)
                            .Named<LoggingLevelSwitch>("GlobalLoggingSwitch");
                        containerBuilder
                            .RegisterInstance(virtualMemoryLoggingSwitch)
                            .Named<LoggingLevelSwitch>("VirtualMemoryLoggingSwitch");
                        containerBuilder
                            .RegisterInstance(dataMemoryLoggingSwitch)
                            .Named<LoggingLevelSwitch>("DataLoggingSwitch");
                        containerBuilder
                            .RegisterInstance(lockingLoggingSwitch)
                            .Named<LoggingLevelSwitch>("LockingLoggingSwitch");
                        containerBuilder
                            .RegisterInstance(logWriterLoggingSwitch)
                            .Named<LoggingLevelSwitch>("LogWriterLoggingSwitch");
                        var loggerConfig = new LoggerConfiguration()
                            .WriteTo.Sink(rollingFileSink)
                            .Enrich.FromLogContext()
                            .MinimumLevel.ControlledBy(globalLoggingSwitch)
                            .MinimumLevel.Override(typeof(VirtualPageId).Namespace, virtualMemoryLoggingSwitch)
                            .MinimumLevel.Override(typeof(DataPage).Namespace, dataMemoryLoggingSwitch)
                            .MinimumLevel.Override(typeof(IGlobalLockManager).Namespace, lockingLoggingSwitch)
                            .MinimumLevel.Override(typeof(LogPage).Namespace, logWriterLoggingSwitch);
                        serviceConfig.UseSerilog(loggerConfig);

                        // Initialise IoC container
                        var container = InitializeAutofacContainer(containerBuilder);
                        serviceConfig.UseAutofacContainer(container);

                        serviceConfig
                            .Service(
                                (ServiceConfigurator<TrunkStorageEngineService> sc) =>
                                {
                                    sc.ConstructUsing(() => container
                                        .Resolve<TrunkStorageEngineService>(
                                            new NamedParameter("pathInformation", pathInfo)));
                                    sc.WhenStarted(service => service.Start());
                                    sc.WhenStopped(service => service.Stop());
                                });
                    })
                .Run();
        }

        private static ILifetimeScope InitializeAutofacContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<TrunkStorageEngineService>();

            // Register master database device
            containerBuilder.RegisterType<MasterDatabaseDevice>()
                .SingleInstance()
                .AsSelf();

            // Register temporary database device
            containerBuilder.RegisterType<TemporaryDatabaseDevice>()
                .SingleInstance()
                .AsSelf();

            // Register network support
            containerBuilder.RegisterModule<AutofacNetworkModule>();

            // Finally build the container
            return containerBuilder.Build();
        }
    }
}
