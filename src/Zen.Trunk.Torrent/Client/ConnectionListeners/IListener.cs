namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Net;
	using Zen.Trunk.Torrent.Common;

	public interface IListener
	{
		event EventHandler<EventArgs> StatusChanged;

		IPEndPoint LocalEndPoint
		{
			get;
		}

		ListenerStatus Status
		{
			get;
		}

		void ChangeEndpoint(IPEndPoint port);
		void Start();
		void Stop();
	}
}
