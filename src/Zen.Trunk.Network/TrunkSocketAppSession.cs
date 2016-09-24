using System;
using Autofac;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Network
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="SuperSocket.SocketBase.AppSession{TrunkSocketAppSession, BinaryRequestInfo}" />
    public class TrunkSocketAppSession : AppSession<TrunkSocketAppSession, BinaryRequestInfo>
    {
        private readonly ILifetimeScope _lifetimeScope;
        private IConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkSocketAppSession"/> class.
        /// </summary>
        public TrunkSocketAppSession()
        {
            // This constructor must not be called; we need the autofac container
            throw new NotSupportedException();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkSocketAppSession"/> class.
        /// </summary>
        /// <param name="lifetimeScope">The lifetime scope.</param>
        public TrunkSocketAppSession(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
            _connection = _lifetimeScope.Resolve<IConnection>();
        }

        /// <summary>
        /// Closes the and release locks.
        /// </summary>
        public void CloseAndReleaseLocks()
        {
            _connection.Dispose();
            Close(CloseReason.ClientClosing);
        }

        /// <summary>
        /// Gets the maximum allowed length of the request.
        /// </summary>
        /// <returns></returns>
        protected override int GetMaxRequestLength()
        {
            // We use Virtual Buffers to store requests so request size is
            //  constrained to size of page
            var bufferFactory = _lifetimeScope.Resolve<IVirtualBufferFactory>();
            return bufferFactory.BufferSize;
        }

        /// <summary>
        /// Called when [session started].
        /// </summary>
        protected override void OnSessionStarted()
        {
            // Immediately send message to client reporting our name and version
            Send("Zen Trunk Server v1.0\r\n");

            base.OnSessionStarted();
        }

        protected override void HandleUnknownRequest(BinaryRequestInfo requestInfo)
        {
            base.HandleUnknownRequest(requestInfo);
        }

        protected override void HandleException(Exception e)
        {
            base.HandleException(e);
        }
    }
}