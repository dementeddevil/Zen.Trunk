﻿using Autofac;
using Zen.Trunk.Storage;

namespace Zen.Trunk.Network
{
    /// <summary>
    /// <c>AutofacNetworkModule</c>
    /// </summary>
    /// <seealso cref="Autofac.Module" />
    public class AutofacNetworkModule : Module
    {
        /// <summary>
        /// Override to add registrations to the container.
        /// </summary>
        /// <param name="builder">The builder through which components can be
        /// registered.</param>
        /// <remarks>
        /// Note that the ContainerBuilder parameter is unique to this module.
        /// </remarks>
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

            // Register SuperSocket session so we gain Ioc injection
            builder.RegisterType<TrunkSocketAppServer>().AsSelf().SingleInstance();
            builder.RegisterType<TrunkSocketAppSession>().AsSelf();
        }
    }
}
