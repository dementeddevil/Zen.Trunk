namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Client.Connections;
	using Zen.Trunk.Torrent.Client.Encryption;
	using Zen.Trunk.Torrent.Client.Messages;
	using Zen.Trunk.Torrent.Client.Messages.FastPeer;
	using Zen.Trunk.Torrent.Client.Messages.Libtorrent;
	using Zen.Trunk.Torrent.Client.Messages.Standard;
	using Zen.Trunk.Torrent.Common;

	/// <summary>
	/// Main controller class for all incoming and outgoing connections
	/// </summary>
	public class ConnectionManager
	{
		#region Private Fields
		internal static readonly int ChunkLength = 2096 + 64;   // Download in 2kB chunks to allow for better rate limiting
		private ClientEngine _engine;
		private CloneableList<TorrentManager> _torrents;
		#endregion

		#region Events
		/// <summary>
		/// Event that is fired when requesting whether a peer should be banned
		/// </summary>
		public event EventHandler<AttemptConnectionEventArgs> BanPeer;

		/// <summary>
		/// Event that is fired every time a message is sent or received from a peer.
		/// </summary>
		public event EventHandler<PeerMessageEventArgs> PeerMessageTransferred;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionManager"/> class.
		/// </summary>
		/// <param name="engine">The engine.</param>
		public ConnectionManager(ClientEngine engine)
		{
			_engine = engine;
			_torrents = new CloneableList<TorrentManager>();
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// The number of half open connections
		/// </summary>
		public int HalfOpenConnections
		{
			get
			{
				return NetworkIO.HalfOpens;
			}
		}


		/// <summary>
		/// The maximum number of half open connections
		/// </summary>
		public int MaxHalfOpenConnections
		{
			get
			{
				return _engine.Settings.GlobalMaxHalfOpenConnections;
			}
		}


		/// <summary>
		/// The number of open connections
		/// </summary>
		public int OpenConnections
		{
			get
			{
				return _torrents.Sum((item) => item.Peers.ConnectedPeers.Count);
			}
		}


		/// <summary>
		/// The maximum number of open connections
		/// </summary>
		public int MaxOpenConnections
		{
			get
			{
				return _engine.Settings.GlobalMaxConnections;
			}
		}
		#endregion

		#region Async Connection Methods
		internal async Task ConnectToPeer(TorrentManager manager, Peer peer)
		{
			// Connect to the peer.
			IConnection connection = ConnectionFactory.Create(peer.ConnectionUri);
			if (connection == null)
			{
				return;
			}

			peer.LastConnectionAttempt = DateTime.UtcNow;
			manager.Peers.ConnectingToPeers.Add(peer);
			try
			{
				await NetworkIO.EnqueueConnect(connection, manager, peer);
				if (manager.State != TorrentState.Downloading &&
					manager.State != TorrentState.Seeding)
				{
					connection.Dispose();
				}
			}
			catch
			{
				Logger.Log(null, "ConnectionManager - Failed to connect{0}", peer);

				manager.RaiseConnectionAttemptFailed(
					new PeerConnectionFailedEventArgs(
						manager, peer, Direction.Outgoing, "ConnectToPeer"));

				peer.FailedConnectionAttempts++;
				connection.Dispose();
				manager.Peers.BusyPeers.Add(peer);
				return;
			}
			finally
			{
				manager.Peers.ConnectingToPeers.Remove(peer);
			}

			// Pass connection for processing
			PeerId id = new PeerId(peer, manager, connection);
			manager.Peers.ActivePeers.Add(peer);

			Logger.Log(id.Connection, "ConnectionManager - Connection opened");
			await ProcessFreshConnection(id);

			// Try to connect to another peer
			//TryConnect();
		}

		internal async Task ProcessFreshConnection(PeerId id)
		{
			bool cleanUp = false;
			string reason = null;
			try
			{
				// If we have too many open connections, close the connection
				if (OpenConnections > this.MaxOpenConnections)
				{
					Logger.Log(id.Connection, "ConnectionManager - Too many connections");
					reason = "Too many connections";
					cleanUp = true;
					return;
				}

				id.ProcessingQueue = true;

				// Add peer to the connected list
				id.TorrentManager.Peers.ConnectedPeers.Add(id);
				id.WhenConnected = DateTime.UtcNow;

				// Check peer encryption
				await CheckEncryption(id);

				// Send handshake
				await SendHandshake(id);
			}
			catch (Exception error)
			{
				Logger.Log(id.Connection, string.Format(
					"Failed to setup encryption/handshake {0}", error));

				id.TorrentManager.RaiseConnectionAttemptFailed(
					new PeerConnectionFailedEventArgs(
						id.TorrentManager, 
						id.Peer, 
						Direction.Outgoing, 
						"ProcessFreshConnection: failed to setup encryption/handshake"));

				cleanUp = true;
			}
			finally
			{
				// Decrement the half open connections
				if (cleanUp)
				{
					CleanupSocket(id, reason);
				}
			}
		}

		private async Task CheckEncryption(PeerId id)
		{
			try
			{
				// Determine encryption state
				byte[] initialData = await EncryptorFactory.CheckEncryptionAsync(id);
				if (initialData != null && initialData.Length > 0)
				{
					throw new EncryptionException("Unhandled initial data");
				}

				EncryptionTypes e = _engine.Settings.AllowedEncryption;
				if (id.Encryptor is RC4 && !Toolbox.HasEncryption(e, EncryptionTypes.RC4Full) ||
					id.Encryptor is RC4Header && !Toolbox.HasEncryption(e, EncryptionTypes.RC4Header) ||
					id.Encryptor is PlainTextEncryption && !Toolbox.HasEncryption(e, EncryptionTypes.PlainText))
				{
					throw new EncryptionException("Unable to select encryption scheme.");
				}
			}
			catch(Exception error)
			{
				id.Peer.Encryption &= ~EncryptionTypes.RC4Full;
				id.Peer.Encryption &= ~EncryptionTypes.RC4Header;
				throw new Exception("Failed encryption check", error);
			}
		}

		private async Task SendHandshake(PeerId id)
		{
			try
			{
				// Create a handshake message to send to the peer
				HandshakeMessage handshake = new HandshakeMessage(
					id.TorrentManager.Torrent.InfoHash,
					_engine.PeerId,
					VersionInfo.ProtocolStringV100);
				await SendMessage(id, handshake);

				id.TorrentManager.RaisePeerConnected(
					new PeerConnectionEventArgs(id.TorrentManager, id, Direction.Outgoing));
				Logger.Log(id.Connection, "ConnectionManager - Sent Handshake");

				// Receive the handshake
				// FIXME: Will fail if protocol version changes. FIX THIS
				//ClientEngine.BufferManager.FreeBuffer(ref id.sendBuffer);
				id.AllocateReceiveBuffer(68);
				int count = await NetworkIO.EnqueueReceive(id.Connection, id.ReceiveBuffer, 0, 68);

				// Decode the handshake and handle it
				id.Decryptor.Decrypt(id.ReceiveBuffer.Array, id.ReceiveBuffer.Offset, count);
				PeerMessage msg = new HandshakeMessage();
				msg.Decode(id.ReceiveBuffer, 0, count);
				msg.Handle(id);

				Logger.Log(id.Connection, "ConnectionManager - Handshake received");
				if (id.SupportsFastPeer && ClientEngine.SupportsFastPeer)
				{
					if (id.TorrentManager.Bitfield.AllFalse || id.TorrentManager.IsInitialSeeding)
					{
						msg = new HaveNoneMessage();
					}
					else if (id.TorrentManager.Bitfield.AllTrue)
					{
						msg = new HaveAllMessage();
					}
					else
					{
						msg = new BitfieldMessage(id.TorrentManager.Bitfield);
					}
				}
				else if (id.TorrentManager.IsInitialSeeding)
				{
					BitField btfld = new BitField(id.TorrentManager.Bitfield.Length);
					btfld.SetAll(false);
					msg = new BitfieldMessage(btfld);
				}
				else
				{
					msg = new BitfieldMessage(id.TorrentManager.Bitfield);
				}

				if (id.SupportsLTMessages)
				{
					MessageBundle bundle = new MessageBundle();
					bundle.Messages.Add(new ExtendedHandshakeMessage());
					bundle.Messages.Add(msg);
					msg = bundle;
				}

				await SendMessage(id, msg);

				if (id.Connection == null)
				{
					return;
				}

				// Now we will enqueue a FastPiece message for each piece we
				//	will allow the peer to download even if they are choked
				if (ClientEngine.SupportsFastPeer && id.SupportsFastPeer)
				{
					for (int i = 0; i < id.AmAllowedFastPieces.Count; i++)
					{
						id.Enqueue(new AllowedFastMessage(id.AmAllowedFastPieces[i]));
					}
				}

				// Allow the auto processing of the send queue to commence
				if (id.QueueLength > 0)
				{
					id.ConnectionManager.ProcessQueue(id);
				}
				else
				{
					id.ProcessingQueue = false;
				}

				// Begin the infinite looping to receive messages
				id.FreeReceiveBuffer();
				NetworkIO.ReceiveMessageLoop(id);
			}
			catch (Exception error)
			{
				throw new Exception("Failed handshake", error);
			}
		}

		private async Task SendMessage(PeerId id, PeerMessage message)
		{
			try
			{
				if (id.Connection == null)
				{
					return;
				}

				id.AllocateSendBuffer(message.ByteLength);
				id.CurrentlySendingMessage = message;
				if (message is PieceMessage)
				{
					id.IsRequestingPiecesCount--;
				}

				id.BytesSent = 0;
				id.BytesToSend = message.Encode(id.SendBuffer, 0);
				id.Encryptor.Encrypt(id.SendBuffer.Array, id.SendBuffer.Offset, id.BytesToSend);

				RateLimiter limiter = _engine.Settings.GlobalMaxUploadSpeed > 0 ? _engine.UploadLimiter : null;
				limiter = limiter ?? (id.TorrentManager.Settings.MaximumUploadSpeed > 0 ? id.TorrentManager.UploadLimiter : null);

				int count = await NetworkIO.EnqueueSend(
					id.Connection,
					id.SendBuffer,
					id.BytesSent,
					id.BytesToSend,
					limiter,
					id.TorrentManager.Monitor,
					id.Monitor);

				// If the peer has disconnected, don't continue
				if (id.Connection == null)
				{
					return;
				}

				id.BytesSent += count;
			}
			catch (Exception error)
			{
				throw new Exception("Failed to send message", error);
			}
		}
		#endregion

		#region Methods
		internal void AsyncCleanupSocket(PeerId id, bool localClose, string message)
		{
			if (id == null) // Sometimes onEncryptoError will fire with a null id
				return;

			try
			{
				// It's possible the peer could be in an async send *and* receive and so end up
				// in this block twice. This check makes sure we don't try to double dispose.
				if (id.Connection == null)
				{
					return;
				}

				// We can reuse this peer if the connection says so and it's not marked as inactive
				bool canResuse = id.Connection.CanReconnect &&
					!id.TorrentManager.InactivePeerManager.InactiveUris.Contains(id.Uri);
				Logger.Log(id.Connection, "Cleanup Reason : " + message);
				Logger.Log(id.Connection, "*******Cleaning up*******");
				id.TorrentManager.PieceManager.RemoveRequests(id);
				id.Peer.CleanedUpCount++;

				if (id.PeerExchangeManager != null)
				{
					id.PeerExchangeManager.Dispose();
				}

				id.FreeReceiveBuffer();
				id.FreeSendBuffer();

				if (!id.AmChoking)
				{
					id.TorrentManager.UploadingTo--;
				}

				id.CloseConnectionImmediate();
				id.TorrentManager.Peers.ConnectedPeers.RemoveAll((peer) => id == peer);

				if (id.TorrentManager.Peers.ActivePeers.Contains(id.Peer))
				{
					id.TorrentManager.Peers.ActivePeers.Remove(id.Peer);
				}

				// If we get our own details, this check makes sure we don't try connecting to ourselves again
				if (canResuse && id.Peer.PeerId != _engine.PeerId)
				{
					if (!id.TorrentManager.Peers.AvailablePeers.Contains(id.Peer) &&
						id.Peer.CleanedUpCount < 5)
					{
						id.TorrentManager.Peers.AvailablePeers.Insert(0, id.Peer);
					}
				}
			}

			finally
			{
				id.TorrentManager.RaisePeerDisconnected(
					new PeerConnectionEventArgs(id.TorrentManager, id, Direction.None, message));
				TryConnect();
			}
		}

		/// <summary>
		/// This method is called when a connection needs to be closed and the resources for it released.
		/// </summary>
		/// <param name="id">The peer whose connection needs to be closed</param>
		internal void CleanupSocket(PeerId id, string message)
		{
			ClientEngine.MainLoop.QueueAsync(
				() =>
				{
					id.ConnectionManager.AsyncCleanupSocket(id, true, message);
				});
		}

		/// <summary>
		/// This method is called when the ClientEngine recieves a valid incoming connection
		/// </summary>
		/// <param name="result"></param>
		internal async Task IncomingConnectionAccepted(int count, PeerId id)
		{
			string reason = null;
			bool cleanUp = false;

			try
			{
				// Make sure we have sent all we needed to
				while (true)
				{
					id.BytesSent += count;
					if (count != id.BytesToSend)
					{
						count = await NetworkIO.EnqueueSend(
							id.Connection,
							id.SendBuffer,
							id.BytesSent,
							id.BytesToSend - id.BytesSent);
					}
					else
					{
						break;
					}
				}

				if (id.Peer.PeerId == _engine.PeerId) // The tracker gave us our own IP/Port combination
				{
					Logger.Log(id.Connection, "ConnectionManager - Recieved myself");
					reason = "Received myself";
					cleanUp = true;
					return;
				}

				if (id.TorrentManager.Peers.ActivePeers.Contains(id.Peer))
				{
					Logger.Log(id.Connection, "ConnectionManager - Already connected to peer");
					id.Connection.Dispose();
					return;
				}

				Logger.Log(id.Connection, "ConnectionManager - Incoming connection fully accepted");
				id.TorrentManager.Peers.AvailablePeers.Remove(id.Peer);
				id.TorrentManager.Peers.ActivePeers.Add(id.Peer);
				id.TorrentManager.Peers.ConnectedPeers.Add(id);
				id.WhenConnected = DateTime.UtcNow;

				//ClientEngine.BufferManager.FreeBuffer(ref id.sendBuffer);
				id.TorrentManager.RaisePeerConnected(
					new PeerConnectionEventArgs(id
						.TorrentManager,
						id,
						Direction.Incoming));

				if (OpenConnections >= Math.Min(this.MaxOpenConnections, id.TorrentManager.Settings.MaximumConnections))
				{
					reason = "Too many peers";
					cleanUp = true;
					return;
				}
				if (id.TorrentManager.IsInitialSeeding)
				{
					int pieceIndex = id.TorrentManager.InitialSeed.GetNextPieceForPeer(id);
					if (pieceIndex != -1)
					{
						// If the peer has the piece already, we need to recalculate his "interesting" status.
						bool hasPiece = id.BitField[pieceIndex];

						// Check to see if have supression is enabled and send the have message accordingly
						if (!hasPiece ||
							(hasPiece && !_engine.Settings.HaveSupressionEnabled))
						{
							id.Enqueue(new HaveMessage(pieceIndex));
						}
					}
				}
				Logger.Log(id.Connection, "ConnectionManager - Receiving message length");
				id.AllocateReceiveBuffer(68);
				NetworkIO.ReceiveMessageLoop(id);
			}
			catch (Exception e)
			{
				reason = "Exception for incoming connection: {0}" + e.Message;
				cleanUp = true;
			}
			finally
			{
				if (cleanUp)
				{
					CleanupSocket(id, reason);

					id.TorrentManager.RaiseConnectionAttemptFailed(
						new PeerConnectionFailedEventArgs(id.TorrentManager, id.Peer, Direction.Incoming, reason));
				}
			}
		}

		/// <summary>
		/// This method should be called to begin processing messages stored in the SendQueue
		/// </summary>
		/// <param name="id">The peer whose message queue you want to start processing</param>
		internal async Task ProcessQueue(PeerId id)
		{
			try
			{
				while (id.QueueLength > 0)
				{
					PeerMessage msg = id.Dequeue();
					if (msg is PieceMessage)
					{
						id.PiecesSent++;
					}

					await SendMessage(id, msg);

					// If the peer has been cleaned up, just return.
					if (id.Connection == null)
					{
						return;
					}

					// Fire the event to let the user know a message was sent
					RaisePeerMessageTransferred(
						new PeerMessageEventArgs(
							id.TorrentManager,
							(PeerMessage)id.CurrentlySendingMessage,
							Direction.Outgoing,
							id));

					//ClientEngine.BufferManager.FreeBuffer(ref id.sendBuffer);
					id.LastMessageSent = DateTime.UtcNow;
				}
			}
			catch (Exception e)
			{
				CleanupSocket(id, "Exception calling SendMessage: " + e.Message);
			}
			finally
			{
				id.ProcessingQueue = false;
			}
		}

		internal void RaisePeerMessageTransferred(PeerMessageEventArgs e)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				EventHandler<PeerMessageEventArgs> handler = PeerMessageTransferred;

				if (!(e.Message is MessageBundle))
				{
					if (handler != null)
					{
						handler(e.TorrentManager, e);
					}
					return;
				}

				// Message bundles are only a convience for internal usage!
				MessageBundle b = (MessageBundle)e.Message;
				foreach (PeerMessage message in b.Messages)
				{
					PeerMessageEventArgs args = new PeerMessageEventArgs(
						e.TorrentManager, message, e.Direction, e.ID);
					if (handler != null)
					{
						handler(args.TorrentManager, args);
					}
				}
			});
		}

		internal void RegisterManager(TorrentManager torrentManager)
		{
			if (_torrents.Contains(torrentManager))
			{
				throw new TorrentException("TorrentManager is already registered in the connection manager");
			}

			_torrents.Add(torrentManager);
			TryConnect();
		}

		internal bool ShouldBanPeer(Peer peer)
		{
			bool result = false;
			EventHandler<AttemptConnectionEventArgs> handler = BanPeer;
			if (handler != null)
			{
				AttemptConnectionEventArgs e =
					new AttemptConnectionEventArgs(peer);
				handler(this, e);
				result = e.BanPeer;
			}
			return result;
		}

		internal Task TryConnect()
		{
			try
			{
				// If we have already reached our max connections globally, don't try to connect to a new peer
				if ((OpenConnections >= this.MaxOpenConnections) ||
					(this.HalfOpenConnections >= this.MaxHalfOpenConnections))
				{
					return CompletedTask.Default;
				}

				// Check each torrent manager in turn to see if they have any peers we want to connect to
				foreach (TorrentManager manager in _torrents)
				{
					// If we have reached the max peers allowed for this torrent, don't connect to a new peer for this torrent
					if (manager.Peers.ConnectedPeers.Count >= manager.Settings.MaximumConnections)
					{
						continue;
					}

					// If the torrent isn't active, don't connect to a peer for it
					if (manager.State != TorrentState.Downloading &&
						manager.State != TorrentState.Seeding)
					{
						continue;
					}

					// If we are not seeding, we can connect to anyone.
					// If we are seeding, we should only connect to a peer
					//	if they are not a seeder.
					int peerIndex;
					for (peerIndex = 0; peerIndex < manager.Peers.AvailablePeers.Count; peerIndex++)
					{
						if (manager.State == TorrentState.Seeding && manager.Peers.AvailablePeers[peerIndex].IsSeeder)
						{
							continue;
						}
						else
						{
							break;
						}
					}

					// If this is true, there were no peers in the available list to connect to.
					if (peerIndex == manager.Peers.AvailablePeers.Count)
					{
						continue;
					}

					// Remove the peer from the lists so we can start connecting to it
					Peer peer = manager.Peers.AvailablePeers[peerIndex];
					manager.Peers.AvailablePeers.RemoveAt(peerIndex);

					// If the peer is banned then return
					// NOTE: Peer has been removed from the available list
					if (ShouldBanPeer(peer))
					{
						continue;
					}

					// Put the manager at the end of the list so we try the other ones next
					_torrents.Remove(manager);
					_torrents.Add(manager);

					// Connect to the peer
					return ConnectToPeer(manager, peer);
				}
			}
			catch (Exception ex)
			{
				_engine.RaiseCriticalException(new CriticalExceptionEventArgs(ex, _engine));
			}
			return CompletedTask.Default;
		}

		internal void UnregisterManager(TorrentManager torrentManager)
		{
			if (!_torrents.Contains(torrentManager))
			{
				throw new TorrentException("TorrentManager is not registered in the connection manager");
			}

			_torrents.Remove(torrentManager);
		}

		internal bool IsRegistered(TorrentManager torrentManager)
		{
			return _torrents.Contains(torrentManager);
		}
		#endregion
	}
}
