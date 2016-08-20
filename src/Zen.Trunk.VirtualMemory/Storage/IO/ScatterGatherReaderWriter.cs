namespace Zen.Trunk.Storage.IO
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// <c>ScatterGatherReaderWriter</c> optimises buffer persistence by
	/// grouping reads and writes on sequential buffers together and performing
	/// them in one overlapped operation.
	/// </summary>
	public sealed class ScatterGatherReaderWriter : IDisposable
	{
		#region Private Fields
		private AdvancedFileStream _stream;
		private StreamScatterGatherHelper _readBuffers;
		private StreamScatterGatherHelper _writeBuffers;
		private CancellationTokenSource _shutdown;
		private Task _cleanupTask;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="T:ScatterGatherReaderWriter"/> class.
		/// </summary>
		/// <param name="stream">Underlying stream object.</param>
		public ScatterGatherReaderWriter(AdvancedFileStream stream)
		{
			_stream = stream;
			_readBuffers = new StreamScatterGatherHelper(stream, true);
			_writeBuffers = new StreamScatterGatherHelper(stream, false);
			_shutdown = new CancellationTokenSource ();

			_cleanupTask = Task.Factory.StartNew(
				async () =>
				{
					while (!_shutdown.IsCancellationRequested)
					{
						await Task.Delay(200);
						await _readBuffers.OptimisedFlush();
						await _writeBuffers.OptimisedFlush();
					}
				},
				_shutdown.Token);
		}
		#endregion

		#region Public Methods
		public void Dispose()
		{
			DisposeManagedObjects();
		}

		/// <summary>
		/// Performs an asynchronous write of the specified buffer at the given
		/// physical page address.
		/// </summary>
		/// <param name="physicalPageId">The page id.</param>
		/// <param name="buffer">A <see cref="T:VirtualBuffer"/> object to be
		/// persisted.</param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public Task WriteBufferAsync(uint physicalPageId, VirtualBuffer buffer)
		{
			return _writeBuffers.ProcessBufferAsync(physicalPageId, buffer);
		}

		/// <summary>
		/// Performs an asynchronous read of the specified buffer at the given
		/// physical page address.
		/// </summary>
		/// <param name="pageId">The page id.</param>
		/// <param name="buffer">A <see cref="T:VirtualBuffer"/> object to be
		/// persisted.</param>
		/// <returns>
		/// </returns>
		[CLSCompliant(false)]
		public Task ReadBufferAsync(uint physicalPageId, VirtualBuffer buffer)
		{
			return _readBuffers.ProcessBufferAsync(physicalPageId, buffer);
		}

		/// <summary>
		/// Flushes all outstanding read and write requests to the underlying 
		/// stream.
		/// </summary>
		public Task Flush()
		{
			return Flush(true, true);
		}

		/// <summary>
		/// Flushes all outstanding read and write requests to the underlying
		/// stream.
		/// </summary>
		/// <param name="flushReads">if set to <c>true</c> then flush reads.</param>
		/// <param name="flushWrites">if set to <c>true</c> then flush writes.</param>
		public Task Flush(bool flushReads, bool flushWrites)
		{
			List<Task> tasks = new List<Task>();
			if (flushReads)
			{
				tasks.Add(_readBuffers.Flush());
			}
			if (flushWrites)
			{
				tasks.Add(_writeBuffers.Flush());
			}
			return TaskExtra.WhenAllOrEmpty(tasks.ToArray());
		}
		#endregion

		#region Private Methods
		private void DisposeManagedObjects()
		{
			if (_shutdown != null && !_shutdown.IsCancellationRequested)
			{
				// Signal shutdown and wait
				_shutdown.Cancel();
				_cleanupTask.Wait();

				// Cleanup cancellation object
				_shutdown.Dispose();
				_shutdown = null;

				// Force final flush
				Flush();
			}
		}
		#endregion
	}
}
