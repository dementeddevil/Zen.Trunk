using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;

namespace Zen.Trunk.Network.Commands
{
    public class BatchCommand : CommandBase<TrunkSocketAppSession, BinaryRequestInfo>
    {
        public override string Name => "BTCH";

        public override async void ExecuteCommand(TrunkSocketAppSession session, BinaryRequestInfo requestInfo)
        {
            // TODO: We need to support cancellation
            // TODO: Unpack statement from request body
            await session.ExecuteBatchAsync(string.Empty).ConfigureAwait(false);
        }
    }
}
