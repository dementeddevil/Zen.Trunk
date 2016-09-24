using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;

namespace Zen.Trunk.Network.Commands
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="SuperSocket.SocketBase.Command.CommandBase{TrunkSocketAppSession, BinaryRequestInfo}" />
    public class QuitCommand : CommandBase<TrunkSocketAppSession, BinaryRequestInfo>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name => "QUIT";

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="requestInfo">The request info.</param>
        public override void ExecuteCommand(TrunkSocketAppSession session, BinaryRequestInfo requestInfo)
        {
            session.CloseAndReleaseLocks();
        }
    }
}