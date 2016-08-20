using System;
using System.Text;
using Zen.Trunk.Torrent.Common;

namespace Zen.Trunk.Torrent.Client
{
    public class TorrentLoadException : TorrentException
    {

        public TorrentLoadException()
            : base()
        {
        }


        public TorrentLoadException(string message)
            : base(message)
        {
        }


        public TorrentLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


        public TorrentLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}