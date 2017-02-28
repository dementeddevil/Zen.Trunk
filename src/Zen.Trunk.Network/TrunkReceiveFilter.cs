using System;
using System.Text;
using SuperSocket.Common;
using SuperSocket.Facility.Protocol;
using SuperSocket.SocketBase.Protocol;

namespace Zen.Trunk.Network
{
    public class TrunkReceiveFilter : FixedHeaderReceiveFilter<BinaryRequestInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkReceiveFilter"/> class.
        /// </summary>
        public TrunkReceiveFilter() : base(headerSize: 6)
        {
        }

        /// <summary>
        /// Gets the body length from header.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length)
        {
            return (int)header[offset + 4] * 256 + (int)header[offset + 5];
        }

        /// <summary>
        /// Resolves the request data.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="bodyBuffer">The body buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override BinaryRequestInfo ResolveRequestInfo(ArraySegment<byte> header, byte[] bodyBuffer, int offset, int length)
        {
            return new BinaryRequestInfo(
                Encoding.ASCII.GetString(header.Array, header.Offset, 4),
                bodyBuffer.CloneRange(offset, length));
        }
    }
}