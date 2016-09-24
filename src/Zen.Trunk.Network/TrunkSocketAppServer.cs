using Autofac;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

namespace Zen.Trunk.Network
{
    /// <summary>
    /// <c>TrunkSocketAppServer</c> handles the network protocol socket server.
    /// </summary>
    /// <seealso cref="SuperSocket.SocketBase.AppServer{TrunkSocketAppSession}" />
    /// <remarks>
    /// All messages sent to the trunk server consist of the following
    /// Command (4 bytes ascii)
    /// Length (2 bytes)
    /// Payload (variable length)
    /// </remarks>
    public class TrunkSocketAppServer : AppServer<TrunkSocketAppSession, BinaryRequestInfo>
    {
        private readonly ILifetimeScope _lifetimeScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkSocketAppServer"/> class.
        /// </summary>
        /// <param name="lifetimeScope">The autofac lifetime scope.</param>
        public TrunkSocketAppServer(ILifetimeScope lifetimeScope)
            : base(new DefaultReceiveFilterFactory<TrunkReceiveFilter, BinaryRequestInfo>())
        {
            _lifetimeScope = lifetimeScope;
        }

        /// <summary>
        /// create a new TAppSession instance, you can override it to create the session instance in your own way
        /// </summary>
        /// <param name="socketSession">the socket session.</param>
        /// <returns>
        /// the new created session instance
        /// </returns>
        protected override TrunkSocketAppSession CreateAppSession(ISocketSession socketSession)
        {
            // Create session object via Autofac IoC lifetime scope.
            return _lifetimeScope.Resolve<TrunkSocketAppSession>();
        }
    }
}