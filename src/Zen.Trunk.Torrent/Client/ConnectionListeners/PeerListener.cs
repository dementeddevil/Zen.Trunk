namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Net;
	using Zen.Trunk.Torrent.Client.Connections;
	using Zen.Trunk.Torrent.Common;

	public abstract class PeerListener : Listener
	{
		public event EventHandler<NewConnectionEventArgs> ConnectionReceived;

		protected PeerListener(IPEndPoint localEndPoint)
			: base(localEndPoint)
		{
		}

		protected virtual void RaiseConnectionReceived(
			Peer peer, IConnection connection, TorrentManager manager)
		{
			if (ConnectionReceived != null)
			{
				Toolbox.RaiseAsyncEvent<NewConnectionEventArgs>(
					ConnectionReceived, 
					this, 
					new NewConnectionEventArgs(peer, connection, manager));
			}
		}
	}
}
