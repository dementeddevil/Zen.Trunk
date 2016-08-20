namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Client.Connections;
	using Zen.Trunk.Torrent.Client.Encryption;
	using Zen.Trunk.Torrent.Client.Messages;
	using Zen.Trunk.Torrent.Client.Messages.Standard;
	using Zen.Trunk.Torrent.Client.Tracker;
	using Zen.Trunk.Torrent.Common;
	using Zen.Trunk.Torrent.Dht;

	public class TorrentManager : IDisposable, IEquatable<TorrentManager>
	{
		#region Private Fields
		private BitField _bitfield;              // The bitfield representing the pieces we've downloaded and have to download
		private ClientEngine _engine;            // The engine that this torrent is registered with
		private FileManager _fileManager;        // Controls all reading/writing to/from the disk
		internal Queue<int> _finishedPieces;     // The list of pieces which we should send "have" messages for
		private bool _hashChecked;               // True if the manager has been hash checked
		private int _hashFails;                  // The total number of pieces receieved which failed the hashcheck
		private ConnectionMonitor _monitor;      // Calculates download/upload speed
		private PeerManager _peers;              // Stores all the peers we know of in a list
		private PieceManager _pieceManager;      // Tracks all the piece requests we've made and decides what pieces we can request off each peer
		private RateLimiter _uploadLimiter;        // Contains the logic to decide how many chunks we can download
		private RateLimiter _downloadLimiter;        // Contains the logic to decide how many chunks we can download
		private TorrentSettings _settings;       // The settings for this torrent
		private DateTime? _startTime;             // The time at which the torrent was started at.
		private DateTime? _stopTime;
		private TorrentState _state;             // The current state (seeding, downloading etc)
		private TorrentObject _torrent;                // All the information from the physical torrent that was loaded
		private TrackerManager _trackerManager;  // The class used to control all access to the tracker
		private int _uploadingTo;                // The number of peers which we're currently uploading to
		private ChokeUnchokeManager _chokeUnchoker; // Used to choke and unchoke peers
		private InactivePeerManager _inactivePeerManager; // Used to identify inactive peers we don't want to connect to
		private InitialSeed _initialSeed;	//superseed class manager

		private CancellationTokenSource _shutdownTorrent;
		#endregion

		#region Events

		public event EventHandler<PeerConnectionEventArgs> PeerConnected;


		public event EventHandler<PeerConnectionEventArgs> PeerDisconnected;

		internal event EventHandler<PeerConnectionFailedEventArgs> ConnectionAttemptFailed;

		public event EventHandler<PeersAddedEventArgs> PeersFound;

		public event EventHandler<PieceHashedEventArgs> PieceHashed;

		public event EventHandler<TorrentStateChangedEventArgs> TorrentStateChanged;

		internal event EventHandler<PeerAddedEventArgs> OnPeerFound;

		#endregion

		#region Public Constructors
		/// <summary>
		/// Creates a new TorrentManager instance.
		/// </summary>
		/// <param name="torrent">The torrent to load in</param>
		/// <param name="settings">The settings to use for controlling connections</param>
		public TorrentManager(TorrentObject torrent, TorrentSettings settings)
			: this(torrent, settings, torrent.Files.Length == 1 ? string.Empty : torrent.Name, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TorrentManager"/> class.
		/// </summary>
		/// <param name="torrent">The torrent.</param>
		/// <param name="settings">The settings.</param>
		/// <param name="fastResumeData">The fast resume data.</param>
		public TorrentManager(TorrentObject torrent, TorrentSettings settings, FastResume fastResumeData)
			: this(torrent, settings, torrent.Files.Length == 1 ? string.Empty : torrent.Name, fastResumeData)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TorrentManager"/> class.
		/// </summary>
		/// <param name="torrent">The torrent.</param>
		/// <param name="settings">The settings.</param>
		/// <param name="baseDirectory">The base directory.</param>
		public TorrentManager(TorrentObject torrent, TorrentSettings settings, string baseDirectory)
			: this(torrent, settings, baseDirectory, null)
		{
		}

		/// <summary>
		/// Creates a new TorrentManager instance.
		/// </summary>
		/// <param name="torrent">The torrent to load in</param>
		/// <param name="settings">The settings to use for controlling connections</param>
		/// <param name="baseDirectory">In the case of a multi-file torrent, the name of the base directory containing the files. Defaults to Torrent.Name</param>
		/// <param name="fastResumeData">The fast resume data.</param>
		public TorrentManager(TorrentObject torrent, TorrentSettings settings, string baseDirectory, FastResume fastResumeData)
		{
			Check.Torrent(torrent);
			Check.Settings(settings);
			Check.BaseDirectory(baseDirectory);

			// Setup default save path
			if (settings.SavePath == null)
			{
				// Determine default when not specified in torrent or config
				settings.SavePath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
					"My Downloads");
			}

			_bitfield = new BitField(torrent.Pieces.Count);
			_fileManager = new FileManager(this, torrent.Files, torrent.PieceLength, settings.SavePath, baseDirectory);
			_finishedPieces = new Queue<int>();
			_shutdownTorrent = new CancellationTokenSource();
			_monitor = new ConnectionMonitor();
			_settings = settings;
			_inactivePeerManager = new InactivePeerManager(this, settings.TimeToWaitUntilIdle);
			_peers = new PeerManager();
			_pieceManager = new PieceManager(_bitfield, torrent.Files);
			_torrent = torrent;
			_trackerManager = new TrackerManager(this);
			_downloadLimiter = new RateLimiter();
			_uploadLimiter = new RateLimiter();

			if (fastResumeData != null)
			{
				LoadFastResume(fastResumeData);
			}

			if (ClientEngine.SupportsInitialSeed)
			{
				_initialSeed = (settings.InitialSeedingEnabled ? (new InitialSeed(this)) : null);
			}
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the bitfield.
		/// </summary>
		/// <value>The bitfield.</value>
		public BitField Bitfield
		{
			get
			{
				return _bitfield;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance can use DHT.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance can use DHT; otherwise, <c>false</c>.
		/// </value>
		public bool CanUseDht
		{
			get
			{
				return !_torrent.IsPrivate && _settings.UseDht;
			}
		}

		public bool IsComplete
		{
			get
			{
				return _bitfield.AllTrue;
			}
		}

		/// <summary>
		/// Gets or sets the engine.
		/// </summary>
		/// <value>The engine.</value>
		public ClientEngine Engine
		{
			get
			{
				return _engine;
			}
			internal set
			{
				_engine = value;
			}
		}

		/// <summary>
		/// Gets the file manager.
		/// </summary>
		/// <value>The file manager.</value>
		public FileManager FileManager
		{
			get
			{
				return _fileManager;
			}
		}

		/// <summary>
		/// Gets the peer review rounds complete.
		/// </summary>
		/// <value>The peer review rounds complete.</value>
		public int PeerReviewRoundsComplete
		{
			get
			{
				if (_chokeUnchoker != null)
					return _chokeUnchoker.ReviewsExecuted;
				else
					return 0;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is hash checked.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is hash checked; otherwise, <c>false</c>.
		/// </value>
		public bool IsHashChecked
		{
			get
			{
				return _hashChecked;
			}
			internal set
			{
				_hashChecked = value;
			}
		}

		/// <summary>
		/// Gets the number of hash failures.
		/// </summary>
		/// <value>The hash fails.</value>
		public int HashFails
		{
			get
			{
				return _hashFails;
			}
		}

		/// <summary>
		/// Gets the connection monitor.
		/// </summary>
		/// <value>The monitor.</value>
		public ConnectionMonitor Monitor
		{
			get
			{
				return _monitor;
			}
		}

		/// <summary>
		/// The number of peers that this torrent instance is connected to
		/// </summary>
		public int OpenConnections
		{
			get
			{
				return this.Peers.ConnectedPeers.Count;
			}
		}

		/// <summary>
		/// Gets the peer manager for this instance.
		/// </summary>
		/// <value>The peers.</value>
		public PeerManager Peers
		{
			get
			{
				return _peers;
			}
		}

		/// <summary>
		/// Gets the piece manager for this instance.
		/// </summary>
		/// <value>The piece manager.</value>
		public PieceManager PieceManager
		{
			get
			{
				return _pieceManager;
			}
		}

		/// <summary>
		/// The current progress of the torrent in percent
		/// </summary>
		/// <value>The progress.</value>
		public double Progress
		{
			get
			{
				return (_bitfield.PercentComplete);
			}
		}

		/// <summary>
		/// The directory to download the files to
		/// </summary>
		public string SavePath
		{
			get
			{
				return _fileManager.SavePath;
			}
		}

		/// <summary>
		/// The settings for with this TorrentManager
		/// </summary>
		public TorrentSettings Settings
		{
			get
			{
				return _settings;
			}
		}

		/// <summary>
		/// The current state of the TorrentManager
		/// </summary>
		public TorrentState State
		{
			get
			{
				return _state;
			}
		}

		/// <summary>
		/// The time the torrent manager was started at
		/// </summary>
		public DateTime? StartTime
		{
			get
			{
				return _startTime;
			}
		}

		public DateTime? StopTime
		{
			get
			{
				return _stopTime;
			}
		}

		/// <summary>
		/// Gets the tracker manager associated with this instance
		/// </summary>
		/// <value>The tracker manager.</value>
		public TrackerManager TrackerManager
		{
			get
			{
				return _trackerManager;
			}
		}

		/// <summary>
		/// Gets torrent associated with this instance.
		/// </summary>
		/// <value>The torrent.</value>
		public TorrentObject Torrent
		{
			get
			{
				return _torrent;
			}
		}

		/// <summary>
		/// The number of peers that we are currently uploading to
		/// </summary>
		/// <value>The uploading to.</value>
		public int UploadingTo
		{
			get
			{
				return _uploadingTo;
			}
			internal set
			{
				_uploadingTo = value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is initial seeding.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is initial seeding; otherwise, <c>false</c>.
		/// </value>
		public bool IsInitialSeeding
		{
			get
			{
				return _settings.InitialSeedingEnabled &&
					_state == TorrentState.Seeding &&
					ClientEngine.SupportsInitialSeed;
			}
		}

		/// <summary>
		/// Number of peers we have inactivated for this torrent
		/// </summary>
		public int InactivatedPeers
		{
			get
			{
				return _inactivePeerManager.InactivatedPeers;
			}
		}
		#endregion

		#region Internal Properties
		/// <summary>
		/// Gets the inactive peer manager for this instance.
		/// </summary>
		/// <value>The inactive peer manager.</value>
		internal InactivePeerManager InactivePeerManager
		{
			get
			{
				return _inactivePeerManager;
			}
		}

		internal InitialSeed InitialSeed
		{
			get
			{
				if (_initialSeed == null)
				{
					_initialSeed = new InitialSeed(this);
				}
				return _initialSeed;
			}
		}

		internal RateLimiter UploadLimiter
		{
			get
			{
				return _uploadLimiter;
			}
		}

		internal RateLimiter DownloadLimiter
		{
			get
			{
				return _downloadLimiter;
			}
		}

		/// <summary>
		/// Gets the shutdown token.
		/// </summary>
		/// <value>The shutdown token.</value>
		internal CancellationToken ShutdownToken
		{
			get
			{
				return _shutdownTorrent.Token;
			}
		}
		#endregion

		#region Public Methods

		/// <summary>
		/// Changes the picker.
		/// </summary>
		/// <param name="picker">The picker.</param>
		/// <returns></returns>
		public Task ChangePicker(PiecePickerBase picker)
		{
			Check.Picker(picker);

			return ClientEngine.MainLoop.QueueAsync(
				() =>
				{
					_pieceManager.ChangePicker(picker, _torrent.Files);
				});
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			DisposeManagedObjects();
		}

		/// <summary>
		/// Overrridden. Returns the name of the torrent.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.Torrent.Name;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			TorrentManager m = obj as TorrentManager;
			return (m == null) ? false : this.Equals(m);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(TorrentManager other)
		{
			return (other == null) ? false : _torrent.Equals(other._torrent);
		}

		public async Task<List<Piece>> GetActiveRequests()
		{
			return (List<Piece>)await ClientEngine.MainLoop.QueueAsync(
				() =>
				{
					return _pieceManager.PiecePicker.ExportActiveRequests();
				});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return Toolbox.HashCode(_torrent.infoHash);
		}

		public async Task<List<PeerId>> GetPeers()
		{
			return (List<PeerId>)await ClientEngine.MainLoop.QueueAsync(
				() =>
				{
					return new List<PeerId>(_peers.ConnectedPeers);
				});
		}

		/// <summary>
		/// Starts a hashcheck. If forceFullScan is false, the library will attempt to load fastresume data
		/// before performing a full scan, otherwise fast resume data will be ignored and a full scan will be started
		/// </summary>
		/// <param name="forceFullScan">True if a full hash check should be performed ignoring fast resume data</param>
		public void HashCheck(bool autoStart)
		{
			ClientEngine.MainLoop.QueueAsync(
				() =>
				{
					if (_state != TorrentState.Stopped)
					{
						throw new TorrentException(string.Format(
							"A hashcheck can only be performed when the manager is stopped. State is: {0}", _state));
					}

					CheckRegisteredAndDisposed();
					_startTime = DateTime.UtcNow;
					UpdateState(TorrentState.Hashing);

					Task.Run(() => PerformHashCheck(autoStart));
				});
		}


		/// <summary>
		/// Pauses the TorrentManager
		/// </summary>
		public Task Pause()
		{
			return ClientEngine.MainLoop.QueueAsync(
				() =>
				{
					CheckRegisteredAndDisposed();
					if (_state != TorrentState.Downloading && _state != TorrentState.Seeding)
						return;

					// By setting the state to "paused", peers will not be dequeued from the either the
					// sending or receiving queues, so no traffic will be allowed.
					UpdateState(TorrentState.Paused);
					this.SaveFastResume();
				});
		}


		/// <summary>
		/// Starts the TorrentManager
		/// </summary>
		public Task Start()
		{
			return ClientEngine.MainLoop.QueueAsync(
				() =>
				{
					CheckRegisteredAndDisposed();

					_engine.Start();
					// If the torrent was "paused", then just update the state to Downloading and forcefully
					// make sure the peers begin sending/receiving again
					if (_state == TorrentState.Paused)
					{
						UpdateState(TorrentState.Downloading);
						return;
					}

					// If the torrent has not been hashed, we start the hashing process then we wait for it to finish
					// before attempting to start again
					if (!_hashChecked)
					{
						if (_state != TorrentState.Hashing)
						{
							HashCheck(true);
						}
						return;
					}

					if (_state == TorrentState.Seeding || _state == TorrentState.Downloading)
						return;

					if (IsComplete)
					{
						UpdateState(TorrentState.Seeding);
					}
					else
					{
						UpdateState(TorrentState.Downloading);
					}

					if (TrackerManager.CurrentTracker != null)
					{
						if (_trackerManager.CurrentTracker.CanScrape)
						{
							TrackerManager.Scrape();
						}
						_trackerManager.Announce(TorrentEvent.Started); // Tell server we're starting
					}

					if (!_torrent.IsPrivate)
					{
						_engine.DhtEngine.PeersFound += DhtPeersFound;

						// First get some peers
						_engine.DhtEngine.GetPeers(_torrent.infoHash);

						// Second, get peers every 10 minutes (if we need them)
						ClientEngine.MainLoop.QueueRecurring(
							TimeSpan.FromMinutes(10),
							() =>
							{
								// Torrent is no longer active
								if (State != TorrentState.Seeding && State != TorrentState.Downloading)
								{
									return false;
								}

								// Only use DHT if it hasn't been (temporarily?) disabled in settings
								if (CanUseDht && Peers.AvailablePeers.Count < Settings.MaximumConnections)
								{
									_engine.DhtEngine.Announce(_torrent.infoHash, _engine.Settings.ListenPort);
									_engine.DhtEngine.GetPeers(_torrent.infoHash);
								}
								return true;
							});
					}

					_startTime = DateTime.UtcNow;
					if (_engine.ConnectionManager.IsRegistered(this))
					{
						Logger.Log(null, "TorrentManager - Error, this manager is already in the connectionmanager!");
					}
					else
					{
						_engine.ConnectionManager.RegisterManager(this);
					}
					_pieceManager.Reset();

					ClientEngine.MainLoop.QueueRecurring(
						TimeSpan.FromSeconds(2),
						() =>
						{
							if (State != TorrentState.Downloading && State != TorrentState.Seeding)
							{
								return false;
							}

							_pieceManager.PiecePicker.CancelTimedOutRequests();
							return true;
						});
				});
		}


		/// <summary>
		/// Stops the TorrentManager
		/// </summary>
		public Task Stop()
		{
			return ClientEngine.MainLoop.QueueAsync(
				async () =>
				{
					CheckRegisteredAndDisposed();

					_stopTime = DateTime.UtcNow;
					_shutdownTorrent.Cancel();
					try
					{
						if (_state == TorrentState.Stopped)
						{
							return;
						}

						if (!_torrent.IsPrivate)
						{
							_engine.DhtEngine.PeersFound -= DhtPeersFound;
						}

						if (_state == TorrentState.Hashing)
						{
							UpdateState(TorrentState.Stopped);
							return;
						}

						UpdateState(TorrentState.Stopped);

						System.Diagnostics.Debug.WriteLine("Shutting down torrent");
						if (_trackerManager.CurrentTracker != null)
						{
							await _trackerManager.Announce(TorrentEvent.Stopped, false);
							System.Diagnostics.Debug.WriteLine("Tracker announce completed");
						}

						System.Diagnostics.Debug.WriteLine("Closing peers");
						foreach (PeerId id in Peers.ConnectedPeers)
						{
							id.CloseConnectionImmediate();
						}
						_peers.ClearAll();

						System.Diagnostics.Debug.WriteLine("Closing files");
						await _engine.DiskManager.CloseFileStreams(FileManager.SavePath, Torrent.Files);

						System.Diagnostics.Debug.WriteLine("Saving fast resume");
						if (_hashChecked)
						{
							this.SaveFastResume();
						}

						System.Diagnostics.Debug.WriteLine("Unregistering with connection manager");
						_monitor.Reset();
						_pieceManager.Reset();
						if (_engine.ConnectionManager.IsRegistered(this))
						{
							_engine.ConnectionManager.UnregisterManager(this);
						}
						_engine.Stop();

						System.Diagnostics.Debug.WriteLine("Stop completed");
					}
					catch (Exception error)
					{
						System.Diagnostics.Debug.WriteLine(error.Message);
					}
					finally
					{

					}
				});
		}

		#endregion

		#region Protected Methods
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void DisposeManagedObjects()
		{
			if (_shutdownTorrent != null)
			{
				_shutdownTorrent.Dispose();
				_shutdownTorrent = null;
			}
			if (_pieceManager != null)
			{
				_pieceManager.Dispose();
				_pieceManager = null;
			}
		}
		#endregion

		#region Internal Methods
		internal Task SetPieceHashResult(Piece piece, bool isValid)
		{
			return ClientEngine.MainLoop.QueueAsync(
				() =>
				{
					// Update piece manager state and notify torrent manager
					Bitfield[piece.Index] = isValid;
					PieceManager.UnhashedPieces[piece.Index] = !isValid;
					HashedPiece(new PieceHashedEventArgs(this, piece.Index, isValid));

					// Build list of peers from whom we have requested blocks
					//	found in this piece.
					IEnumerable<PeerId> peerQuery =
						piece.Blocks
						.Select((block) => block.RequestedOff)
						.Where((peerId) => peerId != null && peerId.Connection != null)
						.Distinct();

					// Inform all peers that are still connected
					foreach (PeerId id in peerQuery)
					{
						if (id.Connection != null)
						{
							id.Peer.HashedPiece(isValid);
						}
					}

					// If the piece was successfully hashed, enqueue a new 
					//	"have" message to be sent out
					if (isValid)
					{
						_finishedPieces.Enqueue(piece.Index);
					}
				});
		}

		internal int AddPeers(IEnumerable<Peer> peers)
		{
			return peers.Sum((peer) => AddPeers(peer));
		}

		internal int AddPeers(Peer peer)
		{
			try
			{
				if (_peers.Contains(peer))
				{
					return 0;
				}

				// Ignore peers in the inactive list
				if (_inactivePeerManager.InactiveUris.Contains(peer.ConnectionUri))
				{
					return 0;
				}

				_peers.AvailablePeers.Add(peer);
				if (OnPeerFound != null)
				{
					OnPeerFound(this, new PeerAddedEventArgs(this, peer));
				}

				// When we successfully add a peer we try to connect to the next available peer
				return 1;
			}
			finally
			{
				ClientEngine e = _engine;
				if (e != null)
				{
					e.ConnectionManager.TryConnect();
				}
			}
		}

		internal void PreLogicTick(int counter)
		{
			_engine.ConnectionManager.TryConnect();

			// Execute iniitial logic for individual peers
			if (counter % (1000 / ClientEngine.TickLength) == 0)     // Call it every second... ish
			{
				_monitor.Tick();
			}

			if (_finishedPieces.Count > 0)
			{
				SendHaveMessagesToAll();
			}

			foreach (PeerId id in Peers.ConnectedPeers.Where((peer) => peer.Connection != null))
			{
				if (counter % (1000 / ClientEngine.TickLength) == 0)     // Call it every second... ish
				{
					id.Monitor.Tick();
				}
			}
		}

		internal void PostLogicTick(int counter)
		{
			DateTime nowTime = DateTime.UtcNow;
			DateTime fiftySecondsAgo = nowTime.AddSeconds(-50);
			DateTime nintySecondsAgo = nowTime.AddSeconds(-90);
			DateTime oneHundredAndEightySecondsAgo = nowTime.AddSeconds(-180);

			// Perform house-keeping tasks on all connected peers
			foreach (PeerId id in Peers.ConnectedPeers
				.Where((peer) => peer.Connection != null).ToArray())
			{
				// Process peer outbound message queue (if any)
				if (id.QueueLength > 0 && !id.ProcessingQueue)
				{
					id.ProcessingQueue = true;
					id.ConnectionManager.ProcessQueue(id);
				}

				// If we haven't sent anything to this peer in over 90secs then
				//	send a keep-alive message
				if (nintySecondsAgo > id.LastMessageSent)
				{
					id.LastMessageSent = DateTime.UtcNow;
					id.Enqueue(new KeepAliveMessage());
				}

				// If we have not received a message in over 3mins from this
				//	peer then disconnect
				if (oneHundredAndEightySecondsAgo > id.LastMessageReceived)
				{
					_engine.ConnectionManager.CleanupSocket(id, "Peer inactive");
					continue;
				}

				// If 50secs have elapsed since we last received a message and
				//	we have outstanding requests then disconnect
				if (fiftySecondsAgo > id.LastMessageReceived && id.AmRequestingPiecesCount > 0)
				{
					_engine.ConnectionManager.CleanupSocket(id, "Peer did not respond to piece requests");
					continue;
				}
			}

			// Perform house-keeping tasks on the current tracker
			if (_state == TorrentState.Seeding ||
				_state == TorrentState.Downloading)
			{
				_trackerManager.AnnounceIfNecessary();
			}

			// Update upload/download limits every second
			if (counter % (1000 / ClientEngine.TickLength) == 0)
			{
				_downloadLimiter.UpdateChunks(
					_settings.MaximumDownloadSpeed, _monitor.DownloadSpeed);
				_uploadLimiter.UpdateChunks(
					_settings.MaximumUploadSpeed, _monitor.UploadSpeed);
			}
		}

		internal void DownloadLogic(int counter)
		{
			//If download is complete, set state to 'Seeding'
			if (this.Progress == 100.0 && this.State != TorrentState.Seeding)
			{
				UpdateState(TorrentState.Seeding);
			}

			// FIXME: Hardcoded 15kB/sec - is this ok?
			if ((DateTime.UtcNow - _startTime) > TimeSpan.FromMinutes(1) && Monitor.DownloadSpeed < 15 * 1024)
			{
				foreach (string s in _torrent.GetRightHttpSeeds)
				{
					Uri uri = new Uri(s);
					Peer peer = new Peer(new string('0', 20), uri);
					HttpConnection connection = new HttpConnection(new Uri(s));
					connection.Manager = this;
					PeerId id = new PeerId(peer, this, connection);
					peer.IsSeeder = true;
					id.BitField.SetAll(true);
					id.Encryptor = new PlainTextEncryption();
					id.Decryptor = new PlainTextEncryption();
					id.IsChoking = false;
					_peers.ConnectedPeers.Add(id);
					NetworkIO.ReceiveMessageLoop(id);
				}

				// FIXME: In future, don't clear out this list. It may be useful to keep the list of HTTP seeds
				// Add a boolean or something so that we don't add them twice.
				_torrent.GetRightHttpSeeds.Clear();
			}

			// Remove inactive peers we haven't heard from if we're downloading
			if (_state == TorrentState.Downloading)
			{
				_inactivePeerManager.TimePassed();
			}

			// Now choke/unchoke peers; first instantiate the choke/unchoke manager if we haven't done so already
			if (_chokeUnchoker == null)
			{
				_chokeUnchoker = new ChokeUnchokeManager(this, this.Settings.MinimumTimeBetweenReviews, this.Settings.PercentOfMaxRateToSkipReview);
			}
			_chokeUnchoker.TimePassed();
		}

		internal void HashedPiece(PieceHashedEventArgs pieceHashedEventArgs)
		{
			if (!pieceHashedEventArgs.HashPassed)
			{
				Interlocked.Increment(ref _hashFails);
			}

			RaisePieceHashed(pieceHashedEventArgs);
		}

		internal void RaisePeerConnected(PeerConnectionEventArgs args)
		{
			Toolbox.RaiseAsyncEvent<PeerConnectionEventArgs>(PeerConnected, this, args);
		}

		internal void RaisePeerDisconnected(PeerConnectionEventArgs args)
		{
			Toolbox.RaiseAsyncEvent<PeerConnectionEventArgs>(PeerDisconnected, this, args);
		}

		internal void RaisePeersFound(PeersAddedEventArgs args)
		{
			Toolbox.RaiseAsyncEvent<PeersAddedEventArgs>(PeersFound, this, args);
		}

		internal void RaisePieceHashed(PieceHashedEventArgs args)
		{
			int index = args.PieceIndex;
			TorrentFile[] files = _torrent.Files;

			for (int i = 0; i < files.Length; i++)
			{
				if (index >= files[i].StartPieceIndex && index <= files[i].EndPieceIndex)
				{
					files[i].BitField[index - files[i].StartPieceIndex] = args.HashPassed;
				}
			}

			Toolbox.RaiseAsyncEvent<PieceHashedEventArgs>(PieceHashed, this, args);
		}

		internal void RaiseTorrentStateChanged(TorrentStateChangedEventArgs e)
		{
			// Whenever we have a state change, we need to make sure that we flush the buffers.
			// For example, Started->Paused, Started->Stopped, Downloading->Seeding etc should all
			// flush to disk.
			Toolbox.RaiseAsyncEvent<TorrentStateChangedEventArgs>(TorrentStateChanged, this, e);
		}

		/// <summary>
		/// Raise the connection attempt failed event
		/// </summary>
		/// <param name="args"></param>
		internal void RaiseConnectionAttemptFailed(PeerConnectionFailedEventArgs args)
		{
			Toolbox.RaiseAsyncEvent<PeerConnectionFailedEventArgs>(this.ConnectionAttemptFailed, this, args);
		}

		internal void SeedingLogic(int counter)
		{
			//Choke/unchoke peers; first instantiate the choke/unchoke manager if we haven't done so already
			if (_chokeUnchoker == null)
			{
				_chokeUnchoker = new ChokeUnchokeManager(this, this.Settings.MinimumTimeBetweenReviews, this.Settings.PercentOfMaxRateToSkipReview);
			}

			_chokeUnchoker.TimePassed();
		}

		internal void SetAmInterestedStatus(PeerId id, bool interesting)
		{
			if (interesting && !id.AmInterested)
			{
				id.AmInterested = true;
				id.Enqueue(new InterestedMessage());

				// He's interesting, so attempt to queue up any FastPieces (if that's possible)
				while (id.TorrentManager._pieceManager.AddPieceRequest(id))
				{
				}
			}
			else if (!interesting && id.AmInterested)
			{
				id.AmInterested = false;
				id.Enqueue(new NotInterestedMessage());
			}
		}
		#endregion

		#region Private Methods
		private void CheckRegisteredAndDisposed()
		{
			if (_engine == null)
			{
				throw new TorrentException("This manager has not been registed with an Engine");
			}
			if (_engine.Disposed)
			{
				throw new InvalidOperationException("The registered engine has been disposed");
			}
		}

		private void DhtPeersFound(object o, PeersFoundEventArgs e)
		{
			if (!CanUseDht || !Toolbox.ByteMatch(_torrent.InfoHash, e.InfoHash))
			{
				return;
			}

			int count = AddPeers(e.Peers);
			RaisePeersFound(new DhtPeersAdded(this, count, e.Peers.Count));
		}

		private async Task PerformHashCheck(bool autoStart)
		{
			try
			{
				// Store the value for whether the streams are open or not
				// If they are initially closed, we need to close them again after we hashcheck

				// We only need to hashcheck if at least one file already exists on the disk
				bool filesExist = _fileManager.CheckFilesExist();

				// A hashcheck should only be performed if some/all of the files exist on disk
				if (filesExist)
				{
					List<Task> subTasks = new List<Task>();
					for (int i = 0; i < _torrent.Pieces.Count; i++)
					{
						// This happens if the user cancels the hash by stopping the torrent.
						_shutdownTorrent.Token.ThrowIfCancellationRequested();
						subTasks.Add(ValidatePieceHash(i));
					}
					await TaskExtra.WhenAllOrEmpty(subTasks.ToArray());
				}
				else
				{
					_bitfield.SetAll(false);
					for (int i = 0; i < _torrent.Pieces.Count; i++)
					{
						// This happens if the user cancels the hash by stopping the torrent.
						_shutdownTorrent.Token.ThrowIfCancellationRequested();

						RaisePieceHashed(new PieceHashedEventArgs(this, i, false));
					}
				}

				_hashChecked = true;

				// This happens if the user cancels the hash by stopping the torrent.
				_shutdownTorrent.Token.ThrowIfCancellationRequested();

				if (autoStart)
				{
					Start();
				}
				else
				{
					UpdateState(TorrentState.Stopped);
				}
			}
			finally
			{
				// Ensure file streams are all closed after hashing
				_engine.DiskManager.Writer.Close(SavePath, _torrent.Files);
			}
		}

		private async Task ValidatePieceHash(int pieceIndex)
		{
			byte[] pieceHash = await _fileManager.GetHash(pieceIndex, true);
			_bitfield[pieceIndex] = _torrent.Pieces.IsValid(pieceHash, pieceIndex);

			// This happens if the user cancels the hash by stopping the torrent.
			_shutdownTorrent.Token.ThrowIfCancellationRequested();

			RaisePieceHashed(new PieceHashedEventArgs(this, pieceIndex, _bitfield[pieceIndex]));
		}

		internal async Task ValidatePieceHash(BufferedIO data)
		{
			Piece piece = data.Piece;

			// Hashcheck the piece as we now have all the blocks.
			byte[] pieceHash = await FileManager.GetHash(piece.Index, true);
			bool result = Torrent.Pieces.IsValid(pieceHash, piece.Index);

			await SetPieceHashResult(piece, result);
		}

		private void LoadFastResume(FastResume fastResumeData)
		{
			if (fastResumeData == null)
			{
				throw new ArgumentNullException("fastResumeData");
			}
			if (!Toolbox.ByteMatch(_torrent.infoHash, fastResumeData.InfoHash) || _torrent.Pieces.Count != fastResumeData.Bitfield.Length)
			{
				throw new ArgumentException("The fast resume data does not match this torrent", "fastResumeData");
			}

			for (int i = 0; i < _bitfield.Length; i++)
			{
				_bitfield[i] = fastResumeData.Bitfield[i];
			}

			for (int i = 0; i < _torrent.Pieces.Count; i++)
			{
				RaisePieceHashed(new PieceHashedEventArgs(this, i, _bitfield[i]));
			}

			_hashChecked = true;
		}

		public FastResume SaveFastResume()
		{
			return new FastResume(_torrent.infoHash, _bitfield, new List<Peer>());
		}

		private void SendHaveMessagesToAll()
		{
			// This is "Have Suppression" as defined in the spec.
			List<int> pieces;
			lock (_finishedPieces)
			{
				pieces = new List<int>(_finishedPieces);
				_finishedPieces.Clear();
			}

			for (int i = 0; i < this.Peers.ConnectedPeers.Count; i++)
			{
				if (this.Peers.ConnectedPeers[i].Connection == null)
				{
					continue;
				}

				MessageBundle bundle = new MessageBundle();

				foreach (int pieceIndex in pieces)
				{
					// If the peer has the piece already, we need to recalculate his "interesting" status.
					bool hasPiece = this.Peers.ConnectedPeers[i].BitField[pieceIndex];
					if (hasPiece)
					{
						bool isInteresting = _pieceManager.IsInteresting(this.Peers.ConnectedPeers[i]);
						SetAmInterestedStatus(this.Peers.ConnectedPeers[i], isInteresting);
					}

					// Check to see if have supression is enabled and send the have message accordingly
					if (!hasPiece || (hasPiece && !_engine.Settings.HaveSupressionEnabled))
					{
						bundle.Messages.Add(new HaveMessage(pieceIndex));
					}
				}

				this.Peers.ConnectedPeers[i].Enqueue(bundle);
			}
		}

		private void UpdateState(TorrentState newState)
		{
			if (_state != newState)
			{
				TorrentStateChangedEventArgs e = new TorrentStateChangedEventArgs(this, _state, newState);
				_state = newState;

				RaiseTorrentStateChanged(e);
			}
		}

		#endregion
	}
}
