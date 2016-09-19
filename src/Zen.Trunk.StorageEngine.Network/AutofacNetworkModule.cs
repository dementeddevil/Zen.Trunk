using Autofac;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.Network;

namespace Zen.Trunk
{
    public class AutofacNetworkModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .Register(
                    context =>
                    {
                        var sessionManager = context.Resolve<ISessionManager>();
                        var masterDatabase = context.Resolve<MasterDatabaseDevice>();
                        return new Connection(sessionManager.CreateSession(), masterDatabase);
                    })
                .As<IConnection>();
        }
    }
}
