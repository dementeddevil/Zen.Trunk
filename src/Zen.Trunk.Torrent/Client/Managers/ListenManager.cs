namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Generic;
	using Zen.Trunk.Torrent.Client.Encryption;
	using Zen.Trunk.Torrent.Client.Messages;
	using Zen.Trunk.Torrent.Client.Messages.Standard;
	using Zen.Trunk.Torrent.Common;

	/// <summary>
	/// Instance methods of this class are threadsafe
	/// </summary>
	public class ListenManager : IDisposable
	{
		#region Member Variables

		private object locker;
		private ClientEngine engine;
		private CloneableList<PeerListener> listeners;

		#endregion Member Variables


		#region Properties

		public CloneableList<PeerListener> Listeners
		{
			get
			{
				return listeners;
			}
		}

		internal object Locker
		{
			get
			{
				return locker;
			}
			private set
			{
				locker = value;
			}
		}

		internal ClientEngine Engine
		{
			get
			{
				return engine;
			}
			private set
			{
				engine = value;
			}
		}

		#endregion Properties


		#region Constructors

		internal ListenManager(ClientEngine engine)
		{
			Engine = engine;
			Locker = new object();
			listeners = new CloneableList<PeerListener>();
		}

		#endregion Constructors


		#region Public Methods

		public void Dispose()
		{
		}

		public void Register(PeerListener listener)
		{
			listener.ConnectionReceived += new EventHandler<NewConnectionEventArgs>(ConnectionReceived);
		}

		public void Unregister(PeerListener listener)
		{
			listener.ConnectionReceived -= new EventHandler<NewConnectionEventArgs>(ConnectionReceived);
		}

		#endregion Public Methods

		private async void ConnectionReceived(object sender, NewConnectionEventArgs e)
		{
			if (engine.ConnectionManager.ShouldBanPeer(e.Peer))
			{
				e.Connection.Dispose();
				return;
			}

			PeerId id = new PeerId(e.Peer, e.TorrentManager, e.Connection);
			Logger.Log(id.Connection, "ListenManager - ConnectionReceived");
			if (!id.Connection.IsIncoming)
			{
				// FIXME I saw this once - how did it happen?
				// Probably the connection was cleaned up before
				// the delegate was invoked on the main thread.
				//if (id.Engine == null)
				ClientEngine.MainLoop.QueueAsync(
					() =>
					{
						id.ConnectionManager.ProcessFreshConnection(id);
					});
			}
			else
			{
				id.AllocateReceiveBuffer(68);
				id.BytesReceived = 0;
				id.BytesToReceive = 68;

				List<byte[]> skeys = new List<byte[]>();
				await ClientEngine.MainLoop.QueueAsync(
					() =>
					{
						for (int i = 0; i < engine.Torrents.Count; i++)
						{
							skeys.Add(engine.Torrents[i].Torrent.InfoHash);
						}
					});

				try
				{
					byte[] initialData = await EncryptorFactory.CheckEncryptionAsync(id, skeys.ToArray());
					if (initialData == null)
					{
						initialData = new byte[0];
					}

					id.BytesReceived += Message.Write(
						id.ReceiveBuffer.Array,
						id.ReceiveBuffer.Offset,
						initialData);

					int count;
					if (id.BytesToReceive != id.BytesReceived)
					{
						count = await NetworkIO.EnqueueReceive(
							id.Connection,
							id.ReceiveBuffer,
							initialData.Length,
							id.BytesToReceive - id.BytesReceived);
						if (count == 0)
						{
							throw new TorrentException("Connection dropped.");
						}

						id.BytesReceived += count;
					}

					Logger.Log(id.Connection, "ListenManager - Received handshake. Beginning to handle");
					TorrentManager man = null;
					HandshakeMessage handshake = new HandshakeMessage();

					// Nasty hack - If there is initial data on the connection, it's already decrypted
					// If there was no initial data, we need to decrypt it here
					handshake.Decode(id.ReceiveBuffer, 0, id.BytesToReceive);
					if (handshake.ProtocolString != VersionInfo.ProtocolStringV100)
					{
						id.Decryptor.Decrypt(id.ReceiveBuffer.Array, id.ReceiveBuffer.Offset, id.BytesToReceive);
						handshake.Decode(id.ReceiveBuffer, 0, id.BytesToReceive);
					}

					if (handshake.ProtocolString != VersionInfo.ProtocolStringV100)
					{
						throw new ProtocolException("Invalid protocol string in handshake");
					}

					await ClientEngine.MainLoop.QueueAsync(
						() =>
						{
							man = engine.Torrents.Find(
								(t) => Toolbox.ByteMatch(handshake.infoHash, t.Torrent.InfoHash));
						});

					// We're not hosting that torrent or the torrent is stopped
					if (man == null)
					{
						throw new TorrentException("Handshake requested nonexistent torrent.");
					}
					if (man.State == TorrentState.Stopped)
					{
						throw new TorrentException("Handshake requested for torrent which is not running");
					}

					id.Peer.PeerId = handshake.PeerId;
					id.TorrentManager = man;

					// If the handshake was parsed properly without encryption,
					//	then it definitely was not encrypted. If this is not allowed, abort
					if ((id.Encryptor is PlainTextEncryption &&
						!Toolbox.HasEncryption(engine.Settings.AllowedEncryption, EncryptionTypes.PlainText)) &&
						ClientEngine.SupportsEncryption)
					{
						throw new TorrentException("Encryption is required but was not active");
					}

					handshake.Handle(id);
					Logger.Log(id.Connection, "ListenManager - Handshake successfully handled");

					id.FreeReceiveBuffer();
					id.ClientApp = new Software(handshake.PeerId);

					MessageBundle bundle = new MessageBundle();
					bundle.Messages.Add(new HandshakeMessage(id.TorrentManager.Torrent.InfoHash, engine.PeerId, VersionInfo.ProtocolStringV100));
					bundle.Messages.Add(new BitfieldMessage(id.TorrentManager.Bitfield));

					id.AllocateSendBuffer(bundle.ByteLength);
					id.BytesSent = 0;
					id.BytesToSend = bundle.Encode(id.SendBuffer, 0);
					id.Encryptor.Encrypt(id.SendBuffer.Array, id.SendBuffer.Offset, id.BytesToSend);

					Logger.Log(id.Connection, "ListenManager - Sending connection to torrent manager");
					count = await NetworkIO.EnqueueSend(id.Connection, id.SendBuffer, 0, id.BytesToSend);
					await engine.ConnectionManager.IncomingConnectionAccepted(count, id);
				}
				catch (Exception ex)
				{
					Logger.Log(id.Connection, string.Format(
						"ListenManager - Exception occurred [{0}]",
						ex.Message));
					CleanupSocket(id);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		private void CleanupSocket(PeerId id)
		{
			if (id == null) // Sometimes onEncryptionError fires with a null id
			{
				return;
			}

			Logger.Log(id.Connection, "ListenManager - Cleaning up socket");
			id.CloseConnectionImmediate();
			id.FreeReceiveBuffer();
			id.FreeSendBuffer();
		}
	}
}
