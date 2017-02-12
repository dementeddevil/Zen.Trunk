using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Zen.Trunk.Utils;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
	/// Implements the logical-virtual lookup service
	/// </summary>
	public sealed class LogicalVirtualManager : ILogicalVirtualManager
    {
		#region Private Types
		private class GetNewLogicalRequest : TaskRequest<LogicalPageId>
		{
		}

		private class AddLookupRequest : TaskRequest<LogicalPageId>
		{
			public AddLookupRequest(VirtualPageId pageId, LogicalPageId logicalId)
			{
				PageId = pageId;
				LogicalPageId = logicalId;
			}

			public VirtualPageId PageId
			{
				get;
			}

			public LogicalPageId LogicalPageId
			{
				get;
			}
		}

		private class GetLogicalRequest : TaskRequest<LogicalPageId>
		{
			public GetLogicalRequest(VirtualPageId pageId)
			{
				PageId = pageId;
			}

			public VirtualPageId PageId { get; }
		}

		private class GetVirtualRequest : TaskRequest<VirtualPageId>
		{
			public GetVirtualRequest(LogicalPageId logicalId)
			{
				LogicalPageId = logicalId;
			}

			public LogicalPageId LogicalPageId { get; }
		}
		#endregion

		#region Private Fields
		private readonly CancellationTokenSource _shutdownToken;
        private readonly ITargetBlock<GetNewLogicalRequest> _getNewLogicalPort;
		private readonly ITargetBlock<AddLookupRequest> _addLookupPort;
		private readonly ITargetBlock<GetLogicalRequest> _getLogicalPort;
		private readonly ITargetBlock<GetVirtualRequest> _getVirtualPort;

		private readonly Dictionary<LogicalPageId, VirtualPageId> _logicalToVirtual =
			new Dictionary<LogicalPageId, VirtualPageId>(1024);
		private readonly Dictionary<VirtualPageId, LogicalPageId> _virtualToLogical =
			new Dictionary<VirtualPageId, LogicalPageId>(1024);
		private ulong _nextLogicalPageId = 1;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="LogicalVirtualManager"/> class.
		/// </summary>
		public LogicalVirtualManager()
		{
		    _shutdownToken = new CancellationTokenSource();
			var taskInterleave = new ConcurrentExclusiveSchedulerPair();
			_getNewLogicalPort = new ActionBlock<GetNewLogicalRequest>(
				request =>
				{
					try
					{
						var nextLogicalPageId = _nextLogicalPageId;
						_nextLogicalPageId++;
						request.TrySetResult(new LogicalPageId(nextLogicalPageId));
					}
					catch (Exception e)
					{
						request.TrySetException(e);
					}
				},
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = taskInterleave.ExclusiveScheduler,
					MaxMessagesPerTask = 1,
					MaxDegreeOfParallelism = 1,
					CancellationToken = _shutdownToken.Token
				});
			_addLookupPort = new ActionBlock<AddLookupRequest>(
				request =>
				{
					try
					{
						var logicalId = request.LogicalPageId;
						if (_virtualToLogical.ContainsKey(request.PageId) ||
							_logicalToVirtual.ContainsKey(request.LogicalPageId))
						{
							throw new ArgumentException("Mapping already exists.");
						}

						_virtualToLogical.Add(request.PageId, request.LogicalPageId);
						_logicalToVirtual.Add(request.LogicalPageId, request.PageId);
						_nextLogicalPageId = Math.Max(_nextLogicalPageId, 1 + logicalId.Value);
						request.TrySetResult(logicalId);
					}
					catch (Exception e)
					{
						request.TrySetException(e);
					}
				},
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = taskInterleave.ExclusiveScheduler,
					MaxMessagesPerTask = 1,
					MaxDegreeOfParallelism = 1,
					CancellationToken = _shutdownToken.Token
				});
			_getLogicalPort = new ActionBlock<GetLogicalRequest>(
				request =>
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
					TaskScheduler = taskInterleave.ConcurrentScheduler,
					MaxMessagesPerTask = 4,
					MaxDegreeOfParallelism = 4,
					CancellationToken = _shutdownToken.Token
				});
			_getVirtualPort = new ActionBlock<GetVirtualRequest>(
				request =>
				{
					try
					{
						VirtualPageId pageId;
						if (!_logicalToVirtual.TryGetValue(request.LogicalPageId, out pageId))
						{
							throw new ArgumentException("Logical id not found.");
							//throw new DeviceInvalidPageException(
							//	0, request.LogicalPageId, false);
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
					TaskScheduler = taskInterleave.ConcurrentScheduler,
					MaxMessagesPerTask = 4,
					MaxDegreeOfParallelism = 4,
					CancellationToken = _shutdownToken.Token
				});
		}
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
		{
			_shutdownToken.Cancel();
		}

        /// <summary>
        /// Gets a new logical page identifier.
        /// </summary>
        /// <returns>
        /// A <see cref="LogicalPageId" /> object representing the new logical id.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<LogicalPageId> GetNewLogicalAsync()
		{
			var request = new GetNewLogicalRequest();
			if (!_getNewLogicalPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

        /// <summary>
        /// Adds a lookup between the specified virtual page id and logical page id.
        /// </summary>
        /// <param name="virtualPageId">A <see cref="VirtualPageId" /> representing the virtual page identifier.</param>
        /// <param name="logicalPageId">A <see cref="LogicalPageId" /> representing the logical page identifier.</param>
        /// <returns>
        /// A <see cref="LogicalPageId" /> object representing the logical id.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<LogicalPageId> AddLookupAsync(VirtualPageId virtualPageId, LogicalPageId logicalPageId)
		{
			var request = new AddLookupRequest(virtualPageId, logicalPageId);
			if (!_addLookupPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

        /// <summary>
        /// Gets the logical page identifier that corresponds to the specified virtual page id.
        /// </summary>
        /// <param name="virtualPageId"></param>
        /// <returns>
        /// A <see cref="LogicalPageId" /> object representing the logical id.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<LogicalPageId> GetLogicalAsync(VirtualPageId virtualPageId)
		{
			var request = new GetLogicalRequest(virtualPageId);
			if (!_getLogicalPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

        /// <summary>
        /// Gets the virtual page identifier that corresponds to the specified logical id.
        /// </summary>
        /// <param name="logicalPageId">A <see cref="LogicalPageId" /> representing the logical page identifier.</param>
        /// <returns>
        /// A <see cref="VirtualPageId" /> representing the virtual page identifier.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<VirtualPageId> GetVirtualAsync(LogicalPageId logicalPageId)
		{
			var request = new GetVirtualRequest(logicalPageId);
			if (!_getVirtualPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}
		#endregion
	}
}
