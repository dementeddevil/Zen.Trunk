using System;
using Autofac;

namespace Zen.Trunk.VirtualMemory.Tests
{
    public class TestFixture : IDisposable
    {
        private TempFileTracker _globalTracker;

        protected TestFixture()
        {
            InitializeScope();
        }

        ~TestFixture()
        {
            Dispose(false);
        }

        public ILifetimeScope Scope { get; private set; }

        public TempFileTracker GlobalTracker => _globalTracker ?? (_globalTracker = new TempFileTracker());

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void InitializeScope()
        {
            var builder = new ContainerBuilder();
            InitializeContainerBuilder(builder);
            Scope = builder.Build();
        }

        protected virtual void InitializeContainerBuilder(ContainerBuilder builder)
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Scope?.Dispose();
                _globalTracker?.Dispose();
            }

            Scope = null;
            _globalTracker = null;
        }
    }
}
