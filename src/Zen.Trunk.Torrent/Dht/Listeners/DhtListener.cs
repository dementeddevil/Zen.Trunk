namespace Zen.Trunk.Torrent.Dht.Listeners
{
	using System.Net;
	using Zen.Trunk.Torrent.Client;

	public delegate void MessageReceived(byte[] buffer, IPEndPoint endpoint);

	public abstract class DhtListener : Listener
	{
		public event MessageReceived MessageReceived;

		protected DhtListener(IPEndPoint endpoint)
			: base(endpoint)
		{
		}

		protected virtual void RaiseMessageReceived(byte[] buffer, IPEndPoint endpoint)
		{
			MessageReceived handler = MessageReceived;
			if (handler != null)
			{
				handler(buffer, endpoint);
			}
		}

		public abstract void Send(byte[] buffer, IPEndPoint endpoint);
	}
}
