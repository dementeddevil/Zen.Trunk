namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;

    /// <summary>
	/// Implements the logical-virtual lookup service
	/// </summary>
	public sealed class LogicalVirtualManager : IDisposable, ILogicalVirtualManager
    {
		#region Private Types
		private class GetNewLogicalRequest : TaskRequest<LogicalPageId>
		{
		}

		private class AddLookupRequest : TaskRequest<LogicalPageId>
		{
			public AddLookupRequest(DevicePageId pageId, LogicalPageId logicalId)
			{
				PageId = pageId;
				LogicalId = logicalId;
			}

			public DevicePageId PageId
			{
				get;
			}

			public LogicalPageId LogicalId
			{
				get;
			}
		}

		private class GetLogicalRequest : TaskRequest<LogicalPageId>
		{
			public GetLogicalRequest(DevicePageId pageId)
			{
				PageId = pageId;
			}

			public DevicePageId PageId { get; }
		}

		private class GetVirtualRequest : TaskRequest<DevicePageId>
		{
			public GetVirtualRequest(LogicalPageId logicalId)
			{
				LogicalId = logicalId;
			}

			public LogicalPageId LogicalId { get; }
		}
		#endregion

		#region Private Fields
		private CancellationTokenSource _shutdownToken;
		private ConcurrentExclusiveSchedulerPair _taskInterleave;
		private ITargetBlock<GetNewLogicalRequest> _getNewLogicalPort;
		private ITargetBlock<AddLookupRequest> _addLookupPort;
		private ITargetBlock<GetLogicalRequest> _getLogicalPort;
		private ITargetBlock<GetVirtualRequest> _getVirtualPort;

		private Dictionary<LogicalPageId, DevicePageId> _logicalToVirtual =
			new Dictionary<LogicalPageId, DevicePageId>(1024);
		private Dictionary<DevicePageId, LogicalPageId> _virtualToLogical =
			new Dictionary<DevicePageId, LogicalPageId>(1024);
		private ulong _nextLogicalId = 1;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="LogicalVirtualManager"/> class.
		/// </summary>
		public LogicalVirtualManager()
		{
			_shutdownToken = new CancellationTokenSource();
			_taskInterleave = new ConcurrentExclusiveSchedulerPair();
			_getNewLogicalPort = new ActionBlock<GetNewLogicalRequest>(
				(request) =>
				{
					try
					{
						ulong nextLogicalId = _nextLogicalId;
						_nextLogicalId++;
						request.TrySetResult(new LogicalPageId(nextLogicalId));
					}
					catch (Exception e)
					{
						request.TrySetException(e);
					}
				},
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ExclusiveScheduler,
					MaxMessagesPerTask = 1,
					MaxDegreeOfParallelism = 1,
					CancellationToken = _shutdownToken.Token
				});
			_addLookupPort = new ActionBlock<AddLookupRequest>(
				(request) =>
				{
					try
					{
						var logicalId = request.LogicalId;
						if (_virtualToLogical.ContainsKey(request.PageId) ||
							_logicalToVirtual.ContainsKey(request.LogicalId))
						{
							throw new ArgumentException("Mapping already exists.");
						}

						_virtualToLogical.Add(request.PageId, request.LogicalId);
						_logicalToVirtual.Add(request.LogicalId, request.PageId);
						_nextLogicalId = Math.Max(_nextLogicalId, 1 + logicalId.Value);
						request.TrySetResult(logicalId);
					}
					catch (Exception e)
					{
						request.TrySetException(e);
					}
				},
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ExclusiveScheduler,
					MaxMessagesPerTask = 1,
					MaxDegreeOfParallelism = 1,
					CancellationToken = _shutdownToken.Token
				});
			_getLogicalPort = new ActionBlock<GetLogicalRequest>(
				(request) =>
				{
					try
					{
                        LogicalPageId logicalId;
						if (!_virtualToLogical.TryGetValue(request.PageId, out logicalId))
						{
							throw new ArgumentException("Page id not found.");
						}
						request.TrySetResult(logicalId);
					}
					catch (Exception e)
					{
						request.TrySetException(e);
					}
				},
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ConcurrentScheduler,
					MaxMessagesPerTask = 4,
					MaxDegreeOfParallelism = 4,
					CancellationToken = _shutdownToken.Token
				});
			_getVirtualPort = new ActionBlock<GetVirtualRequest>(
				(request) =>
				{
					try
					{
						DevicePageId pageId;
						if (!_logicalToVirtual.TryGetValue(request.LogicalId, out pageId))
						{
							throw new ArgumentException("Logical id not found.");
							//throw new DeviceInvalidPageException(
							//	0, request.LogicalId, false);
						}
						request.TrySetResult(pageId);
					}
					catch (Exception e)
					{
						request.TrySetException(e);
					}
				},
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ConcurrentScheduler,
					MaxMessagesPerTask = 4,
					MaxDegreeOfParallelism = 4,
					CancellationToken = _shutdownToken.Token
				});
		}
		#endregion

		#region Public Methods
		public void Dispose()
		{
			_shutdownToken.Cancel();
		}

		public Task<LogicalPageId> GetNewLogicalAsync()
		{
			var request = new GetNewLogicalRequest();
			if (!_getNewLogicalPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task<LogicalPageId> AddLookupAsync(DevicePageId pageId, LogicalPageId logicalId)
		{
			var request = new AddLookupRequest(pageId, logicalId);
			if (!_addLookupPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task<LogicalPageId> GetLogicalAsync(DevicePageId pageId)
		{
			var request = new GetLogicalRequest(pageId);
			if (!_getLogicalPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task<DevicePageId> GetVirtualAsync(LogicalPageId logicalId)
		{
			var request = new GetVirtualRequest(logicalId);
			if (!_getVirtualPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}
		#endregion
	}
}
