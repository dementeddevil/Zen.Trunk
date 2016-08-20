using System;
using System.Collections.Generic;
using System.Text;
using Zen.Trunk.Torrent.Client.Messages.Standard;

namespace Zen.Trunk.Torrent.Client.Connections
{
    public partial class HttpConnection
    {
        private class HttpRequestData
        {
            public RequestMessage Request;
            public bool SentLength;
            public bool SentHeader;
            public int TotalToReceive;
            public int TotalReceived;

            public bool IsComplete
            {
                get { return TotalToReceive == TotalReceived; }
            }

            public HttpRequestData(RequestMessage request)
            {
                Request = request;
                PieceMessage m = new PieceMessage(null, request.PieceIndex, request.StartOffset, request.RequestLength);
                TotalToReceive = m.ByteLength;
            }
        }
    }
}
