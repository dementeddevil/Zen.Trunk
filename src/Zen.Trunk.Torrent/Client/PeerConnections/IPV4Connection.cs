namespace Zen.Trunk.Torrent.Client.Connections
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Net.Sockets;
	using System.Threading.Tasks;

	public class IPV4Connection : ConnectionBase
	{
		#region Member Variables
		private bool _isIncoming;
		private IPEndPoint _endpoint;
		private Socket _socket;
		private Uri _uri;
		#endregion

		#region Constructors

		public IPV4Connection(Uri uri)
			: this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
				   new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port),
				   false)
		{
			_uri = uri;
		}

		public IPV4Connection(IPEndPoint endPoint)
			: this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), endPoint, false)
		{

		}

		public IPV4Connection(Socket socket, bool isIncoming)
			: this(socket, (IPEndPoint)socket.RemoteEndPoint, isIncoming)
		{

		}


		private IPV4Connection(Socket socket, IPEndPoint endpoint, bool isIncoming)
		{
			_socket = socket;
			_endpoint = endpoint;
			_isIncoming = isIncoming;
		}
		#endregion

		#region Public Properties
		public override bool CanReconnect
		{
			get
			{
				return !_isIncoming;
			}
		}

		public override bool Connected
		{
			get
			{
				return _socket.Connected;
			}
		}

		public override EndPoint EndPoint
		{
			get
			{
				return _endpoint;
			}
		}

		public override bool IsIncoming
		{
			get
			{
				return _isIncoming;
			}
		}

		public override Uri Uri
		{
			get
			{
				return _uri;
			}
		}

		public override byte[] AddressBytes
		{
			get
			{
				return _endpoint.Address.GetAddressBytes();
			}
		}
		#endregion

		#region Async Methods
		public override Task ConnectAsync()
		{
			ThrowIfDisposed();
			
			return _socket.ConnectAsync(_endpoint);
		}

		public override Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
		{
			ThrowIfDisposed();

			List<ArraySegment<byte>> receiveBuffers = new List<ArraySegment<byte>>();
			receiveBuffers.Add(new ArraySegment<byte>(buffer, offset, count));
			return _socket.ReceiveAsync(receiveBuffers, SocketFlags.None);
		}

		public override Task<int> SendAsync(byte[] buffer, int offset, int count)
		{
			ThrowIfDisposed();

			List<ArraySegment<byte>> sendBuffers = new List<ArraySegment<byte>>();
			sendBuffers.Add(new ArraySegment<byte>(buffer, offset, count));
			return _socket.SendAsync(sendBuffers, SocketFlags.None);
		}

		protected override void DisposeManagedObjects()
		{
			// Attempt graceful close
			if (_socket != null)
			{
				if (_socket.Connected)
				{
					try
					{
						_socket.Shutdown(SocketShutdown.Both);
						_socket.Close(3);
					}
					catch
					{
					}
				}
				((IDisposable)_socket).Dispose();
				_socket = null;
			}
			base.DisposeManagedObjects();
		}
		#endregion
	}
}