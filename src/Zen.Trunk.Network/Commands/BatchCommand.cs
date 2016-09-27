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
            try
            {
                // Unpack statement from request body (assume unicode)
                var batch = Encoding.Unicode.GetString(requestInfo.Body);
                await session.ExecuteBatchAsync(batch).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                // TODO: Categorise errors into critical, fatal, severe etc
                //  so we can act accordingly with regard to the session
                //  as well as returning sane error numbers and such like

                // For now this ham-fisted method will have to do
                session.Send($"ERROR {exception.Message}");
            }
        }
    }
}
