using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Zen.Trunk.Logging;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// <c>ScatterGatherRequestArray</c> encapsulates a number of scatter/gather
    /// requests into a batch of operations to be performed simultaneously.
    /// </summary>
    public class ScatterGatherRequestArray
    {
        private static readonly ILog Logger = LogProvider.For<ScatterGatherRequestArray>();

		private readonly DateTime _createdDate;
		private readonly List<ScatterGatherRequest> _callbackInfo = new List<ScatterGatherRequest>();
		private uint _startBlockNo;
		private uint _endBlockNo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterGatherRequestArray"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        [CLSCompliant(false)]
		public ScatterGatherRequestArray(ScatterGatherRequest request)
		{
			_createdDate = DateTime.UtcNow;
			_startBlockNo = _endBlockNo = request.PhysicalPageId;
			_callbackInfo.Add(request);
		}

        /// <summary>
        /// Adds the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [CLSCompliant(false)]
		public bool AddRequest(ScatterGatherRequest request)
		{
			if (request.PhysicalPageId == _startBlockNo - 1)
			{
				_startBlockNo = request.PhysicalPageId;
				_callbackInfo.Insert(0, request);
				return true;
			}

            if (request.PhysicalPageId == _endBlockNo + 1)
			{
				_endBlockNo = request.PhysicalPageId;
				_callbackInfo.Add(request);
				return true;
			}

            return false;
		}

        /// <summary>
        /// Determines whether a flush of the underlying stream is required given
        /// the specified maximum request age and/or maximum count of requests
        /// </summary>
        /// <param name="maximumAge">The maximum age.</param>
        /// <param name="maximumLength">The maximum length.</param>
        /// <returns>
        /// <c>true</c> if a flush is required; otherwise <c>false</c>.
        /// </returns>
        public bool RequiresFlush(TimeSpan maximumAge, int maximumLength)
		{
			if ((_endBlockNo - _startBlockNo + 1) > maximumLength)
			{
				return true;
			}

            if ((DateTime.UtcNow - _createdDate) > maximumAge)
			{
				return true;
			}

            return false;
		}

        /// <summary>
        /// Consumes the specified request array into this instance.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>
        /// <c>true</c> if the array was successfully consumed; otherwise <c>false</c>.
        /// </returns>
        public bool Consume(ScatterGatherRequestArray other)
		{
			if (_endBlockNo == (other._startBlockNo - 1))
			{
				_endBlockNo = other._endBlockNo;
				_callbackInfo.AddRange(other._callbackInfo);
				return true;
			}

            if (_startBlockNo == (other._endBlockNo + 1))
			{
				_startBlockNo = other._startBlockNo;
				_callbackInfo.InsertRange(0, other._callbackInfo);
				return true;
			}

            return false;
		}

        /// <summary>
        /// Flushes the request array under the assumption that each element
        /// relates to a pending read from the underlying stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public async Task FlushAsReadAsync(AdvancedFileStream stream)
		{
		    if (Logger.IsDebugEnabled())
		    {
		        Logger.Debug($"Reading {_callbackInfo.Count} memory blocks from disk");
		    }

			// Prepare buffer array
			var buffers = _callbackInfo
				.Select(item => item.Buffer)
				.ToArray();
			var bufferSize = buffers[0].BufferSize;

			Task task;
			lock (stream.SyncRoot)
			{
				// Adjust the file position and perform scatter/gather
				//	operation
				stream.Seek(_startBlockNo * bufferSize, SeekOrigin.Begin);
				task = stream.ReadScatterAsync(buffers);
			}

			try
			{
				await task.ConfigureAwait(false);
			}
			catch (Exception e)
			{
				// Pass error back to each caller
				foreach (var callback in _callbackInfo)
				{
					callback.TrySetException(e);
				}
				return;
			}

			// Notify each callback that we are now finished
			foreach (var callback in _callbackInfo)
			{
				callback.Buffer.ClearDirty();
				callback.TrySetResult(null);
			}
		}

        /// <summary>
        /// Flushes the request array under the assumption that each element
        /// relates to a pending write to the underlying stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public async Task FlushAsWriteAsync(AdvancedFileStream stream)
		{
            if (Logger.IsDebugEnabled())
            {
                Logger.Debug($"Writing {_callbackInfo.Count} memory blocks to disk");
            }

            // Prepare buffer array
            var buffers = _callbackInfo
				.Select(item => item.Buffer)
				.ToArray();
			var bufferSize = buffers[0].BufferSize;

			// Lock stream until we start async operation
			Task task;
			lock (stream.SyncRoot)
			{
				// Adjust the file position and perform scatter/gather
				//	operation
				stream.Seek(_startBlockNo * bufferSize, SeekOrigin.Begin);
				task = stream.WriteGatherAsync(buffers);
			}

			// Wait for completion
			try
			{
				await task.ConfigureAwait(false);
			}
			catch (Exception e)
			{
				// Pass error back to each caller
				foreach (var callback in _callbackInfo)
				{
					callback.TrySetException(e);
				}
				return;
			}

			// Notify each callback that we are now finished
			foreach (var callback in _callbackInfo)
			{
				callback.Buffer.ClearDirty();
				callback.TrySetResult(null);
			}
		}
	}
}
