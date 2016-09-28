using System;
using SuperSocket.SocketBase.Protocol;

namespace Zen.Trunk.Network.Commands
{
    /// <summary>
    /// <c>SyncTrunkCommand</c> is the base class for all synchronous Trunk commands
    /// </summary>
    /// <seealso cref="Zen.Trunk.Network.Commands.TrunkCommand" />
    public abstract class SyncTrunkCommand : TrunkCommand
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
                await session.ResetAsync(true).ConfigureAwait(false);
                session.Send("OK");
            }
            catch (Exception exception)
            {
                ProcessException(session, exception);
            }
        }

        /// <summary>
        /// Called to synchronously execute the command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="requestInfo">The request information.</param>
        protected abstract void OnExecuteCommand(TrunkSocketAppSession session, BinaryRequestInfo requestInfo);
    }
}