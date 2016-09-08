using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Zen.Trunk.Extensions.APM
{
    /// <summary>
    /// <c>SocketExtensions</c> asynchronous helpers for the <see cref="Socket"/> object.
    /// </summary>
    public static class SocketExtensions
	{
        /// <summary>
        /// Connects the socket to the specified endpoint asynchronously.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public static Task ConnectAsync(
			this Socket socket, EndPoint endpoint)
		{
			return Task.Factory.FromAsync(
				socket.BeginConnect(endpoint, null, null), socket.EndConnect);
		}

        /// <summary>
        /// Sends data to the socket asynchronously.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="buffers">The buffers.</param>
        /// <param name="socketFlags">The socket flags.</param>
        /// <returns></returns>
        public static Task<int> SendAsync(
			this Socket socket,
			List<ArraySegment<byte>> buffers,
			SocketFlags socketFlags)
		{
			return Task.Factory.FromAsync(
			    // ReSharper disable once AssignNullToNotNullAttribute
				socket.BeginSend(buffers, socketFlags, null, null),
				socket.EndSend);
		}

        /// <summary>
        /// Receives data from the socket asynchronously.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="buffers">The buffers.</param>
        /// <param name="socketFlags">The socket flags.</param>
        /// <returns></returns>
        public static Task<int> ReceiveAsync(
			this Socket socket,
			List<ArraySegment<byte>> buffers,
			SocketFlags socketFlags)
		{
			return Task.Factory.FromAsync(
                // ReSharper disable once AssignNullToNotNullAttribute
                socket.BeginReceive(buffers, socketFlags, null, null),
				socket.EndReceive);
		}

        /// <summary>
        /// Disconnects the socket asynchronously.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="reuseSocket">if set to <c>true</c> [reuse socket].</param>
        /// <returns></returns>
        public static Task DisconnectAsync(
			this Socket socket, bool reuseSocket)
		{
			return Task.Factory.FromAsync(
				socket.BeginDisconnect(reuseSocket, null, null),
				socket.EndDisconnect);
		}

        /// <summary>
        /// Accepts the connection asynchronously.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <returns></returns>
        public static Task<Socket> AcceptAsync(
			this Socket socket)
		{
			return Task.Factory.FromAsync(
				socket.BeginAccept(null, null),
                socket.EndAccept);
		}
	}
}
