using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Xunit;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.VirtualMemory
{
    public class AutofacVirtualMemoryUnitTests : AutofacContainerUnitTests
    {
        public IVirtualBufferFactory BufferFactory => Scope.Resolve<IVirtualBufferFactory>();

        public IBufferDeviceFactory BufferDeviceFactory => Scope.Resolve<IBufferDeviceFactory>();

        protected override void InitializeContainerBuilder(ContainerBuilder builder)
        {
            base.InitializeContainerBuilder(builder);

            builder
                .WithVirtualBufferFactory()
                .WithBufferDeviceFactory();
        }
    }
}