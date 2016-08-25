﻿using Autofac;
using Zen.Trunk.Storage.IO;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    public class StorageEngineBuilder : ContainerBuilder
    {
        public StorageEngineBuilder WithVirtualBufferFactory(int reservationMB = 32, int bufferSize = 8192)
        {
            this.RegisterType<VirtualBufferFactory>()
                .WithParameter("reservationMB", reservationMB)
                .WithParameter("bufferSize", bufferSize)
                .As<IVirtualBufferFactory>()
                .SingleInstance();
            return this;
        }

        public StorageEngineBuilder WithGlobalLockManager()
        {
            this.RegisterType<GlobalLockManager>()
                .As<IGlobalLockManager>()
                .SingleInstance();
            return this;
        }

        public StorageEngineBuilder WithDatabaseLockManager(DatabaseId dbId)
        {
            this.RegisterType<DatabaseLockManager>()
                .WithParameter("dbId", dbId)
                .As<IDatabaseLockManager>();
            return this;
        }
    }
}
