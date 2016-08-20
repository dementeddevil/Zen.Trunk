using System;
using System.Collections.Generic;
using System.Text;

namespace Zen.Trunk.Torrent.Dht
{
    internal class MessageException : Exception
    {
        private ErrorCode errorCode;

        public ErrorCode ErrorCode
        {
            get { return errorCode; }
        }

        public MessageException(ErrorCode errorCode, string message) : base(message)
        {
            this.errorCode = errorCode;
        }
    }
}
