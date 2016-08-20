namespace Zen.Trunk.Torrent.Bencoding
{
	using System;
	using System.Text;
	using System.Runtime.Serialization;

    [Serializable]
    public class BEncodingException : Exception
    {
        public BEncodingException()
            : base()
        {
        }

        public BEncodingException(string message)
            : base(message)
        {
        }

        public BEncodingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BEncodingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
