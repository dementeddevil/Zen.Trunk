namespace Zen.Trunk.Torrent.Client.Tracker
{
	public class ScrapeParameters
	{
		public ScrapeParameters(TrackerConnectionId id, byte[] infoHash)
		{
			Id = id;
			InfoHash = infoHash;
		}

		public TrackerConnectionId Id
		{
			get;
			private set;
		}

		public byte[] InfoHash
		{
			get;
			private set;
		}
	}
}
