using System;
using Autofac;

namespace Zen.Trunk
{
    public class AutofacContainerUnitTests : IDisposable
    {
        protected AutofacContainerUnitTests()
        {
            InitializeScope();
        }

        ~AutofacContainerUnitTests()
        {
            Dispose(false);
        }

        protected ILifetimeScope Scope { get; private set; }

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
            }

            Scope = null;
        }
    }
}
