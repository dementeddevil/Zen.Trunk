using System;
using System.Threading.Tasks;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;

namespace Zen.Trunk.Network.Commands
{
    public class ResetCommand : AsyncTrunkCommand
    {
        public override string Name => "RSET";

        protected override async Task OnExecuteCommandAsync(TrunkSocketAppSession session, BinaryRequestInfo requestInfo)
        {
            await session.ResetAsync(true).ConfigureAwait(false);
            session.Send("OK");
        }
    }
}