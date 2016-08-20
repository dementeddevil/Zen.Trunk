namespace Zen.Trunk.Torrent.Common
{
	using System;

	public enum ListenerStatus
	{
		Listening,
		PortNotFree,
		NotListening
	}

	public enum PeerStatus
	{
		Available,
		Connecting,
		Connected
	}

	public enum Direction
	{
		None,
		Incoming,
		Outgoing
	}

	public enum TorrentState
	{
		Stopped,
		Paused,
		Downloading,
		Seeding,
		Hashing,
		Stopping
	}

	public enum Priority
	{
		DoNotDownload = 0,
		Lowest = 1,
		Low = 2,
		Normal = 3,
		High = 4,
		Highest = 5,
		Immediate = 6
	}

	public enum TrackerState
	{
		Unknown,
		Announcing,
		AnnouncingFailed,
		AnnounceSuccessful,
		Scraping,
		ScrapingFailed,
		ScrapeSuccessful
	}

	public enum TorrentEvent
	{
		None,
		Started,
		Stopped,
		Completed
	}

	public enum PeerConnectionEvent
	{
		IncomingConnectionReceived,
		OutgoingConnectionCreated,
		Disconnected
	}

	public enum PieceEvent
	{
		BlockWriteQueued,
		BlockNotRequested,
		BlockWrittenToDisk,
		HashPassed,
		HashFailed
	}

	public enum PeerListType
	{
		NascentPeers,
		CandidatePeers,
		OptimisticUnchokeCandidatePeers
	}
}
