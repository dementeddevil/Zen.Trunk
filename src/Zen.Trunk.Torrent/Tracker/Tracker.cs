namespace Zen.Trunk.Torrent.Tracker
{
	using System;
	using System.IO;
	using System.Net;
	using System.Web;
	using System.Text;
	using System.Diagnostics;
	using System.Net.Sockets;
	using System.Collections;
	using System.Collections.Generic;

	using Zen.Trunk.Torrent.Common;
	using Zen.Trunk.Torrent.Bencoding;
	using Zen.Trunk.Torrent.Tracker.Listeners;
	using System.Collections.Concurrent;

	public class Tracker : IEnumerable<SimpleTorrentManager>, IDisposable
	{
		#region Static BEncodedStrings

		internal static readonly BEncodedString PeersKey = "peers";
		internal static readonly BEncodedString IntervalKey = "interval";
		internal static readonly BEncodedString MinIntervalKey = "min interval";
		internal static readonly BEncodedString TrackerIdKey = "tracker id";
		internal static readonly BEncodedString CompleteKey = "complete";
		internal static readonly BEncodedString Incomplete = "incomplete";
		internal static readonly BEncodedString PeerIdKey = "peer id";
		internal static readonly BEncodedString Port = "port";
		internal static readonly BEncodedString Ip = "ip";

		#endregion Static BEncodedStrings

		#region Private Fields
		private bool _allowScrape;
		private bool _allowNonCompact;
		private bool _allowUnregisteredTorrents;
		private TimeSpan _announceInterval;
		private bool _disposed;
		private TimeSpan _minAnnounceInterval;
		private RequestMonitor _monitor;
		private TimeSpan _timeoutInterval;
		private ConcurrentDictionary<byte[], SimpleTorrentManager> _torrents;
		private BEncodedString _trackerId;
		#endregion

		#region Public Events
		public event EventHandler<AnnounceEventArgs> PeerAnnounced;
		public event EventHandler<ScrapeEventArgs> PeerScraped;
		public event EventHandler<TimedOutEventArgs> PeerTimedOut;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Tracker"/> class.
		/// </summary>
		public Tracker()
			: this(new BEncodedString("zentorrent-tracker"))
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Tracker"/> class.
		/// </summary>
		/// <param name="trackerId">The tracker id.</param>
		public Tracker(BEncodedString trackerId)
		{
			_trackerId = trackerId;
			_allowNonCompact = true;
			_allowScrape = true;
			_monitor = new RequestMonitor();
			_torrents = new ConcurrentDictionary<byte[], SimpleTorrentManager>(new ByteComparer());

			_announceInterval = TimeSpan.FromMinutes(45);
			_minAnnounceInterval = TimeSpan.FromMinutes(10);
			_timeoutInterval = TimeSpan.FromMinutes(50);

			Zen.Trunk.Torrent.Client.ClientEngine.MainLoop.QueueRecurring(
				TimeSpan.FromSeconds(1),
				delegate
				{
					Requests.Tick();
					return !_disposed;
				});
		}
		#endregion

		#region Properties

		public bool AllowNonCompact
		{
			get
			{
				return _allowNonCompact;
			}
			set
			{
				_allowNonCompact = value;
			}
		}

		public bool AllowScrape
		{
			get
			{
				return _allowScrape;
			}
			set
			{
				_allowScrape = value;
			}
		}

		public bool AllowUnregisteredTorrents
		{
			get
			{
				return _allowUnregisteredTorrents;
			}
			set
			{
				_allowUnregisteredTorrents = value;
			}
		}

		public TimeSpan AnnounceInterval
		{
			get
			{
				return _announceInterval;
			}
			set
			{
				_announceInterval = value;
			}
		}

		public int Count
		{
			get
			{
				return _torrents.Count;
			}
		}

		public bool Disposed
		{
			get
			{
				return _disposed;
			}
		}

		public TimeSpan MinAnnounceInterval
		{
			get
			{
				return _minAnnounceInterval;
			}
			set
			{
				_minAnnounceInterval = value;
			}
		}

		public RequestMonitor Requests
		{
			get
			{
				return _monitor;
			}
		}

		public TimeSpan TimeoutInterval
		{
			get
			{
				return _timeoutInterval;
			}
			set
			{
				_timeoutInterval = value;
			}
		}

		public BEncodedString TrackerId
		{
			get
			{
				return _trackerId;
			}
		}

		#endregion Properties

		#region Public Methods
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			DisposeManagedObjects();
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		protected virtual void DisposeManagedObjects()
		{
			_disposed = true;
		}

		public bool Add(ITrackable trackable)
		{
			return Add(trackable, new IPAddressComparer());
		}

		public bool Add(ITrackable trackable, IPeerComparer comparer)
		{
			CheckDisposed();
			if (trackable == null)
			{
				throw new ArgumentNullException("trackable");
			}

			SimpleTorrentManager trackManager =
				new SimpleTorrentManager(trackable, comparer, this);
			if (!_torrents.TryAdd(trackable.InfoHash, trackManager))
			{
				return false;
			}

			Debug.WriteLine(string.Format("Tracking Torrent: {0}", trackable.Name));
			return true;
		}

		private void CheckDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}
		}

		public bool Contains(ITrackable trackable)
		{
			CheckDisposed();
			if (trackable == null)
			{
				throw new ArgumentNullException("trackable");
			}

			return _torrents.ContainsKey(trackable.InfoHash);
		}

		public SimpleTorrentManager GetManager(ITrackable trackable)
		{
			CheckDisposed();
			if (trackable == null)
			{
				throw new ArgumentNullException("trackable");
			}

			SimpleTorrentManager value = null;
			if (!_torrents.TryGetValue(trackable.InfoHash, out value))
			{
				value = null;
			}
			return value;
		}

		public IEnumerator<SimpleTorrentManager> GetEnumerator()
		{
			CheckDisposed();
			return _torrents.Values.GetEnumerator();
		}

		public bool IsRegistered(ListenerBase listener)
		{
			CheckDisposed();
			if (listener == null)
			{
				throw new ArgumentNullException("listener");
			}

			return listener.Tracker == this;
		}

		private void ListenerReceivedAnnounce(object sender, AnnounceParameters e)
		{
			if (_disposed)
			{
				e.Response.Add(RequestParameters.FailureKey, (BEncodedString)"The tracker has been shut down");
				return;
			}

			_monitor.AnnounceReceived();

			// Check to see if we're monitoring the requested torrent
			// NOTE: We work this way to reduce locking...
			SimpleTorrentManager manager = null;
			if (AllowUnregisteredTorrents)
			{
				ITrackable trackable = new InfoHashTrackable(
					BitConverter.ToString(e.InfoHash), e.InfoHash);
				SimpleTorrentManager trackManager =
					new SimpleTorrentManager(trackable, new IPAddressComparer(), this);
				manager = _torrents.GetOrAdd(e.InfoHash, trackManager);
			}
			else
			{
				_torrents.TryGetValue(e.InfoHash, out manager);
			}
			if (manager == null)
			{
				e.Response.Add(RequestParameters.FailureKey, 
					(BEncodedString)"The requested torrent is not registered with this tracker");
				return;
			}

			// If a non-compact response is expected and we do not allow 
			//	non-compact responses then bail out
			if (!AllowNonCompact && !e.HasRequestedCompact)
			{
				e.Response.Add(RequestParameters.FailureKey, 
					(BEncodedString)"This tracker does not support non-compact responses");
				return;
			}

			lock (manager)
			{
				// Update the tracker with the peers information.
				//	This adds the peer to the tracker, updates it's information
				//	or removes it depending on the context
				manager.Update(e);

				// Clear any peers who haven't announced within the allowed 
				//	timespan and may be inactive
				manager.ClearZombiePeers(DateTime.UtcNow.Add(-TimeoutInterval));

				// Fulfill the announce request
				manager.GetPeers(e.Response, e.NumberWanted, e.HasRequestedCompact);
			}

			e.Response.Add(Tracker.IntervalKey, new BEncodedNumber((int)AnnounceInterval.TotalSeconds));
			e.Response.Add(Tracker.MinIntervalKey, new BEncodedNumber((int)MinAnnounceInterval.TotalSeconds));
			e.Response.Add(Tracker.TrackerIdKey, _trackerId); // FIXME: Is this right?
			e.Response.Add(Tracker.CompleteKey, new BEncodedNumber(manager.Complete));
			e.Response.Add(Tracker.Incomplete, new BEncodedNumber(manager.Incomplete));
		}

		private void ListenerReceivedScrape(object sender, ScrapeParameters e)
		{
			if (_disposed)
			{
				e.Response.Add(RequestParameters.FailureKey, (BEncodedString)"The tracker has been shut down");
				return;
			}

			_monitor.ScrapeReceived();
			if (!AllowScrape)
			{
				e.Response.Add(RequestParameters.FailureKey, (BEncodedString)"This tracker does not allow scraping");
				return;
			}

			if (e.InfoHashes.Count == 0)
			{
				e.Response.Add(RequestParameters.FailureKey, (BEncodedString)"You must specify at least one infohash when scraping this tracker");
				return;
			}

			List<SimpleTorrentManager> managers = new List<SimpleTorrentManager>();
			BEncodedDictionary files = new BEncodedDictionary();
			for (int i = 0; i < e.InfoHashes.Count; i++)
			{
				// Converting infohash
				SimpleTorrentManager manager;
				string key = Toolbox.ToHex(e.InfoHashes[i]);
				if (!_torrents.TryGetValue(e.InfoHashes[i], out manager))
				{
					continue;
				}
				managers.Add(manager);

				BEncodedDictionary dict = new BEncodedDictionary();
				dict.Add("complete", new BEncodedNumber(manager.Complete));
				dict.Add("downloaded", new BEncodedNumber(manager.Downloaded));
				dict.Add("incomplete", new BEncodedNumber(manager.Incomplete));
				dict.Add("name", new BEncodedString(manager.Trackable.Name));
				files.Add(key, dict);
			}
			RaisePeerScraped(new ScrapeEventArgs(managers));
			e.Response.Add("files", files);
		}

		internal void RaisePeerAnnounced(AnnounceEventArgs e)
		{
			EventHandler<AnnounceEventArgs> h = PeerAnnounced;
			if (h != null)
				h(this, e);
		}

		internal void RaisePeerScraped(ScrapeEventArgs e)
		{
			EventHandler<ScrapeEventArgs> h = PeerScraped;
			if (h != null)
				h(this, e);
		}

		internal void RaisePeerTimedOut(TimedOutEventArgs e)
		{
			EventHandler<TimedOutEventArgs> h = PeerTimedOut;
			if (h != null)
				h(this, e);
		}

		public void RegisterListener(ListenerBase listener)
		{
			CheckDisposed();
			if (listener == null)
				throw new ArgumentNullException("listener");

			if (listener.Tracker != null)
				throw new TorrentException("The listener is registered to a different Tracker");

			listener.Tracker = this;
			listener.AnnounceReceived += new EventHandler<AnnounceParameters>(ListenerReceivedAnnounce);
			listener.ScrapeReceived += new EventHandler<ScrapeParameters>(ListenerReceivedScrape);
		}

		public void Remove(ITrackable trackable)
		{
			CheckDisposed();
			if (trackable == null)
				throw new ArgumentNullException("trackable");

			SimpleTorrentManager manager;
			_torrents.TryRemove(trackable.InfoHash, out manager);
		}

		public void UnregisterListener(ListenerBase listener)
		{
			CheckDisposed();
			if (listener == null)
				throw new ArgumentNullException("listener");

			if (listener.Tracker != this)
				throw new TorrentException("The listener is not registered with this tracker");

			listener.Tracker = null;
			listener.AnnounceReceived -= new EventHandler<AnnounceParameters>(ListenerReceivedAnnounce);
			listener.ScrapeReceived -= new EventHandler<ScrapeParameters>(ListenerReceivedScrape);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
