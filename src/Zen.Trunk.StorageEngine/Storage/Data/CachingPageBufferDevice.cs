namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Concurrent.Partitioners;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;

	public sealed class CachingPageBufferDevice : IDisposable
	{
		#region Private Types
		private class PreparePageBufferRequest : TransactionContextTaskRequest<PageBuffer>
		{
			public PreparePageBufferRequest(DevicePageId pageId)
			{
				PageId = pageId;
			}

			public DevicePageId PageId
			{
				get;
				private set;
			}
		}

		private class FlushCachingDeviceRequest : TaskRequest<FlushCachingDeviceParameters, bool>
		{
			#region Public Constructors
			/// <summary>
			/// Initialises an instance of <see cref="T:FlushBuffers" />.
			/// </summary>
			public FlushCachingDeviceRequest(FlushCachingDeviceParameters flushParams)
				: base(flushParams)
			{
			}
			#endregion
		}

		private class BufferCacheInfo : IDisposable
		{
			#region Private Fields
			private PageBuffer _buffer;
			private DateTime _createdWhen = DateTime.UtcNow;
			private DateTime _lastAccess = DateTime.UtcNow;
			#endregion

			#region Internal Constructors
			internal BufferCacheInfo(PageBuffer buffer)
			{
				_buffer = buffer;
				_buffer.AddRef();
			}
			#endregion

			#region Internal Properties
			internal DateTime Created
			{
				get
				{
					return _createdWhen;
				}
			}

			internal DateTime LastAccess
			{
				get
				{
					return _lastAccess;
				}
			}

			internal TimeSpan Age
			{
				get
				{
					return DateTime.UtcNow - Created;
				}
			}

			internal DevicePageId PageId
			{
				get
				{
					return _buffer.PageId;
				}
			}

			internal PageBuffer PageBuffer
			{
				get
				{
					_lastAccess = DateTime.UtcNow;
					_buffer.AddRef();
					return _buffer;
				}
			}

			internal PageBuffer BufferInternal
			{
				get
				{
					return _buffer;
				}
			}

			internal bool IsReadPending
			{
				get
				{
					return _buffer.IsReadPending;
				}
			}

			internal bool IsWritePending
			{
				get
				{
					return _buffer.IsWritePending;
				}
			}

			internal bool CanFree
			{
				get
				{
					return _buffer.CanFree;
				}
			}
			#endregion

			#region Internal Methods
			internal PageBuffer RemoveBufferInternal()
			{
				PageBuffer returnBuffer = _buffer;
				_buffer = null;
				return returnBuffer;
			}
			#endregion

			#region IDisposable Members
			public void Dispose()
			{
				if (_buffer != null)
				{
					_buffer.Release();
					_buffer = null;
				}
			}
			#endregion
		}

		private class FlushPageBufferState
		{
			private Dictionary<ushort, byte> _devicesAccessed =
				new Dictionary<ushort, byte>();
			private List<Task> _saveTasks = new List<Task>();
			private List<Task> _loadTasks = new List<Task>();

			public FlushPageBufferState(FlushCachingDeviceParameters flushParams)
			{
				Params = flushParams;
			}

			public FlushCachingDeviceParameters Params
			{
				get;
				private set;
			}

			public IList<Task> SaveTasks
			{
				get
				{
					return _saveTasks;
				}
			}

			public IList<Task> LoadTasks
			{
				get
				{
					return _loadTasks;
				}
			}

			public void MarkDeviceAsAccessedForLoad(ushort deviceId)
			{
				MarkDeviceAsAccessed(deviceId, 1);
			}

			public void MarkDeviceAsAccessedForSave(ushort deviceId)
			{
				MarkDeviceAsAccessed(deviceId, 2);
			}

			public async Task FlushAccessedDevices(IMultipleBufferDevice bufferDevice)
			{
				List<Task> flushTasks = new List<Task>();
				foreach (var entry in _devicesAccessed)
				{
					bool flushReads = false;
					bool flushWrites = false;
					if ((entry.Value & 1) != 0)
					{
						flushReads = true;
					}
					if ((entry.Value & 2) != 0)
					{
						flushWrites = true;
					}
					await bufferDevice.FlushBuffersAsync(flushReads, flushWrites, entry.Key);
				}
			}

			private void MarkDeviceAsAccessed(ushort deviceId, byte value)
			{
				if (!_devicesAccessed.ContainsKey(deviceId))
				{
					_devicesAccessed.Add(deviceId, value);
				}
				else
				{
					_devicesAccessed[deviceId] |= value;
				}
			}
		}

		private enum CacheFlushState
		{
			Idle,
			FlushNormal,
			FlushCheckPoint
		}
		#endregion

		#region Private Fields
		private bool _isDisposed;
		private CancellationTokenSource _shutdownToken;
		private IMultipleBufferDevice _bufferDevice;

		// Buffer load/initialisation
		private ConcurrentDictionary<DevicePageId, TaskCompletionSource<PageBuffer>> _pendingLoadOrInit =
			new ConcurrentDictionary<DevicePageId, TaskCompletionSource<PageBuffer>>();

		// Buffer cache
		private SpinLockClass _bufferLookupLock = new SpinLockClass();
		private SortedList<DevicePageId, BufferCacheInfo> _bufferLookup =
			new SortedList<DevicePageId, BufferCacheInfo>();
		private int _cacheSize;
		private int _maxCacheSize = 2048;
		private int _cacheScavengeOffThreshold = 1500;
		private int _cacheScavengeOnThreshold = 1800;
		private TimeSpan _cacheFlushInterval = TimeSpan.FromMilliseconds(500);
		private CacheFlushState _flushState = CacheFlushState.Idle;
		private Task _cacheManagerTask;

		// Free pool
		private ObjectPool<PageBuffer> _freePagePool;
		private int _freePoolMin = 50;
		private int _freePoolMax = 100;
		private Task _freePoolFillerTask;

		// Ports
		private ITargetBlock<PreparePageBufferRequest> _initBufferPort;
		private ITargetBlock<PreparePageBufferRequest> _loadBufferPort;
		private ITargetBlock<FlushCachingDeviceRequest> _flushBuffersPort;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="CachingPageBufferDevice"/> class.
		/// </summary>
		/// <param name="bufferDevice">The buffer device that is to be cached.</param>
		public CachingPageBufferDevice(IMultipleBufferDevice bufferDevice)
		{
			_bufferDevice = bufferDevice;
			Initialize();
		}
		#endregion

		#region Private Properties
		/// <summary>
		/// Gets or sets a value indicating whether this instance is scavenging.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is scavenging; otherwise, <c>false</c>.
		/// </value>
		private bool IsScavenging
		{
			get;
			set;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			CloseAsync().Wait();
		}

		/// <summary>
		/// Closes this instance.
		/// </summary>
		/// <returns></returns>
		public async Task CloseAsync()
		{
			if (!_isDisposed)
			{
				// Shutdown all running threads
				_shutdownToken.Cancel();

				// Wait for the cache thread to terminate
				await _cacheManagerTask;

				// If we have any requests pending load or init then
				//	notify callers
				if (_pendingLoadOrInit.Count > 0)
				{
					Exception exception = new BufferDeviceShuttingDownException();
					foreach (var item in _pendingLoadOrInit.Values)
					{
						item.TrySetException(exception);
					}
					_pendingLoadOrInit.Clear();
				}

				// Invalidate buffer device object
				_bufferDevice = null;
				_isDisposed = true;
			}
		}

		public Task<ushort> AddDeviceAsync(string name, string pathName, ushort deviceId = 0, uint createPageCount = 0)
		{
			CheckDisposed();
			return _bufferDevice.AddDeviceAsync(name, pathName, deviceId, createPageCount);
		}

		public Task RemoveDeviceAsync(ushort deviceId)
		{
			CheckDisposed();
			return _bufferDevice.RemoveDeviceAsync(deviceId);
		}

		public Task<PageBuffer> InitPageAsync(DevicePageId pageId)
		{
			var request = new PreparePageBufferRequest(pageId);
			if (!_initBufferPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task<PageBuffer> LoadPageAsync(DevicePageId pageId)
		{
			var request = new PreparePageBufferRequest(pageId);
			if (!_loadBufferPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task FlushPagesAsync(FlushCachingDeviceParameters flushParams)
		{
			var request = new FlushCachingDeviceRequest(flushParams);
			if (!_flushBuffersPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}
		#endregion

		#region Protected Methods
		#endregion

		#region Private Methods
		private void Initialize()
		{
			// Create our cancellation token used to kill device
			_shutdownToken = new CancellationTokenSource();

			// Initialise the free-buffer pool handler
			_freePagePool = new ObjectPool<PageBuffer>(
				() =>
				{
					if (_shutdownToken.IsCancellationRequested)
					{
						return null;
					}
					return new PageBuffer(_bufferDevice);
				});
			_freePoolFillerTask = Task.Factory.StartNew(
				async () =>
				{
					// Keep the free page pool at minimum level
					while (!_shutdownToken.IsCancellationRequested)
					{
						while (_freePagePool.Count < _freePoolMin)
						{
							_freePagePool.PutObject(new PageBuffer(_bufferDevice));
						}
						await Task.Delay(TimeSpan.FromSeconds(1), _shutdownToken.Token);
					}

					// Drain free page pool when we are asked to shutdown
					while (_freePagePool.Count > 0)
					{
						PageBuffer buffer = _freePagePool.GetObject();
						if (buffer == null)
						{
							// The only way we will ever have nulls in the pool
							//	is if the factory method has detected the call
							//	to shutdown - so we can stop draining now...
							break;
						}
						buffer.Dispose();
					}
				},
				_shutdownToken.Token,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Default);

			// Initialisation and load handlers make use of common handler
			_initBufferPort = new TransactionContextActionBlock<PreparePageBufferRequest, PageBuffer>(
				(request) =>
				{
					return HandleLoadOrInit(request, false);
				},
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = TaskScheduler.Default,
					MaxDegreeOfParallelism = 10,
					MaxMessagesPerTask = 3,
					CancellationToken = _shutdownToken.Token
				});
			_loadBufferPort = new TransactionContextActionBlock<PreparePageBufferRequest, PageBuffer>(
				(request) =>
				{
					return HandleLoadOrInit(request, true);
				},
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = TaskScheduler.Default,
					MaxDegreeOfParallelism = 10,
					MaxMessagesPerTask = 3,
					CancellationToken = _shutdownToken.Token
				});

			// Explicit flush handler
			_flushBuffersPort = new TaskRequestActionBlock<FlushCachingDeviceRequest, bool>(
				(request) =>
				{
					return HandleFlushPageBuffers(request);
				});

			// Initialise caching support
			_cacheManagerTask = Task.Factory.StartNew(
				() => CacheManagerThread(),
				CancellationToken.None,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Default);
		}

		private void CheckDisposed()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}

		private Task<PageBuffer> HandleLoadOrInit(PreparePageBufferRequest request, bool isLoad)
		{
			// Sanity check
			if (_isDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			// If page is already being inited/loaded then reuse same completion task
			bool isAlreadyPending = false;
			var pbtcs = _pendingLoadOrInit.AddOrUpdate(
				request.PageId,
				new TaskCompletionSource<PageBuffer>(),
				(key, existingSource) =>
				{
					isAlreadyPending = true;
					return existingSource;
				});

			if (!isAlreadyPending)
			{
				// Check whether the buffer is already in our cache or create and
				//	and add a fresh buffer if not
				PageBuffer buffer = null;
				bool isNewBuffer = false;
				_bufferLookupLock.Execute(
					() =>
					{
						BufferCacheInfo cacheInfo;
						if (_bufferLookup.TryGetValue(request.PageId, out cacheInfo))
						{
							// Retrieve cached buffer
							// NOTE: Buffer is addref'ed here
							buffer = cacheInfo.PageBuffer;
						}
						else
						{
							if (_bufferLookup.Count >= _maxCacheSize)
							{
								// Buffer cache is at capacity
								// TODO: Force a cache scavenge pass and retry
								throw new OutOfMemoryException("Buffer cache is full.");
							}
							else
							{
								// Get new buffer from free pool and add to cache
								buffer = _freePagePool.GetObject();
								_bufferLookup.Add(request.PageId, new BufferCacheInfo(buffer));
								isNewBuffer = true;
							}
						}
					});

				if (!isNewBuffer)
				{
					// Buffer was in cache so complete the request
					LoadOrInitComplete(request.PageId, buffer);
				}
				else
				{
					// Delegate load or init to buffer object
					Task requestTask;
					if (isLoad)
					{
						requestTask = buffer.RequestLoadAsync(request.PageId, 0);
					}
					else
					{
						requestTask = buffer.InitAsync(request.PageId, 0);
					}

					// Attach the continuations that will clean up the task
					requestTask.ContinueWith((t) => LoadOrInitCancelled(request.PageId), TaskContinuationOptions.OnlyOnCanceled);
					requestTask.ContinueWith((t) => LoadOrInitComplete(request.PageId, buffer), TaskContinuationOptions.OnlyOnRanToCompletion);
					requestTask.ContinueWith((t) => LoadOrInitFailed(request.PageId, t.Exception), TaskContinuationOptions.OnlyOnFaulted);
				}
			}
			return pbtcs.Task;
		}

		private void LoadOrInitComplete(DevicePageId pageId, PageBuffer buffer)
		{
			TaskCompletionSource<PageBuffer> task;
			if (_pendingLoadOrInit.TryRemove(pageId, out task))
			{
				task.TrySetResult(buffer);
			}
		}

		private void LoadOrInitFailed(DevicePageId pageId, Exception error)
		{
			TaskCompletionSource<PageBuffer> task;
			if (_pendingLoadOrInit.TryRemove(pageId, out task))
			{
				task.TrySetException(error);
			}
		}

		private void LoadOrInitCancelled(DevicePageId pageId)
		{
			TaskCompletionSource<PageBuffer> task;
			if (_pendingLoadOrInit.TryRemove(pageId, out task))
			{
				task.TrySetCanceled();
			}
		}

		private async Task CacheManagerThread()
		{
			var flushParams = new FlushCachingDeviceParameters(true, true);
			DateTime? lastFlush = null;
			while (!_shutdownToken.IsCancellationRequested)
			{
				if (_flushState == CacheFlushState.Idle &&
					(lastFlush == null || (DateTime.UtcNow - lastFlush) > _cacheFlushInterval))
				{
					if (!IsScavenging && _bufferLookup.Count > _cacheScavengeOnThreshold)
					{
						IsScavenging = true;
					}
					try
					{
						await FlushPagesAsync(flushParams);
						lastFlush = DateTime.UtcNow;
					}
					finally
					{
						if (IsScavenging && _bufferLookup.Count < _cacheScavengeOffThreshold)
						{
							IsScavenging = false;
						}
					}
				}
				else
				{
					Thread.Sleep(_cacheFlushInterval);
				}
			}

			// Wait for flush to finish if it is still running
			// NOTE: We will wait for a maximum of 30 seconds
			DateTime start = DateTime.UtcNow;
			while (_flushState != CacheFlushState.Idle &&
				(DateTime.UtcNow - start) < TimeSpan.FromSeconds(30))
			{
				Thread.Sleep(1);
			}

			// Discard buffer cache
			foreach (BufferCacheInfo info in _bufferLookup.Values)
			{
				// Dispose of every entry in the cache
				info.Dispose();
			}
			_bufferLookup.Clear();
		}

		private bool HandleFlushPageBuffers(FlushCachingDeviceRequest request)
		{
			// Determine new flush state
			CacheFlushState newState = CacheFlushState.FlushNormal;
			if (request.Message.IsForCheckPoint)
			{
				newState = CacheFlushState.FlushCheckPoint;
			}

			// Switch cache flush state now
			if (_flushState != CacheFlushState.FlushCheckPoint)
			{
				_flushState = newState;
			}
			try
			{
				// Make copy of cache keys
				DevicePageId[] keys = null;
				_bufferLookupLock.Execute(
					() =>
					{
						keys = new DevicePageId[_bufferLookup.Count];
						_bufferLookup.Keys.CopyTo(keys, 0);
					});

				// Create cache partitioner 
				OrderablePartitioner<DevicePageId> cacheKeyPartitioner =
					ChunkPartitioner.Create<DevicePageId>(keys, 10, 100);
				try
				{
					Parallel.ForEach<DevicePageId, FlushPageBufferState>(
						cacheKeyPartitioner,
						new ParallelOptions
						{
							MaxDegreeOfParallelism = 4
						},
						() => new FlushPageBufferState(request.Message),
						ProcessCacheBufferEntry,
						(blockState) =>
						{
							// Issue flush to each device accessed
							bool flushReads = (blockState.LoadTasks.Count > 0);
							bool flushWrites = (blockState.SaveTasks.Count > 0);
							if (flushReads || flushWrites)
							{
								blockState.FlushAccessedDevices(_bufferDevice)
									.Wait();
							}
						});
				}
				finally
				{
					// Discard the partitioner if it implements IDisposable
					IDisposable dispose = cacheKeyPartitioner as IDisposable;
					if (dispose != null)
					{
						dispose.Dispose();
					}
				}
			}
			finally
			{
				// Clear flush state if it is the same as when we started
				if (_flushState == newState)
				{
					_flushState = CacheFlushState.Idle;
				}
			}

			// Signal request has completed
			return true;
		}

		private FlushPageBufferState ProcessCacheBufferEntry(
			DevicePageId pageId, ParallelLoopState loopState, long index, FlushPageBufferState blockState)
		{
			// Retrieve entry from cache - skip if no longer present
			BufferCacheInfo cacheInfo;
			if (_bufferLookup.TryGetValue(pageId, out cacheInfo))
			{
				// Process pages we can load
				if (blockState.Params.FlushReads && cacheInfo.IsReadPending)
				{
					// Create a task that tackles the load operation and notifies
					//	all waiting callers when complete - whatever the outcome
					Task loadTask = cacheInfo.BufferInternal.LoadAsync();
					loadTask.ContinueWith(
						(task) =>
						{
							if (task.IsCanceled)
							{
								LoadOrInitCancelled(cacheInfo.PageId);
							}
							else if (task.Exception != null)
							{
								LoadOrInitFailed(cacheInfo.PageId, task.Exception);
							}
							else
							{
								LoadOrInitComplete(cacheInfo.PageId, cacheInfo.PageBuffer);
							}
						},
						TaskContinuationOptions.AttachedToParent |
						TaskContinuationOptions.ExecuteSynchronously);
					blockState.LoadTasks.Add(loadTask);

					blockState.MarkDeviceAsAccessedForLoad(pageId.DeviceId);
				}

				// Process pages we can save
				else if (blockState.Params.FlushWrites && cacheInfo.IsWritePending)
				{
					// This may throw if another thread changes the
					//	buffer state before the ioSave begins the
					//	write operation - ignore these errors
					blockState.SaveTasks.Add(cacheInfo.BufferInternal.SaveAsync());

					blockState.MarkDeviceAsAccessedForSave(pageId.DeviceId);
				}
				else if (cacheInfo.CanFree && IsScavenging)
				{
					// Free the buffer (may throw)
					cacheInfo.BufferInternal.SetFree();

					// Remove cache item and decrement count
					_bufferLookup.Remove(pageId);
					Interlocked.Decrement(ref _cacheSize);

					// Add to free pages if we can
					if (_freePagePool.Count < _freePoolMax)
					{
						// Add buffer to free pool and disconnect from cache
						_freePagePool.PutObject(cacheInfo.BufferInternal);
						cacheInfo.RemoveBufferInternal();
					}

					// Discard the cache info
					cacheInfo.Dispose();
				}
			}
			return blockState;
		}

		private async Task LoadCacheInfo(BufferCacheInfo cacheInfo)
		{
			try
			{
				await cacheInfo.BufferInternal.LoadAsync();
			}
			catch (OperationCanceledException)
			{
				LoadOrInitCancelled(cacheInfo.PageId);
				return;
			}
			catch (Exception exception)
			{
				LoadOrInitFailed(cacheInfo.PageId, exception);
				return;
			}
			LoadOrInitComplete(cacheInfo.PageId, cacheInfo.PageBuffer);
		}
		#endregion
	}
}
