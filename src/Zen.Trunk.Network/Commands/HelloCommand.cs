using System.IO;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Network.Commands
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="SuperSocket.SocketBase.Command.CommandBase{TrunkSocketAppSession, BinaryRequestInfo}" />
    public class HelloCommand : SyncTrunkCommand
    {
        private class Payload : BufferFieldWrapper
        {
            private readonly BufferFieldUInt16 _protocolVersion;
            private readonly BufferFieldStringFixed _clientName;

            public Payload()
            {
                _protocolVersion = new BufferFieldUInt16();
                _clientName = new BufferFieldStringFixed(_protocolVersion, 255);
            }

            public ushort ProtocolVersion => _protocolVersion.Value;

            public string ClientName => _clientName.Value;

            protected override BufferField FirstField => _protocolVersion;

            protected override BufferField LastField => _clientName;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name => "HELO";

        /// <summary>
        /// Called to synchronously execute the command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="requestInfo">The request information.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void OnExecuteCommand(TrunkSocketAppSession session, BinaryRequestInfo requestInfo)
        {
            // Payload should include client protocol version and name
            var payload = new Payload();
            using (var stream = new MemoryStream(requestInfo.Body, false))
            {
                using (var bufferReaderWriter = new BufferReaderWriter(stream))
                {
                    payload.Read(bufferReaderWriter);
                }
            }

            // Validate protocol version
            if (payload.ProtocolVersion != 0x0100)
            {
                session.Send("FAIL Unknown protocol version.");
                session.Close(CloseReason.ProtocolError);
                return;
            }

            // TODO: Check with server whether we are in single connection mode
            //  and if so whether we are only accepting connection with matching name

            // Log inbound HELO command
            if (session.Logger.IsInfoEnabled)
            {
                session.Logger.InfoFormat(
                    "Inbound HELO received from {0} using client name {1}",
                    session.RemoteEndPoint, payload.ClientName);
            }

            // Reply with OK and version number
            session.Send("OK 1.0");
            throw new System.NotImplementedException();
        }
    }
}