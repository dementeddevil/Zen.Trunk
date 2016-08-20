namespace Zen.Trunk.Torrent.Client.Tracker
{
	using System;
	using System.Threading.Tasks;
	using System.Web;
	using Zen.Trunk.Torrent.Common;

	/// <summary>
	/// Class representing an instance of a Tracker
	/// </summary>
	public abstract class Tracker : IDisposable
	{
		#region Private Fields
		private static Random _random = new Random();

		private Uri _uri;
		private bool _canScrape;
		private int _complete;
		private int _downloaded;
		private int _inComplete;
		private readonly string _key;
		private DateTime _lastUpdated;
		private int _minUpdateInterval = 180;
		private int _updateInterval = 300;
		private TrackerState _state;
		private TrackerTier _tier;
		private string _trackerId;
		private bool _updateSucceeded;
		private string _failureMessage = string.Empty;
		private string _warningMessage = string.Empty;
		#endregion

		#region Public Events
		/// <summary>
		/// Occurs when a tracker announce has completed.
		/// </summary>
		public event EventHandler<AnnounceResponseEventArgs> AnnounceComplete;

		/// <summary>
		/// Occurs when a tracker scrape has completed.
		/// </summary>
		public event EventHandler<ScrapeResponseEventArgs> ScrapeComplete;

		/// <summary>
		/// Occurs when the tracker state has changed.
		/// </summary>
		public event EventHandler<TrackerStateChangedEventArgs> StateChanged;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Tracker"/> class.
		/// </summary>
		/// <param name="uri">The URI.</param>
		protected Tracker(Uri uri)
		{
			_uri = uri;
			_state = TrackerState.Unknown;
			_lastUpdated = DateTime.UtcNow.AddDays(-1);    // Forces an update on the first timertick.

			byte[] passwordKey = new byte[8];
			lock (_random)
			{
				_random.NextBytes(passwordKey);
			}
			_key = HttpUtility.UrlEncode(passwordKey);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets a value indicating whether this instance can scrape.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance can scrape; otherwise, <c>false</c>.
		/// </value>
		public bool CanScrape
		{
			get
			{
				return _canScrape;
			}
			protected set
			{
				_canScrape = value;
			}
		}

		/// <summary>
		/// Gets or sets the complete.
		/// </summary>
		/// <value>The complete.</value>
		public int Complete
		{
			get
			{
				return _complete;
			}
			protected set
			{
				_complete = value;
			}
		}

		/// <summary>
		/// Gets or sets the downloaded.
		/// </summary>
		/// <value>The downloaded.</value>
		public int Downloaded
		{
			get
			{
				return _downloaded;
			}
			protected set
			{
				_downloaded = value;
			}
		}

		/// <summary>
		/// Gets or sets the failure message.
		/// </summary>
		/// <value>The failure message.</value>
		public string FailureMessage
		{
			get
			{
				return _failureMessage;
			}
			protected set
			{
				_failureMessage = value;
			}
		}

		/// <summary>
		/// Gets or sets the incomplete.
		/// </summary>
		/// <value>The incomplete.</value>
		public int Incomplete
		{
			get
			{
				return _inComplete;
			}
			protected set
			{
				_inComplete = value;
			}
		}

		/// <summary>
		/// Gets or sets the last updated.
		/// </summary>
		/// <value>The last updated.</value>
		public DateTime LastUpdated
		{
			get
			{
				return _lastUpdated;
			}
			protected set
			{
				_lastUpdated = value;
			}
		}

		/// <summary>
		/// Gets or sets the min update interval.
		/// </summary>
		/// <value>The min update interval.</value>
		public int MinUpdateInterval
		{
			get
			{
				return _minUpdateInterval;
			}
			protected set
			{
				_minUpdateInterval = value;
			}
		}

		/// <summary>
		/// Gets the state.
		/// </summary>
		/// <value>The state.</value>
		public TrackerState State
		{
			get
			{
				return _state;
			}
		}

		/// <summary>
		/// Gets or sets the tracker id.
		/// </summary>
		/// <value>The tracker id.</value>
		public string TrackerId
		{
			get
			{
				return _trackerId;
			}
			protected set
			{
				_trackerId = value;
			}
		}

		/// <summary>
		/// Gets or sets the update interval.
		/// </summary>
		/// <value>The update interval.</value>
		public int UpdateInterval
		{
			get
			{
				return _updateInterval;
			}
			protected set
			{
				_updateInterval = value;
			}
		}

		/// <summary>
		/// Gets the URI.
		/// </summary>
		/// <value>The URI.</value>
		public Uri Uri
		{
			get
			{
				return _uri;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether [update succeeded].
		/// </summary>
		/// <value><c>true</c> if [update succeeded]; otherwise, <c>false</c>.</value>
		public bool UpdateSucceeded
		{
			get
			{
				return _updateSucceeded;
			}
			protected set
			{
				_updateSucceeded = value;
			}
		}

		/// <summary>
		/// Gets or sets the warning message.
		/// </summary>
		/// <value>The warning message.</value>
		public string WarningMessage
		{
			get
			{
				return _warningMessage;
			}
			protected set
			{
				_warningMessage = value;
			}
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the key.
		/// </summary>
		/// <value>The key.</value>
		protected internal string Key
		{
			get
			{
				return _key;
			}
		}
		#endregion

		#region Internal Properties
		/// <summary>
		/// Gets or sets the tier.
		/// </summary>
		/// <value>The tier.</value>
		internal TrackerTier Tier
		{
			get
			{
				return _tier;
			}
			set
			{
				_tier = value;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		public void Dispose()
		{
			DisposeManagedObjects();
		}

		/// <summary>
		/// Sends an announcement to the tracker server.
		/// </summary>
		/// <param name="parameters">
		/// A <see cref="AnnounceParameters"/> containing the announcement 
		/// information.
		/// </param>
		/// <returns></returns>
		public abstract Task Announce(AnnounceParameters parameters);

		/// <summary>
		/// Sends an scrape request to the tracker server.
		/// </summary>
		/// <param name="parameters">
		/// A <see cref="ScrapeParameters"/> containing the scrape information.
		/// </param>
		/// <returns></returns>
		public abstract Task Scrape(ScrapeParameters parameters);

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode()
		{
			return _uri.GetHashCode();
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return string.Format("Tracker[{0} for {1}]", GetType().Name, _uri);
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		protected virtual void DisposeManagedObjects()
		{
		}

		/// <summary>
		/// Updates the tracker state.
		/// </summary>
		/// <param name="newState">The new state.</param>
		protected void UpdateState(TrackerState newState)
		{
			UpdateState(newState, null);
		}

		/// <summary>
		/// Updates the tracker state.
		/// </summary>
		/// <param name="newState">The new state.</param>
		/// <param name="torrentManager">The torrent manager.</param>
		protected void UpdateState(TrackerState newState, TorrentManager torrentManager)
		{
			if (_state != newState)
			{
				TrackerStateChangedEventArgs e =
					new TrackerStateChangedEventArgs(
						torrentManager, this, State, newState);
				_state = newState;

				RaiseStateChanged(e);
			}
		}

		/// <summary>
		/// Raises the announce complete event.
		/// </summary>
		/// <param name="e">The <see cref="Zen.Trunk.Torrent.Client.AnnounceResponseEventArgs"/> instance containing the event data.</param>
		protected virtual void RaiseAnnounceComplete(AnnounceResponseEventArgs e)
		{
			Toolbox.RaiseAsyncEvent<AnnounceResponseEventArgs>(AnnounceComplete, this, e);
		}

		/// <summary>
		/// Raises the scrape complete event.
		/// </summary>
		/// <param name="e">The <see cref="Zen.Trunk.Torrent.Client.Tracker.ScrapeResponseEventArgs"/> instance containing the event data.</param>
		protected virtual void RaiseScrapeComplete(ScrapeResponseEventArgs e)
		{
			Toolbox.RaiseAsyncEvent<ScrapeResponseEventArgs>(ScrapeComplete, this, e);
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Raises the state changed event.
		/// </summary>
		/// <param name="e">The <see cref="Zen.Trunk.Torrent.Client.Tracker.TrackerStateChangedEventArgs"/> instance containing the event data.</param>
		private void RaiseStateChanged(TrackerStateChangedEventArgs e)
		{
			Toolbox.RaiseAsyncEvent<TrackerStateChangedEventArgs>(StateChanged, this, e);
		}
		#endregion
	}
}
