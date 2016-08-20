namespace Zen.Trunk.Storage.IO
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	public class ScatterGatherRequestArray
	{
		private readonly DateTime _createdDate;
		private List<ScatterGatherRequest> _callbackInfo = new List<ScatterGatherRequest>();
		private uint _startBlockNo;
		private uint _endBlockNo;

		[CLSCompliant(false)]
		public ScatterGatherRequestArray(ScatterGatherRequest request)
		{
			_createdDate = DateTime.UtcNow;
			_startBlockNo = _endBlockNo = request.PhysicalPageId;
			_callbackInfo.Add(request);
		}

		[CLSCompliant(false)]
		public bool AddRequest(ScatterGatherRequest request)
		{
			if (request.PhysicalPageId == _startBlockNo - 1)
			{
				_startBlockNo = request.PhysicalPageId;
				_callbackInfo.Insert(0, request);
				return true;
			}
			else if (request.PhysicalPageId == _endBlockNo + 1)
			{
				_endBlockNo = request.PhysicalPageId;
				_callbackInfo.Add(request);
				return true;
			}
			else
			{
				return false;
			}
		}

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

		public bool Consume(ScatterGatherRequestArray other)
		{
			if (_endBlockNo == (other._startBlockNo - 1))
			{
				_endBlockNo = other._endBlockNo;
				_callbackInfo.AddRange(other._callbackInfo);
				return true;
			}
			else if (_startBlockNo == (other._endBlockNo + 1))
			{
				_startBlockNo = other._startBlockNo;
				_callbackInfo.InsertRange(0, other._callbackInfo);
				return true;
			}
			return false;
		}

		public async Task FlushAsReadAsync(AdvancedFileStream stream)
		{
#if IOTRACE
			Trace.TraceInformation("SGW - Reading {0} memory blocks from disk",
				_callbackInfo.Count);
#endif
			// Prepare buffer array
			VirtualBuffer[] buffers = _callbackInfo
				.Select((item) => item.Buffer)
				.ToArray();
			int bufferSize = buffers[0].BufferSize;

			Task task = null;
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

		public async Task FlushAsWriteAsync(AdvancedFileStream stream)
		{
#if IOTRACE
			Trace.TraceInformation("SGW - Writing {0} memory blocks to disk",
				_callbackInfo.Count);
#endif
			// Prepare buffer array
			VirtualBuffer[] buffers = _callbackInfo
				.Select((item) => item.Buffer)
				.ToArray();
			int bufferSize = buffers[0].BufferSize;

			// Lock stream until we start async operation
			Task task = null;
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
				foreach (ScatterGatherRequest callback in _callbackInfo)
				{
					callback.TrySetException(e);
				}
				return;
			}

			// Notify each callback that we are now finished
			foreach (ScatterGatherRequest callback in _callbackInfo)
			{
				callback.Buffer.ClearDirty();
				callback.TrySetResult(null);
			}
		}
	}
}
