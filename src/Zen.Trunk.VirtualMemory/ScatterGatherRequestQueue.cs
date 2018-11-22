using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zen.Trunk.Utils;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// <c>ScatterGatherRequestQueue</c> optimises buffer persistence by
    /// grouping reads and writes on consecutive buffers together and so
    /// that the I/O can be performed in one overlapped operation.
    /// </summary>
    /// <remarks>
    /// A single request queue instance is assumed to hold either reads or
    /// write requests and never both kinds.
    /// </remarks>
    public sealed class ScatterGatherRequestQueue : IDisposable
	{
		#region Private Fields
		private readonly StreamScatterGatherRequestQueue _readQueue;
		private readonly StreamScatterGatherRequestQueue _writeQueue;
		private readonly CancellationTokenSource _shutdown;
		private readonly Task _cleanupTask;
		#endregion

		#region Public Constructors

	    /// <summary>
	    /// Initializes a new instance of the 
	    /// <see cref="T:ScatterGatherReaderWriter"/> class.
	    /// </summary>
	    /// <param name="stream">Underlying stream object.</param>
	    /// <param name="settings">Settings to control request queue.</param>
	    public ScatterGatherRequestQueue(
		    AdvancedStream stream, ScatterGatherRequestQueueSettings settings)
		{
			_readQueue = new StreamScatterGatherRequestQueue(
			    stream, settings.ReadSettings, (s, a) => a.FlushAsReadAsync(s));
			_writeQueue = new StreamScatterGatherRequestQueue(
			    stream, settings.WriteSettings, (s, a) => a.FlushAsWriteAsync(s));

			_shutdown = new CancellationTokenSource ();

		    if (settings.AutomaticFlushPeriod > TimeSpan.Zero)
		    {
			    _cleanupTask = Task.Factory.StartNew(
				    async () =>
				    {
					    while (!_shutdown.IsCancellationRequested)
					    {
						    await Task.Delay(settings.AutomaticFlushPeriod).ConfigureAwait(false);
						    await _readQueue.OptimisedFlushAsync().ConfigureAwait(false);
						    await _writeQueue.OptimisedFlushAsync().ConfigureAwait(false);
					    }
				    },
				    _shutdown.Token);
		    }
		}
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
		{
			DisposeManagedObjects();
		}

        /// <summary>
        /// Performs an asynchronous write of the specified buffer at the given
        /// physical page index.
        /// </summary>
        /// <param name="physicalPageId">The physical page id.</param>
        /// <param name="buffer">A <see cref="T:IVirtualBuffer"/> object to be persisted.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        [CLSCompliant(false)]
		public Task WriteBufferAsync(uint physicalPageId, IVirtualBuffer buffer)
		{
			return _writeQueue.ProcessBufferAsync(physicalPageId, buffer);
		}

        /// <summary>
        /// Performs an asynchronous read of the specified buffer at the given
        /// physical page address.
        /// </summary>
        /// <param name="physicalPageId">The physical page id.</param>
        /// <param name="buffer">A <see cref="T:IVirtualBuffer"/> object to be persisted.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        [CLSCompliant(false)]
		public Task ReadBufferAsync(uint physicalPageId, IVirtualBuffer buffer)
		{
			return _readQueue.ProcessBufferAsync(physicalPageId, buffer);
		}

        /// <summary>
        /// Flushes all outstanding read and write requests to the underlying
        /// stream.
        /// </summary>
        /// <param name="flushReads">if set to <c>true</c> then flush read queue.</param>
        /// <param name="flushWrites">if set to <c>true</c> then flush write queue.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task Flush(bool flushReads = true, bool flushWrites = true)
		{
			var tasks = new List<Task>();

		    if (flushReads)
			{
				tasks.Add(_readQueue.FlushAsync());
			}

		    if (flushWrites)
			{
				tasks.Add(_writeQueue.FlushAsync());
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
				
				// Force final synchronous flush
				Flush().GetAwaiter().GetResult();
			}
		}
		#endregion
	}
}
