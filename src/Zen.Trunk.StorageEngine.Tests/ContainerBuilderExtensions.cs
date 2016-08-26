using Autofac;
using Zen.Trunk.Storage.IO;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder WithGlobalLockManager(
            this ContainerBuilder builder)
        {
            builder.RegisterType<GlobalLockManager>()
                .As<IGlobalLockManager>()
                .SingleInstance();
            return builder;
        }

        public static ContainerBuilder WithDatabaseLockManager(
            this ContainerBuilder builder, DatabaseId dbId)
        {
            builder.RegisterType<DatabaseLockManager>()
                .WithParameter("dbId", dbId)
                .As<IDatabaseLockManager>();
            return builder;
        }
    }
}
