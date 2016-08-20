namespace Zen.Trunk.Torrent.Client.Connections
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Net.Sockets;
	using System.Threading.Tasks;

	public class IPV6Connection : ConnectionBase
	{
		private bool _isIncoming;
		private Socket _socket;
		private EndPoint _endpoint;
		private Uri _uri;

		public IPV6Connection(Uri uri)
		{
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}

			if (uri.HostNameType != UriHostNameType.IPv6)
			{
				throw new ArgumentException("Uri is not an IPV6 uri", "uri");
			}

			_socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
			_endpoint = new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port);
			_uri = uri;
		}

		public IPV6Connection(Socket socket, bool isIncoming)
		{
			if (socket == null)
			{
				throw new ArgumentNullException("socket");
			}

			if (socket.AddressFamily != AddressFamily.InterNetworkV6)
			{
				throw new ArgumentException("Not an IPV6 socket", "socket");
			}

			_socket = socket;
			_endpoint = socket.RemoteEndPoint;
			_isIncoming = isIncoming;
		}

		public override byte[] AddressBytes
		{
			// Fix this - Technically this is only useful for IPV4 Connections for the fast peer
			// extensions. I shouldn't force every inheritor to need this
			get
			{
				return new byte[4];
			}
		}

		public override bool Connected
		{
			get
			{
				return _socket.Connected;
			}
		}

		public override bool CanReconnect
		{
			get
			{
				return !_isIncoming;
			}
		}

		public override bool IsIncoming
		{
			get
			{
				return _isIncoming;
			}
		}

		public override EndPoint EndPoint
		{
			get
			{
				return _endpoint;
			}
		}

		public override Uri Uri
		{
			get
			{
				return _uri;
			}
		}

		public override Task ConnectAsync()
		{
			return _socket.ConnectAsync(_endpoint);
		}

		public override Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
		{
			List<ArraySegment<byte>> receiveBuffers = new List<ArraySegment<byte>>();
			receiveBuffers.Add(new ArraySegment<byte>(buffer, offset, count));
			return _socket.ReceiveAsync(receiveBuffers, SocketFlags.None);
		}

		public override Task<int> SendAsync(byte[] buffer, int offset, int count)
		{
			List<ArraySegment<byte>> sendBuffers = new List<ArraySegment<byte>>();
			sendBuffers.Add(new ArraySegment<byte>(buffer, offset, count));
			return _socket.SendAsync(sendBuffers, SocketFlags.None);
		}

		protected override void DisposeManagedObjects()
		{
			((IDisposable)_socket).Dispose();
			base.DisposeManagedObjects();
		}
	}
}
