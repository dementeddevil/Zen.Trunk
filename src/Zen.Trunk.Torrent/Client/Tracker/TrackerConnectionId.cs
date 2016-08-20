namespace Zen.Trunk.Torrent.Client.Tracker
{
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Common;

	public class TrackerConnectionId : TaskCompletionSource<object>
	{
		#region Constructors
		public TrackerConnectionId(Tracker tracker, bool trySubsequent, TorrentEvent torrentEvent, object request)
		{
			Tracker = tracker;
			TrySubsequent = trySubsequent;
			TorrentEvent = torrentEvent;
			Request = request;
		}
		#endregion

		internal TorrentEvent TorrentEvent
		{
			get;
			private set;
		}

		public object Request
		{
			get;
			set;
		}

		public Tracker Tracker
		{
			get;
			private set;
		}

		internal bool TrySubsequent
		{
			get;
			private set;
		}
	}
}
