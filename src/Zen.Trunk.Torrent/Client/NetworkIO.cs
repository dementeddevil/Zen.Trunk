namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Concurrent;
	using System.Net;
	using System.Threading;
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Client.Connections;
	using Zen.Trunk.Torrent.Client.Messages;
	using Zen.Trunk.Torrent.Common;

	internal static class NetworkIO
	{
		#region Private Types
		private class AsyncIO : TaskCompletionSource<int>
		{
			public AsyncIO(
				IConnection connection,
				byte[] buffer,
				int offset,
				int total,
				RateLimiter limiter,
				ConnectionMonitor managerMonitor,
				ConnectionMonitor peerMonitor)
			{
				Connection = connection;
				Buffer = buffer;
				Offset = offset;
				Count = 0;
				ManagerMonitor = managerMonitor;
				PeerMonitor = peerMonitor;
				RateLimiter = limiter;
				Total = total;
			}

			public byte[] Buffer;
			public IConnection Connection;
			public ConnectionMonitor ManagerMonitor;
			public int Count;
			public int Offset;
			public ConnectionMonitor PeerMonitor;
			public RateLimiter RateLimiter;
			public int Total;
		}
		#endregion

		#region Private Fields
		private static ConcurrentQueue<AsyncIO> _pendingReceiveQueue = new ConcurrentQueue<AsyncIO>();
		private static ConcurrentQueue<AsyncIO> _pendingSendQueue = new ConcurrentQueue<AsyncIO>();
		private static int _halfOpens;
		#endregion

		static NetworkIO()
		{
			ClientEngine.MainLoop.QueueRecurring(
				TimeSpan.FromMilliseconds(50),
				delegate
				{
					lock (_pendingSendQueue)
					{
						AsyncIO io;
						while (_pendingSendQueue.TryPeek(out io) &&
							io.RateLimiter.Chunks > 0)
						{
							_pendingSendQueue.TryDequeue(out io);
							EnqueueSend(io);
						}
					}
					lock (_pendingReceiveQueue)
					{
						AsyncIO io;
						while (_pendingReceiveQueue.TryPeek(out io) &&
							io.RateLimiter.Chunks > 0)
						{
							_pendingReceiveQueue.TryDequeue(out io);
							EnqueueReceive(io);
						}
					}
					return true;
				});
		}

		public static int HalfOpens
		{
			get
			{
				return _halfOpens;
			}
		}

		internal static async Task EnqueueConnect(IConnection connection, TorrentManager manager, Peer peer)
		{
			Interlocked.Increment(ref _halfOpens);
			try
			{
				await connection
					.ConnectAsync()
					.WithTimeout(TimeSpan.FromSeconds(30));
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("EnqueueConnect exception\n\t{0}", ex);
				connection.Dispose();
			}
			finally
			{
				Interlocked.Decrement(ref _halfOpens);
			}
		}

		internal static Task<int> EnqueueReceive(IConnection connection, ArraySegment<byte> buffer, int offset, int count)
		{
			return EnqueueReceive(connection, buffer, offset, count, null, null, null);
		}

		internal static Task<int> EnqueueReceive(IConnection connection, ArraySegment<byte> buffer, int offset, int count, RateLimiter limiter, ConnectionMonitor managerMonitor, ConnectionMonitor peerMonitor)
		{
			return EnqueueReceive(connection, buffer.Array, buffer.Offset + offset, count, limiter, managerMonitor, peerMonitor);
		}

		internal static Task<int> EnqueueReceive(IConnection connection, byte[] buffer, int offset, int count)
		{
			return EnqueueReceive(connection, buffer, offset, count, null, null, null);
		}

		internal static Task<int> EnqueueReceive(IConnection connection, byte[] buffer, int offset, int count, RateLimiter limiter, ConnectionMonitor managerMonitor, ConnectionMonitor peerMonitor)
		{
			AsyncIO io = new AsyncIO(connection, buffer, offset, count, limiter, managerMonitor, peerMonitor);
			return EnqueueReceive(io);
		}

		internal static Task<int> EnqueueSend(IConnection connection, ArraySegment<byte> buffer, int offset, int count)
		{
			return EnqueueSend(connection, buffer, offset, count, null, null, null);
		}

		internal static Task<int> EnqueueSend(IConnection connection, ArraySegment<byte> buffer, int offset, int count, RateLimiter limiter, ConnectionMonitor managerMonitor, ConnectionMonitor peerMonitor)
		{
			return EnqueueSend(connection, buffer.Array, buffer.Offset + offset, count, limiter, managerMonitor, peerMonitor);
		}

		internal static Task<int> EnqueueSend(IConnection connection, byte[] buffer, int offset, int count)
		{
			return EnqueueSend(connection, buffer, offset, count, null, null, null);
		}

		internal static Task<int> EnqueueSend(IConnection connection, byte[] buffer, int offset, int count, RateLimiter limiter, ConnectionMonitor managerMonitor, ConnectionMonitor peerMonitor)
		{
			AsyncIO io = new AsyncIO(connection, buffer, offset, count, limiter, managerMonitor, peerMonitor);
			return EnqueueSend(io);
		}

		private static async Task<int> EnqueueReceive(AsyncIO io)
		{
			try
			{
				// Keep going while we have data pending
				while (io.Count < io.Total)
				{
					Task<int> innerTask = null;
					if (io.RateLimiter == null)
					{
						innerTask = io.Connection.ReceiveAsync(
							io.Buffer,
							io.Offset + io.Count,
							io.Total - io.Count);
					}
					else if (io.RateLimiter.Chunks > 0)
					{
						if ((io.Total - io.Count) > ConnectionManager.ChunkLength / 2)
						{
							io.RateLimiter.DecrementChunks();
						}

						// Receive in 2kB (or less) chunks to allow rate limiting to work
						innerTask = io.Connection.ReceiveAsync(
							io.Buffer,
							io.Offset + io.Count,
							Math.Min(ConnectionManager.ChunkLength, io.Total - io.Count));
					}
					else
					{
						_pendingReceiveQueue.Enqueue(io);
						innerTask = io.Task;
					}

					// Wait for task to complete
					int count = await innerTask;

					// Update the monitors
					io.Count += count;
					if (io.PeerMonitor != null)
					{
						io.PeerMonitor.BytesReceived(
							count, io.Total > Piece.BlockSize / 2 ? TransferType.Data : TransferType.Protocol);
					}
					if (io.ManagerMonitor != null)
					{
						io.ManagerMonitor.BytesReceived(
							count, io.Total > Piece.BlockSize / 2 ? TransferType.Data : TransferType.Protocol);
					}

					// If the count is zero then abort
					if (count == 0 && io.Count < io.Total)
					{
						throw new InvalidOperationException("Failed to receive all data.");
					}
				}

				return io.Count;
			}
			catch (Exception ex)
			{
				io.TrySetException(ex);
				throw;
			}
		}

		private static async Task<int> EnqueueSend(AsyncIO io)
		{
			try
			{
				// Keep going while we have data pending
				while (io.Count < io.Total)
				{
					Task<int> innerTask;
					if (io.RateLimiter == null)
					{
						innerTask = io.Connection.SendAsync(
							io.Buffer,
							io.Offset + io.Count,
							io.Total - io.Count);
					}
					else if (io.RateLimiter.Chunks > 0)
					{
						if ((io.Total - io.Count) > ConnectionManager.ChunkLength / 2)
						{
							io.RateLimiter.DecrementChunks();
						}

						// Receive in 2kB (or less) chunks to allow rate limiting to work
						innerTask = io.Connection.SendAsync(
							io.Buffer,
							io.Offset + io.Count,
							Math.Min(ConnectionManager.ChunkLength, io.Total - io.Count));
					}
					else
					{
						_pendingSendQueue.Enqueue(io);
						innerTask = io.Task;
					}

					// Wait for task to complete
					int count = await innerTask;

					// Update the monitors
					io.Count += count;
					if (io.PeerMonitor != null)
					{
						io.PeerMonitor.BytesSent(
							count, io.Total > Piece.BlockSize / 2 ? TransferType.Data : TransferType.Protocol);
					}
					if (io.ManagerMonitor != null)
					{
						io.ManagerMonitor.BytesSent(
							count, io.Total > Piece.BlockSize / 2 ? TransferType.Data : TransferType.Protocol);
					}

					// If the count is zero then abort
					if (count == 0 && io.Count < io.Total)
					{
						throw new InvalidOperationException("Failed to send all data.");
					}
				}
				return io.Count;
			}
			catch (Exception ex)
			{
				io.TrySetException(ex);
				throw;
			}
		}

		public static async void ReceiveMessageLoop(PeerId id)
		{
			while (true)
			{
				// Get the connection object associated with the peer
				IConnection connection = id.Connection;
				if (connection == null)
				{
					return;
				}

				// Determine rate limiter to use
				RateLimiter limiter = id.Engine.Settings.GlobalMaxDownloadSpeed > 0 ? id.Engine.DownloadLimiter : null;
				limiter = limiter == null && id.TorrentManager.Settings.MaximumDownloadSpeed > 0 ? id.TorrentManager.DownloadLimiter : null;

				// Receive the message length
				id.AllocateReceiveBuffer(4);
				int count, messageBodyLength = 0;
				try
				{
					// Wait for the message length to arrive then decode
					count = await EnqueueReceive(connection, id.ReceiveBuffer, 0, 4, limiter, id.TorrentManager.Monitor, id.Monitor);

					// Decrypt the message length from the buffer.
					id.Decryptor.Decrypt(
						id.ReceiveBuffer.Array, id.ReceiveBuffer.Offset, count);

					// Get the message length as an integer
					messageBodyLength = IPAddress.NetworkToHostOrder(
						BitConverter.ToInt32(id.ReceiveBuffer.Array, id.ReceiveBuffer.Offset));
				}
				catch (Exception)
				{
					id.ConnectionManager.CleanupSocket(id, "Couldn't receive message length.");
					return;
				}

				// If bytes to receive is zero, it means we received a keep 
				//	alive message so we just start receiving a new message length again
				if (messageBodyLength == 0)
				{
					id.LastMessageReceived = DateTime.UtcNow;
					continue;
				}

				// Messages larger than 128Kb will cause the peer connection to
				//	be forcefully closed
				if (messageBodyLength > 128 * 1024)
				{
					id.ConnectionManager.CleanupSocket(id, "Message too large.");
					return;
				}

				// Otherwise queue the peer in the Receive buffer and try to resume downloading off him
				ArraySegment<byte> buffer = BufferManager.EmptyBuffer;
				ClientEngine.BufferManager.GetBuffer(ref buffer, messageBodyLength + 4);
				Buffer.BlockCopy(id.ReceiveBuffer.Array, id.ReceiveBuffer.Offset, buffer.Array, buffer.Offset, 4);
				id.ReceiveBuffer = buffer;
				try
				{
					count = await EnqueueReceive(connection, id.ReceiveBuffer, 4, messageBodyLength, limiter, id.TorrentManager.Monitor, id.Monitor);
				}
				catch
				{
					id.ConnectionManager.CleanupSocket(id, "Couldn't receive message body");
					return;
				}

				// The first 4 bytes are the already decrypted message length
				id.Decryptor.Decrypt(id.ReceiveBuffer.Array, id.ReceiveBuffer.Offset + 4, count);

				// Reset the peer receive buffer and send message for processing
				buffer = id.DetachReceiveBuffer();
				ClientEngine.MainLoop.QueueAsync(
					() =>
					{
						ProcessMessage(id, buffer, count);
					});
			}
		}

		private static void ProcessMessage(PeerId id, ArraySegment<byte> buffer, int count)
		{
			string reason = string.Empty;
			bool cleanUp = false;
			try
			{
				try
				{
					PeerMessage message = PeerMessage.DecodeMessage(buffer, 0, 4 + count, id.TorrentManager);

					// Fire the event to say we recieved a new message
					PeerMessageEventArgs e = new PeerMessageEventArgs(id.TorrentManager, (PeerMessage)message, Direction.Incoming, id);
					id.ConnectionManager.RaisePeerMessageTransferred(e);

					message.Handle(id);
				}
				catch (Exception ex)
				{
					// Should i nuke the peer with the dodgy message too?
					Logger.Log(null, "*CRITICAL EXCEPTION* - Error decoding message: {0}", ex);
				}
				finally
				{
					ClientEngine.BufferManager.FreeBuffer(ref buffer);
				}


				//FIXME: I thought i was using 5 (i changed the check below from 3 to 5)...
				// if the peer has sent us three bad pieces, we close the connection.
				if (id.Peer.TotalHashFails == 5)
				{
					reason = "5 hashfails";
					Logger.Log(id.Connection, "ConnectionManager - 5 hashfails");
					cleanUp = true;
					return;
				}

				id.LastMessageReceived = DateTime.UtcNow;
			}
			catch (TorrentException ex)
			{
				reason = ex.Message;
				Logger.Log(id.Connection, "Invalid message recieved: {0}", ex.Message);
				cleanUp = true;
				return;
			}
			finally
			{
				if (cleanUp)
				{
					id.ConnectionManager.CleanupSocket(id, reason);
				}
			}
		}
	}
}
