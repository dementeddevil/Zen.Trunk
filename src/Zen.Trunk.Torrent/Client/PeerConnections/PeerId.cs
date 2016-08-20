namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Client.Connections;
	using Zen.Trunk.Torrent.Client.Encryption;
	using Zen.Trunk.Torrent.Client.Messages;
	using Zen.Trunk.Torrent.Client.Messages.Libtorrent;
	using Zen.Trunk.Torrent.Common;
	using System.Threading;

	/// <summary>
	/// 
	/// </summary>
	public class PeerId //: IComparable<PeerIdInternal>
	{
		#region Private Fields
		/// <summary>
		/// When this peer was last unchoked, or null if we haven't unchoked it yet
		/// </summary>
		private DateTime? _lastUnchoked;

		/// <summary>
		/// Number of bytes downloaded when this peer was last reviewed
		/// </summary>
		private long _bytesDownloadedAtLastReview;

		/// <summary>
		/// Number of bytes uploaded when this peer was last reviewed
		/// </summary>
		private long _bytesUploadedAtLastReview;

		/// <summary>
		/// Download rate determined at the end of the last full review period
		/// when this peer was unchoked
		/// </summary>
		private double _lastReviewDownloadRate;

		/// <summary>
		/// Upload rate determined at the end of the last full review period
		/// when this peer was unchoked
		/// </summary>
		private double _lastReviewUploadRate;

		/// <summary>
		/// Set true if this is the first review period since this peer was
		/// last unchoked
		/// </summary>
		private bool _firstReviewPeriod;

		private CloneableList<int> _amAllowedFastPieces;
		private bool _amChoking;
		private bool _amInterested;
		private int _amRequestingPiecesCount;
		private BitField _bitField;
		private int _bytesReceived;
		private int _bytesSent;
		private int _bytesToReceive;
		private int _bytesToSend;
		private Software _clientApp;
		private PeerMessage _currentlySendingMessage;
		private IEncryption _decryptor;
		private IEncryption _encryptor;
		private string _disconnectReason;
		private ClientEngine _engine;
		private CloneableList<int> _isAllowedFastPieces;
		private bool _isChoking;
		private bool _isInterested;
		private int _isRequestingPiecesCount;
		private DateTime _lastMessageReceived;
		private DateTime _lastMessageSent;
		private DateTime _whenConnected;
		private CloneableList<ExtensionSupport> _extensionSupports;
		private int _maxPendingRequests;
		private ConnectionMonitor _monitor;
		private Peer _peer;
		private PeerExchangeManager _pexManager;
		private int _piecesSent;
		private int _piecesReceived;
		private ushort _port;
		private bool _processingQueue;
		private IConnection _connection;
		private ArraySegment<byte> _receiveBuffer = BufferManager.EmptyBuffer;      // The byte array used to buffer data while it's being received
		private ArraySegment<byte> _sendBuffer = BufferManager.EmptyBuffer;         // The byte array used to buffer data before it's sent
		private CloneableList<PeerMessage> _sendQueue;                  // This holds the peermessages waiting to be sent
		private CloneableList<int> _suggestedPieces;
		private bool _supportsFastPeer;
		private bool _supportsLTMessages;
		private TorrentManager _torrentManager;
		#endregion

		#region Constructors
		internal PeerId(Peer peer, TorrentManager manager, IConnection connection)
		{
			if (peer == null)
			{
				throw new ArgumentNullException("peer");
			}

			_connection = connection;
			_suggestedPieces = new CloneableList<int>();
			_amChoking = true;
			_isChoking = true;

			_isAllowedFastPieces = new CloneableList<int>();
			_amAllowedFastPieces = new CloneableList<int>();
			_lastMessageReceived = DateTime.UtcNow;
			_lastMessageSent = DateTime.UtcNow;
			_peer = peer;
			_monitor = new ConnectionMonitor();
			_sendQueue = new CloneableList<PeerMessage>(12);
			TorrentManager = manager;
			InitializeTyrant();
		}
		#endregion

		#region Properties
		internal IConnection Connection
		{
			get
			{
				return _connection;
			}
		}

		internal ArraySegment<byte> ReceiveBuffer
		{
			get
			{
				return _receiveBuffer;
			}
			set
			{
				if (_receiveBuffer != value)
				{
					if (_receiveBuffer != BufferManager.EmptyBuffer)
					{
						FreeReceiveBuffer();
					}
					_receiveBuffer = value;
				}
			}
		}

		internal ArraySegment<byte> SendBuffer
		{
			get
			{
				return _sendBuffer;
			}
			set
			{
				if (_sendBuffer != value)
				{
					if (_sendBuffer != BufferManager.EmptyBuffer)
					{
						FreeSendBuffer();
					}
					_sendBuffer = value;
				}
			}
		}

		internal DateTime? LastUnchoked
		{
			get
			{
				return _lastUnchoked;
			}
			set
			{
				_lastUnchoked = value;
			}
		}

		internal long BytesDownloadedAtLastReview
		{
			get
			{
				return _bytesDownloadedAtLastReview;
			}
			set
			{
				_bytesDownloadedAtLastReview = value;
			}
		}

		internal long BytesUploadedAtLastReview
		{
			get
			{
				return _bytesUploadedAtLastReview;
			}
			set
			{
				_bytesUploadedAtLastReview = value;
			}
		}

		internal double LastReviewDownloadRate
		{
			get
			{
				return _lastReviewDownloadRate;
			}
			set
			{
				_lastReviewDownloadRate = value;
			}
		}

		internal double LastReviewUploadRate
		{
			get
			{
				return _lastReviewUploadRate;
			}
			set
			{
				_lastReviewUploadRate = value;
			}
		}

		internal bool FirstReviewPeriod
		{
			get
			{
				return _firstReviewPeriod;
			}
			set
			{
				_firstReviewPeriod = value;
			}
		}

		internal byte[] AddressBytes
		{
			get
			{
				return Connection.AddressBytes;
			}
		}

		internal CloneableList<int> AmAllowedFastPieces
		{
			get
			{
				return _amAllowedFastPieces;
			}
			set
			{
				_amAllowedFastPieces = value;
			}
		}

		public bool AmChoking
		{
			get
			{
				return _amChoking;
			}
			internal set
			{
				_amChoking = value;
			}
		}

		public bool AmInterested
		{
			get
			{
				return _amInterested;
			}
			internal set
			{
				_amInterested = value;
			}
		}

		public int AmRequestingPiecesCount
		{
			get
			{
				return _amRequestingPiecesCount;
			}
			set
			{
				_amRequestingPiecesCount = value;
			}
		}

		public BitField BitField
		{
			get
			{
				return _bitField;
			}
			set
			{
				_bitField = value;
			}
		}

		internal int BytesReceived
		{
			get
			{
				return _bytesReceived;
			}
			set
			{
				_bytesReceived = value;
			}
		}

		internal int BytesSent
		{
			get
			{
				return _bytesSent;
			}
			set
			{
				_bytesSent = value;
			}
		}

		internal int BytesToReceive
		{
			get
			{
				return _bytesToReceive;
			}
			set
			{
				_bytesToReceive = value;
			}
		}

		internal int BytesToSend
		{
			get
			{
				return _bytesToSend;
			}
			set
			{
				_bytesToSend = value;
			}
		}

		public Software ClientApp
		{
			get
			{
				return _clientApp;
			}
			internal set
			{
				_clientApp = value;
			}
		}

		internal ConnectionManager ConnectionManager
		{
			get
			{
				return _engine.ConnectionManager;
			}
		}

		internal PeerMessage CurrentlySendingMessage
		{
			get
			{
				return _currentlySendingMessage;
			}
			set
			{
				_currentlySendingMessage = value;
			}
		}

		internal IEncryption Decryptor
		{
			get
			{
				return _decryptor;
			}
			set
			{
				_decryptor = value;
			}
		}

		internal string DisconnectReason
		{
			get
			{
				return _disconnectReason;
			}
			set
			{
				_disconnectReason = value;
			}
		}

		public IEncryption Encryptor
		{
			get
			{
				return _encryptor;
			}
			set
			{
				_encryptor = value;
			}
		}

		public ClientEngine Engine
		{
			get
			{
				return _engine;
				;
			}
		}

		internal CloneableList<ExtensionSupport> ExtensionSupports
		{
			get
			{
				return _extensionSupports;
			}
			set
			{
				_extensionSupports = value;
			}
		}

		public int HashFails
		{
			get
			{
				return _peer.TotalHashFails;
			}
		}

		internal CloneableList<int> IsAllowedFastPieces
		{
			get
			{
				return _isAllowedFastPieces;
			}
			set
			{
				_isAllowedFastPieces = value;
			}
		}

		public bool IsChoking
		{
			get
			{
				return _isChoking;
			}
			internal set
			{
				_isChoking = value;
			}
		}

		public bool IsConnected
		{
			get
			{
				return Connection != null;
			}
		}

		public bool IsInterested
		{
			get
			{
				return _isInterested;
			}
			internal set
			{
				_isInterested = value;
			}
		}

		public bool IsSeeder
		{
			get
			{
				return _bitField.AllTrue || _peer.IsSeeder;
			}
		}

		public int IsRequestingPiecesCount
		{
			get
			{
				return _isRequestingPiecesCount;
			}
			set
			{
				_isRequestingPiecesCount = value;
			}
		}

		internal DateTime LastMessageReceived
		{
			get
			{
				return _lastMessageReceived;
			}
			set
			{
				_lastMessageReceived = value;
			}
		}

		internal DateTime LastMessageSent
		{
			get
			{
				return _lastMessageSent;
			}
			set
			{
				_lastMessageSent = value;
			}
		}

		internal DateTime WhenConnected
		{
			get
			{
				return _whenConnected;
			}
			set
			{
				_whenConnected = value;
			}
		}

		internal int MaxPendingRequests
		{
			get
			{
				return _maxPendingRequests;
			}
			set
			{
				_maxPendingRequests = value;
			}
		}

		public ConnectionMonitor Monitor
		{
			get
			{
				return _monitor;
			}
		}

		internal Peer Peer
		{
			get
			{
				return _peer;
			}
			set
			{
				_peer = value;
			}
		}

		internal PeerExchangeManager PeerExchangeManager
		{
			get
			{
				return _pexManager;
			}
			set
			{
				_pexManager = value;
			}
		}

		public string PeerID
		{
			get
			{
				return _peer.PeerId;
			}
		}

		public int PiecesSent
		{
			get
			{
				return _piecesSent;
			}
			internal set
			{
				_piecesSent = value;
			}
		}

		public int PiecesReceived
		{
			get
			{
				return _piecesReceived;
			}
			internal set
			{
				_piecesReceived = value;
			}
		}

		internal ushort Port
		{
			get
			{
				return _port;
			}
			set
			{
				_port = value;
			}
		}

		internal bool ProcessingQueue
		{
			get
			{
				return _processingQueue;
			}
			set
			{
				_processingQueue = value;
			}
		}

		public bool SupportsFastPeer
		{
			get
			{
				return _supportsFastPeer;
			}
			internal set
			{
				_supportsFastPeer = value;
			}
		}

		public bool SupportsLTMessages
		{
			get
			{
				return _supportsLTMessages;
			}
			internal set
			{
				_supportsLTMessages = value;
			}
		}

		internal CloneableList<int> SuggestedPieces
		{
			get
			{
				return _suggestedPieces;
			}
		}

		public TorrentManager TorrentManager
		{
			get
			{
				return _torrentManager;
			}
			set
			{
				_torrentManager = value;
				if (value != null)
				{
					_engine = value.Engine;
					this.BitField = new BitField(value.Torrent.Pieces.Count);
				}
			}
		}

		public Uri Uri
		{
			get
			{
				return _peer.ConnectionUri;
			}
		}
		#endregion

		#region Methods
		internal void AllocateSendBuffer(int minLength)
		{
			ClientEngine.BufferManager.GetBuffer(ref _sendBuffer, minLength);
		}

		internal void FreeSendBuffer()
		{
			ClientEngine.BufferManager.FreeBuffer(ref _sendBuffer);
		}

		internal void AllocateReceiveBuffer(int minLength)
		{
			ClientEngine.BufferManager.GetBuffer(ref _receiveBuffer, minLength);
		}

		internal void FreeReceiveBuffer()
		{
			ClientEngine.BufferManager.FreeBuffer(ref _receiveBuffer);
		}

		internal ArraySegment<byte> DetachReceiveBuffer()
		{
			ArraySegment<byte> buffer = _receiveBuffer;
			_receiveBuffer = BufferManager.EmptyBuffer;
			return buffer;
		}

		public void CloseConnection()
		{
			ClientEngine.MainLoop.QueueAsync(
				() =>
				{
					CloseConnectionImmediate();
				});
		}

		internal void CloseConnectionImmediate()
		{
			IConnection connection = 
				Interlocked.Exchange(ref _connection, null);
			if (connection != null)
			{
				connection.Dispose();
			}
		}

		internal PeerMessage Dequeue()
		{
			return _sendQueue.Dequeue();
		}

		internal void Enqueue(PeerMessage msg)
		{
			_sendQueue.Add(msg);
			if (!_processingQueue)
			{
				_processingQueue = true;
				ConnectionManager.ProcessQueue(this);
			}
		}

		internal void EnqueueAt(PeerMessage message, int index)
		{
			if (_sendQueue.Count == 0 || index >= _sendQueue.Count)
			{
				Enqueue(message);
			}
			else
			{
				_sendQueue.Insert(index, message);
			}
		}

		public override bool Equals(object obj)
		{
			PeerId id = obj as PeerId;
			return id == null ? false : _peer.Equals(id._peer);
		}

		public override int GetHashCode()
		{
			return _peer.ConnectionUri.GetHashCode();
		}

		internal int QueueLength
		{
			get
			{
				return _sendQueue.Count;
			}
		}

		public Task SendMessageAsync(PeerMessage message)
		{
			if (message == null)
			{
				throw new ArgumentNullException("message");
			}

			if (Connection == null)
			{
				return CompletedTask.Default;
			}

			return ClientEngine.MainLoop.QueueAsync(
				() =>
				{
					if (Connection == null)
					{
						return;
					}

					Enqueue(message);
				});
		}

		public override string ToString()
		{
			return _peer.ConnectionUri.ToString();
		}

		#endregion

		#region BitTyrantasaurus implementation

		private const int MARKET_RATE = 7000;       // taken from reference BitTyrant implementation
		private RateLimiter rateLimiter;            // used to limit the upload capacity we give this peer
		private DateTime lastChokedTime;            // last time we looked that we were still choked
		private DateTime lastRateReductionTime;     // last time we reduced rate of this peer
		private int lastMeasuredDownloadRate;       // last download rate measured
		private long startTime;

		// stats
		private int maxObservedDownloadSpeed;
		private short roundsChoked, roundsUnchoked;     // for stats measurement

		private void InitializeTyrant()
		{
			this.haveMessagesReceived = 0;
			this.startTime = Stopwatch.GetTimestamp();

			this.rateLimiter = new RateLimiter();
			this.uploadRateForRecip = MARKET_RATE;
			this.lastMeasuredDownloadRate = 0;

			this.maxObservedDownloadSpeed = 0;
			this.roundsChoked = 0;
			this.roundsUnchoked = 0;
		}

		/// <summary>
		/// Measured from number of Have messages
		/// </summary>
		private int haveMessagesReceived;

		/// <summary>
		/// how much we have to send to this peer to guarantee reciprocation
		/// TODO: Can't allow upload rate to exceed this
		/// </summary>
		private int uploadRateForRecip;


		internal int HaveMessagesReceived
		{
			get
			{
				return this.haveMessagesReceived;
			}
			set
			{
				this.haveMessagesReceived = value;
			}
		}

		/// <summary>
		/// This is Up
		/// </summary>
		internal int UploadRateForRecip
		{
			get
			{
				return this.uploadRateForRecip;
			}
		}


		/// <summary>
		/// TGS CHANGE: Get the estimated download rate of this peer based on the rate at which he sends
		/// us Have messages. Note that this could be false if the peer has a malicious client.
		/// Units: Bytes/s
		/// </summary>
		internal int EstimatedDownloadRate
		{
			get
			{
				int timeElapsed = (int)new TimeSpan(Stopwatch.GetTimestamp() - this.startTime).TotalSeconds;
				return timeElapsed == 0 ? 0 : (this.haveMessagesReceived * this.TorrentManager.Torrent.PieceLength) / timeElapsed;
			}
		}

		/// <summary>
		/// This is the ratio of Dp to Up
		/// </summary>
		internal float Ratio
		{
			get
			{
				float downloadRate = (float)GetDownloadRate();
				return downloadRate / (float)uploadRateForRecip;
			}
		}

		/// <summary>
		/// Last time we looked that this peer was choking us
		/// </summary>
		internal DateTime LastChokedTime
		{
			get
			{
				return this.lastChokedTime;
			}
		}

		/// <summary>
		/// Used to check how much upload capacity we are giving this peer
		/// </summary>
		internal RateLimiter RateLimiter
		{
			get
			{
				return this.rateLimiter;
			}
		}

		internal short RoundsChoked
		{
			get
			{
				return this.roundsChoked;
			}
		}

		internal short RoundsUnchoked
		{
			get
			{
				return this.roundsUnchoked;
			}
		}

		/// <summary>
		/// Get our download rate from this peer -- this is Dp.
		/// 
		/// 1. If we are not choked by this peer, return the actual measure download rate.
		/// 2. If we are choked, then attempt to make an educated guess at the download rate using the following steps
		///     - use the rate of Have messages received from this peer as an estimate of its download rate
		///     - assume that its upload rate is equivalent to its estimated download rate
		///     - divide this upload rate by the standard implementation's active set size for that rate
		/// </summary>
		/// <returns></returns>
		internal int GetDownloadRate()
		{
			if (this.lastMeasuredDownloadRate > 0)
			{
				return this.lastMeasuredDownloadRate;
			}
			else
			{
				// assume that his upload rate will match his estimated download rate, and 
				// get the estimated active set size
				int estimatedDownloadRate = this.EstimatedDownloadRate;
				int activeSetSize = GetActiveSetSize(estimatedDownloadRate);

				return estimatedDownloadRate / activeSetSize;
			}
		}


		/// <summary>
		/// Should be called by ChokeUnchokeManager.ExecuteReview
		/// Logic taken from BitTyrant implementation
		/// </summary>
		internal void UpdateTyrantStats()
		{
			// if we're still being choked, set the time of our last choking
			if (_isChoking)
			{
				this.roundsChoked++;

				this.lastChokedTime = DateTime.UtcNow;
			}
			else
			{
				this.roundsUnchoked++;

				if (_amInterested)
				{
					//if we are interested and unchoked, update last measured download rate, unless it is 0
					if (this.Monitor.DownloadSpeed > 0)
					{
						this.lastMeasuredDownloadRate = this.Monitor.DownloadSpeed;

						this.maxObservedDownloadSpeed = Math.Max(this.lastMeasuredDownloadRate, this.maxObservedDownloadSpeed);
					}
				}
			}

			// last rate wasn't sufficient to achieve reciprocation
			if (!_amChoking && _isChoking && _isInterested) // only increase upload rate if he's interested, otherwise he won't request any pieces
			{
				this.uploadRateForRecip = (this.uploadRateForRecip * 12) / 10;
			}

			// we've been unchoked by this guy for a while....
			if (!_isChoking && !_amChoking
					&& (DateTime.UtcNow - lastChokedTime).TotalSeconds > 30
					&& (DateTime.UtcNow - lastRateReductionTime).TotalSeconds > 30)           // only do rate reduction every 30s
			{
				this.uploadRateForRecip = (this.uploadRateForRecip * 9) / 10;
				this.lastRateReductionTime = DateTime.UtcNow;
			}
		}


		/// <summary>
		/// Compares the actual upload rate with the upload rate that we are supposed to be limiting them to (UploadRateForRecip)
		/// </summary>
		/// <returns>True if the upload rate for recip is greater than the actual upload rate</returns>
		internal bool IsUnderUploadLimit()
		{
			return this.uploadRateForRecip > this.Monitor.UploadSpeed;
		}

		/// <summary>
		/// Stolen from reference BitTyrant implementation (see org.gudy.azureus2.core3.peer.TyrantStats)
		/// </summary>
		/// <param name="uploadRate">Upload rate of peer</param>
		/// <returns>Estimated active set size of peer</returns>
		internal static int GetActiveSetSize(int uploadRate)
		{
			if (uploadRate < 11)
			{
				return 2;
			}
			else if (uploadRate < 35)
			{
				return 3;
			}
			else if (uploadRate < 80)
			{
				return 4;
			}
			else if (uploadRate < 200)
			{
				return 5;
			}
			else if (uploadRate < 350)
			{
				return 6;
			}
			else if (uploadRate < 600)
			{
				return 7;
			}
			else if (uploadRate < 900)
			{
				return 8;
			}
			else
			{
				return 9;
			}
		}
		#endregion
	}
}
