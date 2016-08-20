namespace Zen.Trunk.Torrent.Client.Tracker
{
	using Zen.Trunk.Torrent.Common;

	public class AnnounceParameters
	{
		public AnnounceParameters(
			TorrentManager torrentManager,
			TorrentEvent clientEvent,
			byte[] infohash,
			TrackerConnectionId id,
			bool requireEncryption,
			bool supportsEncryption,
			string ipaddress,
			int port)
		{
			Torrent = torrentManager;
			BytesDownloaded = torrentManager.Monitor.DataBytesDownloaded;
			BytesUploaded = torrentManager.Monitor.DataBytesUploaded;
			BytesLeft = (long)((1 - torrentManager.Bitfield.PercentComplete / 100.0) * torrentManager.Torrent.Size);
			PeerId = torrentManager.Engine.PeerId;

			ClientEvent = clientEvent;
			InfoHash = infohash;
			Id = id;
			RequireEncryption = requireEncryption;
			SupportsEncryption = supportsEncryption;
			IpAddress = ipaddress;
			Port = port;
		}

		public TorrentManager Torrent
		{
			get;
			private set;
		}

		public long BytesDownloaded
		{
			get;
			private set;
		}

		public long BytesLeft
		{
			get;
			private set;
		}

		public long BytesUploaded
		{
			get;
			private set;
		}

		public string PeerId
		{
			get;
			private set;
		}

		public TorrentEvent ClientEvent
		{
			get;
			set;
		}

		public TrackerConnectionId Id
		{
			get;
			set;
		}

		public byte[] InfoHash
		{
			get;
			set;
		}

		public string IpAddress
		{
			get;
			set;
		}

		public int Port
		{
			get;
			set;
		}

		public bool RequireEncryption
		{
			get;
			set;
		}

		public bool SupportsEncryption
		{
			get;
			set;
		}
	}
}