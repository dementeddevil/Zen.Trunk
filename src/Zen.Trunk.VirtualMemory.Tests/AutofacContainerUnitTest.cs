using System;
using Autofac;

namespace Zen.Trunk
{
    public class AutofacContainerUnitTests : IDisposable
    {
        private TempFileTracker _globalTracker;

        protected AutofacContainerUnitTests()
        {
            InitializeScope();
        }

        ~AutofacContainerUnitTests()
        {
            Dispose(false);
        }

        protected ILifetimeScope Scope { get; private set; }

        protected TempFileTracker GlobalTracker
        {
            get
            {
                if (_globalTracker == null)
                {
                    _globalTracker = new TempFileTracker();
                }
                return _globalTracker;
            }
        }

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
