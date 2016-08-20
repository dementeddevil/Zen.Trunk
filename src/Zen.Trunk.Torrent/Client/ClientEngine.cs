namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Text;
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Client.Managers;
	using Zen.Trunk.Torrent.Client.PieceWriters;
	using Zen.Trunk.Torrent.Common;
	using Zen.Trunk.Torrent.Dht;
	using Zen.Trunk.Torrent.Dht.Listeners;

	/// <summary>
	/// The Engine that contains the TorrentManagers
	/// </summary>
	public class ClientEngine : IDisposable
	{
		#region Public Fields
		public static readonly bool SupportsInitialSeed = true;
		public static readonly bool SupportsWebSeed = true;
		public static readonly bool SupportsExtended = true;
		public static readonly bool SupportsFastPeer = true;
		public static readonly bool SupportsEncryption = true;
		public static readonly bool SupportsEndgameMode = true;
		public static readonly bool SupportsDht = true;
		#endregion

		#region Internal Fields
		internal const int TickLength = 500;    // A logic tick will be performed every TickLength miliseconds
		internal static MainLoop MainLoop = new MainLoop("Client Engine Loop");
		internal static readonly BufferManager BufferManager = new BufferManager();
		#endregion

		#region Private Fields
		private static Random random = new Random();
		private readonly string _peerId;

		private bool _disposed;
		private bool _isRunning;
		private ListenManager _listenManager;         // Listens for incoming connections and passes them off to the correct TorrentManager
		private ConnectionManager _connectionManager;
		private CloneableList<TorrentManager> _torrents;
		private DiskManager _diskManager;
		private PeerListener _listener;
		private EngineSettings _settings;
		private int _tickCount;
		private DhtEngine _dhtEngine;
		private DhtListener _dhtListener;
		private RateLimiter _uploadLimiter;
		private RateLimiter _downloadLimiter;
		#endregion

		#region Events

		public event EventHandler<StatsUpdateEventArgs> StatsUpdate;
		public event EventHandler<CriticalExceptionEventArgs> CriticalException;

		public event EventHandler<TorrentEventArgs> TorrentRegistered;
		public event EventHandler<TorrentEventArgs> TorrentUnregistered;

		#endregion

		#region Constructors
		public ClientEngine(EngineSettings settings)
			: this(settings, new DiskWriter())
		{

		}

		public ClientEngine(EngineSettings settings, PieceWriter writer)
			: this(settings, new SocketListener(new IPEndPoint(IPAddress.Any, 0)), writer)
		{

		}

		public ClientEngine(EngineSettings settings, PeerListener listener)
			: this(settings, listener, new DiskWriter())
		{

		}

		public ClientEngine(EngineSettings settings, PeerListener listener, PieceWriter writer)
		{
			Check.Settings(settings);
			Check.Listener(listener);
			Check.Writer(writer);

			this._listener = listener;
			this._settings = settings;

			this._connectionManager = new ConnectionManager(this);
			this._dhtListener = new UdpListener(new IPEndPoint(settings.ListenAddress, settings.ListenPort));
			this._dhtEngine = new DhtEngine(_dhtListener);
			this._diskManager = new DiskManager(this, writer);
			this._listenManager = new ListenManager(this);
			MainLoop.QueueRecurring(
				TimeSpan.FromMilliseconds(TickLength),
				() =>
				{
					if (IsRunning && !_disposed)
					{
						LogicTick();
					}
					return !_disposed;
				});
			this._torrents = new CloneableList<TorrentManager>();
			this._downloadLimiter = new RateLimiter();
			this._uploadLimiter = new RateLimiter();
			this._peerId = GeneratePeerId();

			_listenManager.Register(listener);

			_dhtEngine.StateChanged += delegate
			{
				if (_dhtEngine.State != State.Ready)
				{
					return;
				}
				MainLoop.QueueAsync(
					() =>
					{
						foreach (TorrentManager manager in _torrents)
						{
							if (!manager.CanUseDht)
							{
								continue;
							}

							_dhtEngine.Announce(manager.Torrent.infoHash, Listener.LocalEndPoint.Port);
							_dhtEngine.GetPeers(manager.Torrent.infoHash);
						}
					});
			};

			// This means we created the listener in the constructor
			if (listener.LocalEndPoint.Port == 0)
			{
				listener.ChangeEndpoint(new IPEndPoint(settings.ListenAddress, settings.ListenPort));
			}

			listener.Start();

			_dhtListener.Start();
			_dhtEngine.Start();
		}
		#endregion

		#region Public Properties
		public ConnectionManager ConnectionManager
		{
			get
			{
				return this._connectionManager;
			}
		}

		public DhtEngine DhtEngine
		{
			get
			{
				return _dhtEngine;
			}
		}

		public DiskManager DiskManager
		{
			get
			{
				return _diskManager;
			}
		}

		public bool Disposed
		{
			get
			{
				return _disposed;
			}
		}

		public PeerListener Listener
		{
			get
			{
				return this._listener;
			}
		}

		public bool IsRunning
		{
			get
			{
				return this._isRunning;
			}
		}

		public string PeerId
		{
			get
			{
				return _peerId;
			}
		}

		public EngineSettings Settings
		{
			get
			{
				return this._settings;
			}
		}

		public int TotalDownloadSpeed
		{
			get
			{
				return _torrents.Sum((item) => item.Monitor.DownloadSpeed);
			}
		}

		public int TotalUploadSpeed
		{
			get
			{
				return _torrents.Sum((item) => item.Monitor.UploadSpeed);
			}
		}
		#endregion

		#region Internal Properties
		internal CloneableList<TorrentManager> Torrents
		{
			get
			{
				return this._torrents;
			}
			set
			{
				this._torrents = value;
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
		#endregion

		#region Public Methods
		public void ChangeListenEndpoint(IPEndPoint endpoint)
		{
			Check.Endpoint(endpoint);

			Settings.ListenPort = endpoint.Port;
			_dhtListener.ChangeEndpoint(endpoint);
			_listenManager.Listeners[0].ChangeEndpoint(endpoint);
		}

		public bool Contains(TorrentObject torrent)
		{
			CheckDisposed();
			if (torrent == null)
			{
				return false;
			}
			return _torrents.Exists((m) => m.Torrent.Equals(torrent));
		}

		public bool Contains(TorrentManager manager)
		{
			CheckDisposed();
			Check.Manager(manager);

			return Contains(manager.Torrent);
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;
			MainLoop.QueueAsync(
				() =>
				{
					this._dhtEngine.Dispose();
					this._dhtListener.Stop();
					this._diskManager.Dispose();
					this._listenManager.Dispose();
					MainLoop.Dispose();
					Toolbox.Shutdown();
				});
		}

		public Task PauseAll()
		{
			CheckDisposed();
			return MainLoop.QueueAsync(
				() =>
				{
					foreach (TorrentManager manager in _torrents)
					{
						manager.Pause();
					}
				});
		}

		public async Task Register(TorrentManager manager)
		{
			CheckDisposed();
			Check.Manager(manager);

			await MainLoop.QueueAsync(
				() =>
				{
					if (manager.Engine != null)
					{
						throw new TorrentException("This manager has already been registered");
					}
					if (Contains(manager.Torrent))
					{
						throw new TorrentException("A manager for this torrent has already been registered");
					}
					this._torrents.Add(manager);
					manager.PieceHashed += PieceHashed;
					manager.Engine = this;
				});

			if (TorrentRegistered != null)
			{
				TorrentRegistered(this, new TorrentEventArgs(manager));
			}

			if (manager.Settings.StartImmediately)
			{
				await MainLoop.QueueAsync(
					() =>
					{
						manager.Start();
					});
			}
		}

		public Task StartAll()
		{
			CheckDisposed();
			return MainLoop.QueueAsync(
				() =>
				{
					for (int i = 0; i < _torrents.Count; i++)
					{
						_torrents[i].Start();
					}
				});
		}

		public Task StopAll()
		{
			CheckDisposed();

			System.Threading.Tasks.TaskCompletionSource<object> tcs =
				new System.Threading.Tasks.TaskCompletionSource<object>();

			MainLoop.QueueAsync(
				() =>
				{
					List<Task> tasks = new List<Task>();
					for (int i = 0; i < _torrents.Count; i++)
					{
						tasks.Add(_torrents[i].Stop());
					}
					Task.WhenAll(tasks.ToArray())
						.ContinueWith((result) => tcs.SetFromTask(result));
				});

			return tcs.Task;
		}

		public async Task Unregister(TorrentManager manager)
		{
			CheckDisposed();
			Check.Manager(manager);

			await MainLoop.QueueAsync(
				() =>
				{
					if (manager.Engine != this)
					{
						throw new TorrentException("The manager has not been registered with this engine");
					}
					if (manager.State != TorrentState.Stopped)
					{
						throw new TorrentException("The manager must be stopped before it can be unregistered");
					}

					this._torrents.Remove(manager);

					manager.PieceHashed -= PieceHashed;
					manager.Engine = null;
				});

			if (TorrentUnregistered != null)
			{
				TorrentUnregistered(this, new TorrentEventArgs(manager));
			}
		}

		#endregion

		#region Internal methods
		internal void RaiseCriticalException(CriticalExceptionEventArgs e)
		{
			Toolbox.RaiseAsyncEvent<CriticalExceptionEventArgs>(CriticalException, this, e);
		}

		private void PieceHashed(object sender, PieceHashedEventArgs e)
		{
			_diskManager.QueueFlush(e.TorrentManager, e.PieceIndex);
		}

		internal void RaiseStatsUpdate(StatsUpdateEventArgs args)
		{
			Toolbox.RaiseAsyncEvent<StatsUpdateEventArgs>(StatsUpdate, this, args);
		}


		internal void Start()
		{
			CheckDisposed();
			_isRunning = true;
		}


		internal void Stop()
		{
			CheckDisposed();
			// If all the torrents are stopped, stop ticking
			_isRunning = _torrents.Exists(delegate(TorrentManager m)
			{
				return m.State != TorrentState.Stopped;
			});
		}
		#endregion

		#region Private Methods
		private static string GeneratePeerId()
		{
			StringBuilder sb = new StringBuilder(20);
			sb.Append(Common.VersionInfo.ClientVersion);
			lock (random)
			{
				for (int i = 0; i < 12; i++)
				{
					sb.Append(random.Next(0, 9));
				}
			}
			return sb.ToString();
		}

		private void CheckDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}
		}

		private void LogicTick()
		{
			_tickCount++;

			if (_tickCount % (1000 / TickLength) == 0)
			{
				_diskManager.WriteLimiter.UpdateChunks(_settings.MaxWriteRate, _diskManager.WriteRate);
				_diskManager.ReadLimiter.UpdateChunks(_settings.MaxReadRate, _diskManager.ReadRate);
				_downloadLimiter.UpdateChunks(_settings.GlobalMaxDownloadSpeed, TotalDownloadSpeed);
				_uploadLimiter.UpdateChunks(_settings.GlobalMaxUploadSpeed, TotalUploadSpeed);
			}

			for (int i = 0; i < this._torrents.Count; i++)
			{
				this._torrents[i].PreLogicTick(_tickCount);
				switch (this._torrents[i].State)
				{
					case (TorrentState.Downloading):
						this._torrents[i].DownloadLogic(_tickCount);
						break;

					case (TorrentState.Seeding):
						this._torrents[i].SeedingLogic(_tickCount);
						break;

					default:
						break;  // Do nothing.
				}
				this._torrents[i].PostLogicTick(_tickCount);
			}

			RaiseStatsUpdate(new StatsUpdateEventArgs());
		}
		#endregion
	}
}