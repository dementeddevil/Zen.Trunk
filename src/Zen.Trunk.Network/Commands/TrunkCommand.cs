using System;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;

namespace Zen.Trunk.Network.Commands
{
    /// <summary>
    /// <c>TrunkCommand</c> defines the base for all Trunk commands.
    /// </summary>
    /// <remarks>
    /// This object knows how to return exception information to the client.
    /// </remarks>
    /// <seealso cref="SuperSocket.SocketBase.Command.CommandBase{Zen.Trunk.Network.TrunkSocketAppSession, SuperSocket.SocketBase.Protocol.BinaryRequestInfo}" />
    public abstract class TrunkCommand : CommandBase<TrunkSocketAppSession, BinaryRequestInfo>
    {
        protected void ProcessException(TrunkSocketAppSession session, Exception exception)
        {
            // TODO: Depending upon the exception we may need to reset
            //  the session or even forcibly disconnect the client
        }
    }
}