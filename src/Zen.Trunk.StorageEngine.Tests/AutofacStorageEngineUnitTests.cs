using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    public class AutofacStorageEngineUnitTests : AutofacContainerUnitTests
    {
        public static readonly DatabaseId PrimaryDatabaseId = new DatabaseId(1);

        protected override void InitializeContainerBuilder(ContainerBuilder builder)
        {
            base.InitializeContainerBuilder(builder);

            builder
                .WithVirtualBufferFactory()
                .WithBufferDeviceFactory()
                .WithGlobalLockManager()
                .WithDatabaseLockManager(PrimaryDatabaseId);
        }
    }
}
