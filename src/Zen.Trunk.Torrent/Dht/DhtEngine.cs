namespace Zen.Trunk.Torrent.Dht
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Bencoding;
	using Zen.Trunk.Torrent.Client;
	using Zen.Trunk.Torrent.Dht.Listeners;
	using Zen.Trunk.Torrent.Dht.Messages;
	using Zen.Trunk.Torrent.Dht.Tasks;

	internal enum ErrorCode : int
	{
		GenericError = 201,
		ServerError = 202,
		ProtocolError = 203,// malformed packet, invalid arguments, or bad token
		MethodUnknown = 204//Method Unknown
	}

	public class DhtEngine : IDisposable
	{
		#region Private Fields
		internal static MainLoop MainLoop = new MainLoop("DhtLoop");

		private bool _disposed;
		private bool _bootStrap = true;
		private TimeSpan _bucketRefreshTimeout = TimeSpan.FromMinutes(15);
		private MessageLoop _messageLoop;
		private State _state = State.NotReady;
		private RoutingTable _routingTable;
		private TimeSpan _timeout;
		private Dictionary<NodeId, List<Node>> _torrents = new Dictionary<NodeId, List<Node>>();
		private TokenManager _tokenManager;
		#endregion

		#region Public Events
		public event EventHandler<PeersFoundEventArgs> PeersFound;
		public event EventHandler StateChanged;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DhtEngine"/> class.
		/// </summary>
		/// <param name="listener">The listener.</param>
		public DhtEngine(DhtListener listener)
		{
			if (listener == null)
			{
				throw new ArgumentNullException("listener");
			}

			_routingTable = new RoutingTable(
				new Node(NodeId.Create(), listener.LocalEndPoint));
			_messageLoop = new MessageLoop(this, listener);
			_timeout = TimeSpan.FromSeconds(15); // 15 second message timeout by default
			_tokenManager = new TokenManager();
		}
		#endregion

		#region Properties

		internal bool Bootstrap
		{
			get
			{
				return _bootStrap;
			}
			set
			{
				_bootStrap = value;
			}
		}

		internal TimeSpan BucketRefreshTimeout
		{
			get
			{
				return _bucketRefreshTimeout;
			}
			set
			{
				_bucketRefreshTimeout = value;
			}
		}

		public bool Disposed
		{
			get
			{
				return _disposed;
			}
		}

		internal NodeId LocalId
		{
			get
			{
				return RoutingTable.LocalNode.Id;
			}
		}

		internal MessageLoop MessageLoop
		{
			get
			{
				return _messageLoop;
			}
		}

		internal RoutingTable RoutingTable
		{
			get
			{
				return _routingTable;
			}
		}

		public State State
		{
			get
			{
				return _state;
			}
		}

		public int NodeCount
		{
			get
			{
				return _routingTable.CountNodes();
			}
		}

		internal TimeSpan TimeOut
		{
			get
			{
				return _timeout;
			}
			set
			{
				_timeout = value;
			}
		}

		internal TokenManager TokenManager
		{
			get
			{
				return _tokenManager;
			}
		}

		internal Dictionary<NodeId, List<Node>> Torrents
		{
			get
			{
				return _torrents;
			}
		}

		#endregion Properties

		#region Methods
		internal void Add(IEnumerable<Node> nodes)
		{
			if (nodes == null)
			{
				throw new ArgumentNullException("nodes");
			}

			foreach (Node n in nodes)
			{
				Add(n);
			}
		}

		internal void Add(Node node)
		{
			if (node == null)
			{
				throw new ArgumentNullException("node");
			}

			SendQueryTask task = new SendQueryTask(
				this, new Ping(RoutingTable.LocalNode.Id), node);
			task.Execute();
		}

		internal Task Announce(byte[] infoHash, int port)
		{
			CheckDisposed();
			if (infoHash == null)
			{
				throw new ArgumentNullException("infoHash");
			}
			if (infoHash.Length != 20)
			{
				throw new ArgumentException("infoHash must be 20 bytes");
			}

			return new AnnounceTask(this, infoHash, port).Execute();
		}

		void CheckDisposed()
		{
			if (Disposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			// Ensure we don't break any threads actively running right now
			DhtEngine.MainLoop.QueueAsync(
				() =>
				{
					_disposed = true;
					DhtEngine.MainLoop.Dispose();
				});
		}

		internal void GetPeers(byte[] infoHash)
		{
			CheckDisposed();
			if (infoHash == null)
			{
				throw new ArgumentNullException("infoHash");
			}
			if (infoHash.Length != 20)
			{
				throw new ArgumentException("infoHash must be 20 bytes");
			}

			new GetPeersTask(this, infoHash).Execute();
		}

		internal void RaiseStateChanged(State newState)
		{
			_state = newState;

			if (StateChanged != null)
			{
				StateChanged(this, EventArgs.Empty);
			}
		}

		internal void RaisePeersFound(NodeId infoHash, List<Peer> peers)
		{
			if (PeersFound != null)
			{
				PeersFound(this, new PeersFoundEventArgs(infoHash.Bytes, peers));
			}
		}

		public async Task<byte[]> SaveNodes()
		{
			BEncodedList details = new BEncodedList();

			await MainLoop.QueueAsync(
				() =>
				{
					foreach (Bucket b in RoutingTable.Buckets)
					{
						foreach (Node n in b.Nodes.Where((item) => item.State != NodeState.Bad))
						{
							details.Add(n.CompactNode());
						}

						if (b.Replacement != null &&
							b.Replacement.State != NodeState.Bad)
						{
							details.Add(b.Replacement.CompactNode());
						}
					}
				});

			return details.Encode();
		}

		public void Start()
		{
			Start(null);
		}

		public void Start(byte[] initialNodes)
		{
			CheckDisposed();

			_messageLoop.Start();
			if (Bootstrap)
			{
				new InitialiseTask(this, initialNodes).Execute();
				RaiseStateChanged(State.Initialising);
				_bootStrap = false;
			}
			else
			{
				RaiseStateChanged(State.Ready);
			}

			DhtEngine.MainLoop.QueueRecurring(
				TimeSpan.FromMilliseconds(200),
				() =>
				{
					if (Disposed)
					{
						return false;
					}

					DateTime utcNow = DateTime.UtcNow;
					foreach (Bucket b in RoutingTable.Buckets
						.Where((bucket) => (utcNow - bucket.LastChanged) > BucketRefreshTimeout))
					{
						RefreshBucketTask task = new RefreshBucketTask(this, b);
						task.Execute();
					}
					return !Disposed;
				});
		}

		public void Stop()
		{
			_messageLoop.Stop();
		}
		#endregion
	}
}
