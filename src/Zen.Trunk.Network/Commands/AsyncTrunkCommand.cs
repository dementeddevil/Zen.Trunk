using System;
using System.Threading.Tasks;
using SuperSocket.SocketBase.Protocol;

namespace Zen.Trunk.Network.Commands
{
    /// <summary>
    /// <c>AsyncTrunkCommand</c> is the base class for all asynchronous Trunk commands
    /// </summary>
    /// <seealso cref="Zen.Trunk.Network.Commands.TrunkCommand" />
    public abstract class AsyncTrunkCommand : TrunkCommand
    {
        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="requestInfo">The request info.</param>
        public override async void ExecuteCommand(TrunkSocketAppSession session, BinaryRequestInfo requestInfo)
        {
            try
            {
                await OnExecuteCommandAsync(session, requestInfo).ConfigureAwait(false);
                session.Send("OK");
            }
            catch (Exception exception)
            {
                ProcessException(session, exception);
            }
        }

        /// <summary>
        /// Called to asynchronously execute the command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="requestInfo">The request information.</param>
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous operation.
        /// </returns>
        protected abstract Task OnExecuteCommandAsync(TrunkSocketAppSession session, BinaryRequestInfo requestInfo);
    }
}