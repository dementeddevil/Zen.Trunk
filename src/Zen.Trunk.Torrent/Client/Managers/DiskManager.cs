namespace Zen.Trunk.Torrent.Client.Managers
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Client.PieceWriters;
	using Zen.Trunk.Torrent.Common;

	public class DiskManager : IDisposable
	{
		#region Private Fields
		private static MainLoop IOLoop = new MainLoop("Disk IO");
		private bool _disposed;
		private ClientEngine _engine;
		private PieceWriter _writer;

		private ConcurrentQueue<BufferedIO> _bufferedReads =
			new ConcurrentQueue<BufferedIO>();
		private RateMonitor _readMonitor = new RateMonitor();
		private RateLimiter _readLimiter = new RateLimiter();

		private ConcurrentQueue<BufferedIO> _bufferedWrites =
			new ConcurrentQueue<BufferedIO>();
		private RateMonitor _writeMonitor = new RateMonitor();
		private RateLimiter _writeLimiter = new RateLimiter();
		#endregion

		#region Internal Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DiskManager"/> class.
		/// </summary>
		/// <param name="engine">The engine.</param>
		/// <param name="writer">The writer.</param>
		internal DiskManager(ClientEngine engine, PieceWriter writer)
		{
			_engine = engine;
			_writer = writer;

			IOLoop.QueueRecurring(
				TimeSpan.FromMilliseconds(5),
				() =>
				{
					do
					{
						BufferedIO write;
						while ((engine.Settings.MaxWriteRate == 0 || _writeLimiter.Chunks > 0 || _disposed) &&
							_bufferedWrites.TryDequeue(out write))
						{
							_writeLimiter.AdjustChunks(-write.Buffer.Count / ConnectionManager.ChunkLength);
							PerformWrite(write);
						}

						BufferedIO read;
						while ((engine.Settings.MaxReadRate == 0 || _readLimiter.Chunks > 0) &&
							_bufferedReads.TryDequeue(out read))
						{
							if (_disposed)
							{
								read.TrySetException(
									new OperationCanceledException("DiskManager pending shutdown"));
							}
							else
							{
								_readLimiter.AdjustChunks(-read.Count / ConnectionManager.ChunkLength);
								PerformRead(read);
							}
						}
					} while (_disposed && _bufferedWrites.Count > 0);

					if (_disposed)
					{
						_writer.Dispose();
						return false;
					}
					return true;
				});

			IOLoop.QueueRecurring(
				TimeSpan.FromSeconds(1),
				() =>
				{
					if (_disposed)
					{
						return false;
					}

					_readMonitor.Tick();
					_writeMonitor.Tick();
					return true;
				});
		}
		#endregion

		#region Public Properties
		public bool Disposed
		{
			get
			{
				return _disposed;
			}
		}

		public int QueuedWrites
		{
			get
			{
				return this._bufferedWrites.Count;
			}
		}

		public int ReadRate
		{
			get
			{
				return _readMonitor.Rate;
			}
		}

		public int WriteRate
		{
			get
			{
				return _writeMonitor.Rate;
			}
		}

		public long TotalRead
		{
			get
			{
				return _readMonitor.Total;
			}
		}

		public long TotalWritten
		{
			get
			{
				return _writeMonitor.Total;
			}
		}
		#endregion

		#region Internal Properties
		internal PieceWriter Writer
		{
			get
			{
				return _writer;
			}
		}

		internal RateLimiter ReadLimiter
		{
			get
			{
				return _readLimiter;
			}
		}

		internal RateLimiter WriteLimiter
		{
			get
			{
				return _writeLimiter;
			}
		}
		#endregion

		#region Public Methods
		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				IOLoop.Dispose();
			}
		}
		#endregion

		#region Internal Methods
		internal Task CloseFileStreams(string path, TorrentFile[] files)
		{
			TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
			IOLoop.QueueAsync(
				async () =>
				{
					try
					{
						// Dump all buffered reads for the manager we're closing the streams for

						// Enqueue all items from old read queue that don't match
						ConcurrentQueue<BufferedIO> oldBufferedReads =
							Interlocked.Exchange(
								ref _bufferedReads,
								new ConcurrentQueue<BufferedIO>());
						BufferedIO io;
						while (oldBufferedReads.TryDequeue(out io))
						{
							if (io.Files == files)
							{
								io.TrySetCanceled();
							}
							else
							{
								_bufferedReads.Enqueue(io);
							}
						}

						ConcurrentQueue<BufferedIO> oldBufferedWrites =
							Interlocked.Exchange(
								ref _bufferedWrites,
								new ConcurrentQueue<BufferedIO>());
						List<Task> subTasks = new List<Task>();
						while (oldBufferedWrites.TryDequeue(out io))
						{
							if (io.Files == files)
							{
								PerformWrite(io);
								subTasks.Add(io.Task);
							}
							else
							{
								_bufferedWrites.Enqueue(io);
							}
						}
						await TaskExtra.WhenAllOrEmpty(subTasks.ToArray());

						// Close the disk writer for these files
						_writer.Close(path, files);
						tcs.TrySetResult(null);
					}
					catch (Exception ex)
					{
						tcs.TrySetException(ex);
					}
				});
			return tcs.Task;
		}

		internal async Task<int> ReadAsync(TorrentManager manager, byte[] buffer, int bufferOffset, long pieceStartIndex, int bytesToRead)
		{
			string path = manager.FileManager.SavePath;
			ArraySegment<byte> b = new ArraySegment<byte>(buffer, bufferOffset, bytesToRead);
			BufferedIO io = new BufferedIO(
				b,
				pieceStartIndex,
				bytesToRead,
				manager.Torrent.PieceLength,
				manager.Torrent.Files,
				path);
			IOLoop.QueueAsync(
				() =>
				{
					PerformRead(io);
				});
			await io.Task;
			return io.ActualCount;
		}

		internal void QueueFlush(TorrentManager manager, int index)
		{
			IOLoop.QueueAsync(
				() =>
				{
					_writer.Flush(manager.FileManager.SavePath, manager.Torrent.Files, index);
				});
		}

		internal void QueueRead(BufferedIO io)
		{
			_bufferedReads.Enqueue(io);
		}

		internal void QueueWrite(BufferedIO io)
		{
			_bufferedWrites.Enqueue(io);
		}
		#endregion

		#region Private Methods
		private void PerformRead(BufferedIO io)
		{
			try
			{
				io.ActualCount = _writer.ReadChunk(io);
				_readMonitor.AddDelta(io.ActualCount);
				io.TrySetResult(null);
			}
			catch (Exception error)
			{
				io.TrySetException(error);
			}
		}

		private void PerformWrite(BufferedIO data)
		{
			PeerId id = data.Id;
			Piece piece = data.Piece;
			try
			{
				// Perform the actual write
				_writer.Write(data);
				_writeMonitor.AddDelta(data.Count);

				// Find the block that this data belongs to and set it's state to "Written"
				int index = data.PieceOffset / Piece.BlockSize;
				piece.Blocks[index].Written = true;

				// Raise message and notify original caller operation has completed
				id.TorrentManager.FileManager.RaiseBlockWritten(new BlockEventArgs(data));
				data.TrySetResult(null);
			}
			catch (Exception error)
			{
				data.TrySetException(error);
			}

			// If we haven't written all the pieces to disk or we are shutting
			//	down then there is no point in hash checking
			if (!piece.AllBlocksWritten || _disposed)
			{
				return;
			}

			// Validate the hash
			// NOTE: Do NOT await on this method
			data.Id.TorrentManager.ValidatePieceHash(data);
		}
		#endregion
	}
}
