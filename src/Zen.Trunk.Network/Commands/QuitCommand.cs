using System.Threading.Tasks;
using SuperSocket.SocketBase.Protocol;

namespace Zen.Trunk.Network.Commands
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="SuperSocket.SocketBase.Command.CommandBase{TrunkSocketAppSession, BinaryRequestInfo}" />
    public class QuitCommand : AsyncTrunkCommand
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name => "QUIT";

        /// <summary>
        /// Called to asynchronously execute the command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="requestInfo">The request information.</param>
        /// <returns>
        /// A <see cref="Task" /> that represents the asynchronous operation.
        /// </returns>
        protected override async Task OnExecuteCommandAsync(TrunkSocketAppSession session, BinaryRequestInfo requestInfo)
        {
            await session.ResetAsync().ConfigureAwait(false);
            session.CloseAndReleaseLocks();
        }
    }
}