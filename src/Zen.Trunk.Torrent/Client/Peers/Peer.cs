namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Net;
	using System.Text;
	using Zen.Trunk.Torrent.Bencoding;
	using Zen.Trunk.Torrent.Client.Encryption;
	using Zen.Trunk.Torrent.Common;

	public class Peer
	{
		#region Private Fields
		private int _cleanedUpCount;
		private Uri _connectionUri;
		private EncryptionTypes _encryption;
		private int _failedConnectionAttempts;
		private int _localPort;
		private int _totalHashFails;
		private bool _isSeeder;
		private string _peerId;
		private int _repeatedHashFails;
		private DateTime _lastConnectionAttempt;
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Peer"/> class.
		/// </summary>
		/// <param name="peerId">The peer id.</param>
		/// <param name="connectionUri">The connection URI.</param>
		public Peer(string peerId, Uri connectionUri)
			: this(peerId, connectionUri, EncryptionTypes.All)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Peer"/> class.
		/// </summary>
		/// <param name="peerId">The peer id.</param>
		/// <param name="connectionUri">The connection URI.</param>
		/// <param name="encryption">The encryption.</param>
		public Peer(string peerId, Uri connectionUri, EncryptionTypes encryption)
		{
			if (peerId == null)
			{
				throw new ArgumentNullException("peerId");
			}
			if (connectionUri == null)
			{
				throw new ArgumentNullException("connectionUri");
			}

			_connectionUri = connectionUri;
			_encryption = encryption;
			_peerId = peerId;
		}
		#endregion

		#region Public Properties
		public Uri ConnectionUri
		{
			get
			{
				return _connectionUri;
			}
		}

		internal int CleanedUpCount
		{
			get
			{
				return this._cleanedUpCount;
			}
			set
			{
				this._cleanedUpCount = value;
			}
		}

		public EncryptionTypes Encryption
		{
			get
			{
				return _encryption;
			}
			set
			{
				_encryption = value;
			}
		}

		internal int TotalHashFails
		{
			get
			{
				return this._totalHashFails;
			}
		}

		internal string PeerId
		{
			get
			{
				return _peerId;
			}
			set
			{
				_peerId = value;
			}
		}

		internal bool IsSeeder
		{
			get
			{
				return this._isSeeder;
			}
			set
			{
				this._isSeeder = value;
			}
		}

		internal int FailedConnectionAttempts
		{
			get
			{
				return this._failedConnectionAttempts;
			}
			set
			{
				this._failedConnectionAttempts = value;
			}
		}

		internal int LocalPort
		{
			get
			{
				return _localPort;
			}
			set
			{
				_localPort = value;
			}
		}

		internal DateTime LastConnectionAttempt
		{
			get
			{
				return this._lastConnectionAttempt;
			}
			set
			{
				this._lastConnectionAttempt = value;
			}
		}

		internal int RepeatedHashFails
		{
			get
			{
				return this._repeatedHashFails;
			}
		}
		#endregion



		public override bool Equals(object obj)
		{
			return Equals(obj as Peer);
		}

		public bool Equals(Peer other)
		{
			if (other == null)
				return false;

			// FIXME: Don't compare the port, just compare the IP
			if (string.IsNullOrEmpty(_peerId) || string.IsNullOrEmpty(other._peerId))
			{
				return this._connectionUri.Host.Equals(other._connectionUri.Host);
			}

			return _peerId == other._peerId;
		}

		public override int GetHashCode()
		{
			return this._connectionUri.Host.GetHashCode();
		}

		public override string ToString()
		{
			return this._connectionUri.ToString();
		}

		internal byte[] CompactPeer()
		{
			byte[] data = new byte[6];
			CompactPeer(data, 0);
			return data;
		}

		internal void CompactPeer(byte[] data, int offset)
		{
			Buffer.BlockCopy(IPAddress.Parse(this._connectionUri.Host).GetAddressBytes(), 0, data, offset, 4);
			Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(((short)this._connectionUri.Port))), 0, data, offset + 4, 2);
		}

		internal void HashedPiece(bool succeeded)
		{
			if (succeeded && _repeatedHashFails > 0)
			{
				_repeatedHashFails--;
			}

			if (!succeeded)
			{
				_repeatedHashFails++;
				_totalHashFails++;
			}
		}

		internal static CloneableList<Peer> Decode(BEncodedList peers)
		{
			CloneableList<Peer> list = new CloneableList<Peer>(peers.Count);
			foreach (BEncodedValue value in peers)
			{
				try
				{
					if (value is BEncodedDictionary)
					{
						list.Add(DecodeFromDict((BEncodedDictionary)value));
					}
					else if (value is BEncodedString)
					{
						foreach (Peer p in Decode((BEncodedString)value))
						{
							list.Add(p);
						}
					}
				}
				catch
				{
					// If something is invalid and throws an exception, ignore it
					// and continue decoding the rest of the peers
				}
			}
			return list;
		}

		private static Peer DecodeFromDict(BEncodedDictionary dict)
		{
			string peerId;

			if (dict.ContainsKey("peer id"))
			{
				peerId = dict["peer id"].ToString();
			}
			else if (dict.ContainsKey("peer_id"))       // HACK: Some trackers return "peer_id" instead of "peer id"
			{
				peerId = dict["peer_id"].ToString();
			}
			else
			{
				peerId = string.Empty;
			}

			Uri connectionUri = new Uri("tcp://" + dict["ip"].ToString() + ":" + dict["port"].ToString());
			return new Peer(peerId, connectionUri, EncryptionTypes.All);
		}

		internal static CloneableList<Peer> Decode(BEncodedString peers)
		{
			// "Compact Response" peers are encoded in network byte order. 
			// IP's are the first four bytes
			// Ports are the following 2 bytes
			byte[] byteOrderedData = peers.TextBytes;
			int i = 0;
			UInt16 port;
			StringBuilder sb = new StringBuilder(27);
			CloneableList<Peer> list = new CloneableList<Peer>((byteOrderedData.Length / 6) + 1);
			while ((i + 5) < byteOrderedData.Length)
			{
				sb.Remove(0, sb.Length);

				sb.Append("tcp://");
				sb.Append(byteOrderedData[i++]);
				sb.Append('.');
				sb.Append(byteOrderedData[i++]);
				sb.Append('.');
				sb.Append(byteOrderedData[i++]);
				sb.Append('.');
				sb.Append(byteOrderedData[i++]);

				port = (UInt16)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(byteOrderedData, i));
				i += 2;
				sb.Append(':');
				sb.Append(port);

				Uri uri = new Uri(sb.ToString());
				list.Add(new Peer("", uri, EncryptionTypes.All));
			}

			return list;
		}
	}
}