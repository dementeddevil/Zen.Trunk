using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;

namespace Zen.Trunk.Network.Commands
{
    public class ResetCommand : CommandBase<TrunkSocketAppSession, BinaryRequestInfo>
    {
        public override string Name => "RSET";

        public override async void ExecuteCommand(TrunkSocketAppSession session, BinaryRequestInfo requestInfo)
        {
            
        }
    }
}