namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Net;
	using System.Net.Sockets;
	using Zen.Trunk.Torrent.Client.Connections;
	using Zen.Trunk.Torrent.Client.Encryption;
	using Zen.Trunk.Torrent.Common;

	/// <summary>
	/// Accepts incoming connections and passes them off to the right TorrentManager
	/// </summary>
	public class SocketListener : PeerListener
	{
		private Socket _listener;

		public SocketListener(IPEndPoint localEndPoint)
			: base(localEndPoint)
		{
		}

		public override void Start()
		{
			if (Status == ListenerStatus.Listening)
			{
				return;
			}

			try
			{
				_listener = new Socket(
					AddressFamily.InterNetwork,
					SocketType.Stream,
					ProtocolType.Tcp);
				_listener.Bind(LocalEndPoint);
				_listener.Listen(6);

				AcceptLoop();
			}
			catch (SocketException)
			{
				RaiseStatusChanged(ListenerStatus.PortNotFree);
			}
		}

		public override void Stop()
		{
			RaiseStatusChanged(ListenerStatus.NotListening);

			if (_listener != null)
			{
				_listener.Close();
			}
		}

		private async void AcceptLoop()
		{
			RaiseStatusChanged(ListenerStatus.Listening);
			while (Status == ListenerStatus.Listening)
			{
				Socket incomingSocket = null;
				try
				{
					// Wait for incoming connection
					incomingSocket = await _listener.AcceptAsync();

					// Create connection object
					Uri connectionUri;
					IConnection connection = ConnectionFactory.Create(incomingSocket, out connectionUri);

					// Raise notification
					Peer peer = new Peer(string.Empty, connectionUri, EncryptionTypes.All);
					RaiseConnectionReceived(peer, connection, null);
				}
				catch (SocketException)
				{
					// Just dump the connection
					if (incomingSocket != null)
					{
						incomingSocket.Close();
					}
				}
				catch (ObjectDisposedException)
				{
					// We've stopped listening
				}
			}
		}
	}
}