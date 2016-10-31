using System;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase.Protocol;

namespace Zen.Trunk.Network.Commands
{
    public class BatchCommand : AsyncTrunkCommand
    {
        public override string Name => "BTCH";

        protected override async Task OnExecuteCommandAsync(TrunkSocketAppSession session, BinaryRequestInfo requestInfo)
        {
            // Unpack statement from request body (assume unicode)
            var batch = Encoding.Unicode.GetString(requestInfo.Body);
            await session.ExecuteBatchAsync(batch).ConfigureAwait(false);
        }

        protected override void ProcessException(TrunkSocketAppSession session, Exception exception)
        {
            base.ProcessException(session, exception);

            // TODO: Categorise errors into critical, fatal, severe etc
            //  so we can act accordingly with regard to the session
            //  as well as returning sane error numbers and such like

            // For now this ham-fisted method will have to do
            session.Send($"ERROR {exception.Message}");
        }
    }
}
