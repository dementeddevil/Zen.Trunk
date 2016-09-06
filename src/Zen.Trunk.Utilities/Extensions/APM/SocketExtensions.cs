using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Zen.Trunk.Extensions.APM
{
    public static class SocketExtensions
	{
		public static Task ConnectAsync(
			this Socket socket, EndPoint endpoint)
		{
			return Task.Factory.FromAsync(
				socket.BeginConnect(endpoint, null, null), socket.EndConnect);
		}

		public static Task<int> SendAsync(
			this Socket socket,
			List<ArraySegment<byte>> buffers,
			SocketFlags socketFlags)
		{
			return Task.Factory.FromAsync<int>(
				socket.BeginSend(buffers, socketFlags, null, null),
				socket.EndSend);
		}

		public static Task<int> ReceiveAsync(
			this Socket socket,
			List<ArraySegment<byte>> buffers,
			SocketFlags socketFlags)
		{
			return Task.Factory.FromAsync<int>(
				socket.BeginReceive(buffers, socketFlags, null, null),
				socket.EndReceive);
		}

		public static Task DisconnectAsync(
			this Socket socket, bool reuseSocket)
		{
			return Task.Factory.FromAsync(
				socket.BeginDisconnect(reuseSocket, null, null),
				socket.EndDisconnect);
		}

		public static Task<Socket> AcceptAsync(
			this Socket socket)
		{
			return Task.Factory.FromAsync<Socket>(
				socket.BeginAccept(null, null), socket.EndAccept);
		}
	}
}
