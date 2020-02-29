using System;
using Autofac;
using AutofacSerilogIntegration;
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
                    _scope.Value.Dispose();
                }

                _globalTracker?.Dispose();
            }

            _globalTracker = null;
        }

        private ILifetimeScope InitializeScope()
        {
            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .WriteTo.Debug()
                .WriteTo.Trace()
                .CreateLogger();
            Log.Logger = logger;

            var builder = new ContainerBuilder();
            builder.RegisterLogger(logger);
            InitializeContainerBuilder(builder);
            return builder.Build();
        }
    }
}
