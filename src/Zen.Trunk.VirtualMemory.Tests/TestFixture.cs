using System;
using System.Threading.Tasks;
using Autofac;
using AutofacSerilogIntegration;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Serilog;

namespace Zen.Trunk.VirtualMemory.Tests
{
    public class TestFixture : IDisposable
    {
        private readonly Lazy<ILifetimeScope> _scope;
        private TempFileTracker _globalTracker;

        protected TestFixture()
        {
            _scope = new Lazy<ILifetimeScope>(InitializeScope);
        }

        ~TestFixture()
        {
            Dispose(false);
        }

        public ILifetimeScope Scope => _scope.Value;

        public TempFileTracker GlobalTracker => _globalTracker ?? (_globalTracker = new TempFileTracker());

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void InitializeContainerBuilder(ContainerBuilder builder)
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_scope.IsValueCreated)
                {
                    var telemetryClient = new TelemetryClient(_scope.Value.Resolve<TelemetryConfiguration>());
                    telemetryClient.Flush();
                    Task.Delay(5000).GetAwaiter().GetResult();

                    _scope.Value.Dispose();
                }

                _globalTracker?.Dispose();
            }

            _globalTracker = null;
        }

        protected virtual LoggerConfiguration CreateLoggerConfiguration(TelemetryConfiguration telemetryConfiguration)
        {
            return new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .MinimumLevel.Verbose()
                .WriteTo.Debug(Serilog.Events.LogEventLevel.Verbose, "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Properties}{NewLine}{Exception}");
            //.WriteTo.Trace()
            //.WriteTo.ApplicationInsights(
            //    telemetryConfiguration,
            //    TelemetryConverter.Traces);
        }

        private ILifetimeScope InitializeScope()
        {
            var telemetryConfiguration = new TelemetryConfiguration();
            telemetryConfiguration.ConnectionString = "InstrumentationKey=d47e9f15-e0a7-4c11-9585-bafdd12911fb";
            telemetryConfiguration.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());
            telemetryConfiguration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            QuickPulseTelemetryProcessor processor = null;
            telemetryConfiguration.TelemetryProcessorChainBuilder
                .Use(
                    next =>
                    {
                        processor = new QuickPulseTelemetryProcessor(next);
                        return processor;
                    })
                .Build();

            var quickPulse = new QuickPulseTelemetryModule();
            quickPulse.Initialize(telemetryConfiguration);
            quickPulse.RegisterTelemetryProcessor(processor);

            var dependencyTrackingModule = new DependencyTrackingTelemetryModule();
            dependencyTrackingModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            dependencyTrackingModule.Initialize(telemetryConfiguration);

            var logger = CreateLoggerConfiguration(telemetryConfiguration).CreateLogger();
            Log.Logger = logger;

            var builder = new ContainerBuilder();
            builder.RegisterInstance(telemetryConfiguration);
            builder.RegisterLogger(logger);
            InitializeContainerBuilder(builder);
            return builder.Build();
        }
    }
}
