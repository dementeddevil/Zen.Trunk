using System;
using System.Collections.Concurrent;
using System.Collections.Concurrent.Partitioners;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Zen.Trunk.Logging;

namespace Zen.Trunk.Storage.Data
{
    public sealed class CachingPageBufferDevice : ICachingPageBufferDevice
    {
        #region Private Types
        private class PreparePageBufferRequest : TransactionContextTaskRequest<PageBuffer>
        {
            public PreparePageBufferRequest(VirtualPageId pageId)
            {
                PageId = pageId;
            }

            public VirtualPageId PageId { get; }
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
            private readonly DateTime _createdWhen = DateTime.UtcNow;
            private DateTime _lastAccess = DateTime.UtcNow;
            private PageBuffer _buffer;
            #endregion

            #region Internal Constructors
            internal BufferCacheInfo(PageBuffer buffer)
            {
                _buffer = buffer;
                _buffer.AddRef();
            }
            #endregion

            #region Internal Properties
            internal DateTime Created => _createdWhen;

            internal DateTime LastAccess => _lastAccess;

            internal TimeSpan Age => DateTime.UtcNow - Created;

            internal VirtualPageId PageId => _buffer.PageId;

            internal PageBuffer PageBuffer
            {
                get
                {
                    _lastAccess = DateTime.UtcNow;
                    _buffer.AddRef();
                    return _buffer;
                }
            }

            internal PageBuffer BufferInternal => _buffer;

            internal bool IsReadPending => _buffer.IsReadPending;

            internal bool IsWritePending => _buffer.IsWritePending;

            internal bool CanFree => _buffer.CanFree;

            #endregion

            #region Internal Methods
            internal PageBuffer RemoveBufferInternal()
            {
                var returnBuffer = _buffer;
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
            private readonly Dictionary<DeviceId, byte> _devicesAccessed =
                new Dictionary<DeviceId, byte>();
            private readonly List<Task> _saveTasks = new List<Task>();
            private readonly List<Task> _loadTasks = new List<Task>();

            public FlushPageBufferState(FlushCachingDeviceParameters flushParams)
            {
                Params = flushParams;
            }

            public FlushCachingDeviceParameters Params { get; }

            public IList<Task> SaveTasks => _saveTasks;

            public IList<Task> LoadTasks => _loadTasks;

            public void MarkDeviceAsAccessedForLoad(DeviceId deviceId)
            {
                MarkDeviceAsAccessed(deviceId, 1);
            }

            public void MarkDeviceAsAccessedForSave(DeviceId deviceId)
            {
                MarkDeviceAsAccessed(deviceId, 2);
            }

            public async Task FlushAccessedDevices(IMultipleBufferDevice bufferDevice)
            {
                foreach (var entry in _devicesAccessed)
                {
                    var flushReads = false;
                    var flushWrites = false;
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

            private void MarkDeviceAsAccessed(DeviceId deviceId, byte value)
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
        private static readonly ILog Logger = LogProvider.For<CachingPageBufferDevice>();

        private readonly CachingPageBufferDeviceSettings _cacheSettings;

        private bool _isDisposed;
        private readonly CancellationTokenSource _shutdownToken = new CancellationTokenSource();
        private IMultipleBufferDevice _bufferDevice;

        // Buffer load/initialisation
        private readonly ConcurrentDictionary<VirtualPageId, TaskCompletionSource<PageBuffer>> _pendingLoadOrInit =
            new ConcurrentDictionary<VirtualPageId, TaskCompletionSource<PageBuffer>>();

        // Buffer cache
        private readonly SpinLockClass _bufferLookupLock = new SpinLockClass();
        private readonly SortedList<VirtualPageId, BufferCacheInfo> _bufferLookup =
            new SortedList<VirtualPageId, BufferCacheInfo>();
        private int _cacheSize;
        private CacheFlushState _flushState = CacheFlushState.Idle;
        private readonly Task _pageBufferFlushTask;

        // Free pool
        private readonly ObjectPool<PageBuffer> _freePagePool;
        private readonly Task _freePoolFillerTask;

        // Ports
        private readonly ITargetBlock<PreparePageBufferRequest> _initBufferPort;
        private readonly ITargetBlock<PreparePageBufferRequest> _loadBufferPort;
        private readonly ITargetBlock<FlushCachingDeviceRequest> _flushBuffersPort;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CachingPageBufferDevice" /> class.
        /// </summary>
        /// <param name="bufferDevice">The buffer device that is to be cached.</param>
        /// <param name="cacheSettings">The cache device settings.</param>
        public CachingPageBufferDevice(
            IMultipleBufferDevice bufferDevice,
            CachingPageBufferDeviceSettings cacheSettings)
        {
            _bufferDevice = bufferDevice;
            _cacheSettings = cacheSettings ?? new CachingPageBufferDeviceSettings();

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
                FreePoolFillerThread,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            // Initialisation and load handlers make use of common handler
            _initBufferPort = new TransactionContextActionBlock<PreparePageBufferRequest, PageBuffer>(
                request => HandleLoadOrInit(request, false),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = TaskScheduler.Default,
                    MaxDegreeOfParallelism = _cacheSettings.InitBufferThreadCount,
                    MaxMessagesPerTask = 3,
                    CancellationToken = _shutdownToken.Token
                });
            _loadBufferPort = new TransactionContextActionBlock<PreparePageBufferRequest, PageBuffer>(
                request => HandleLoadOrInit(request, true),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = TaskScheduler.Default,
                    MaxDegreeOfParallelism = _cacheSettings.LoadBufferThreadCount,
                    MaxMessagesPerTask = 3,
                    CancellationToken = _shutdownToken.Token
                });

            // Explicit flush handler
            _flushBuffersPort = new TaskRequestActionBlock<FlushCachingDeviceRequest, bool>(
                request => HandleFlushPageBuffers(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = TaskScheduler.Default,
                    CancellationToken = CancellationToken.None
                });

            // Initialise caching support
            _pageBufferFlushTask = Task.Factory.StartNew(
                PageBufferFlushThread,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
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

                // Wait for long-running threads to terminate
                await Task
                    .WhenAll(
                        _pageBufferFlushTask,
                        _freePoolFillerTask)
                    .ConfigureAwait(false);

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

        public Task<DeviceId> AddDeviceAsync(string name, string pathName, DeviceId deviceId, uint createPageCount)
        {
            CheckDisposed();
            return _bufferDevice.AddDeviceAsync(name, pathName, deviceId, createPageCount);
        }

        public Task RemoveDeviceAsync(DeviceId deviceId)
        {
            CheckDisposed();
            return _bufferDevice.RemoveDeviceAsync(deviceId);
        }

        public Task<PageBuffer> InitPageAsync(VirtualPageId pageId)
        {
            var request = new PreparePageBufferRequest(pageId);
            if (!_initBufferPort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        public Task<PageBuffer> LoadPageAsync(VirtualPageId pageId)
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

        #region Private Methods
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
            var isAlreadyPending = false;
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
                var isNewBuffer = false;
                _bufferLookupLock.Execute(
                    () =>
                    {
                        // Attempt to retrieve buffer from cache
                        BufferCacheInfo cacheInfo;
                        if (_bufferLookup.TryGetValue(request.PageId, out cacheInfo))
                        {
                            // Retrieve cached buffer
                            // NOTE: Buffer is addref'ed here
                            buffer = cacheInfo.PageBuffer;
                        }
                        else
                        {
                            // Throw if we are full...
                            if (_bufferLookup.Count >= _cacheSettings.MaximumCacheSize)
                            {
                                // Buffer cache is at capacity
                                throw new OutOfMemoryException("Buffer cache is full.");
                            }

                            // Get new buffer from free pool and add to cache
                            buffer = _freePagePool.GetObject();
                            _bufferLookup.Add(request.PageId, new BufferCacheInfo(buffer));
                            isNewBuffer = true;
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
                    RequestLoadOrInitPageBuffer(buffer, isLoad, request.PageId);
                }
            }
            return pbtcs.Task;
        }

        private void RemovePendingLoadOrInitAndComplete(
            VirtualPageId pageId, Action<TaskCompletionSource<PageBuffer>> completionAction)
        {
            TaskCompletionSource<PageBuffer> task;
            if (_pendingLoadOrInit.TryRemove(pageId, out task))
            {
                completionAction(task);
            }
        }

        private void LoadOrInitComplete(VirtualPageId pageId, PageBuffer buffer)
        {
            RemovePendingLoadOrInitAndComplete(
                pageId, ct => ct.TrySetResult(buffer));
        }

        private void LoadOrInitFailed(VirtualPageId pageId, Exception error)
        {
            RemovePendingLoadOrInitAndComplete(
                pageId, ct => ct.TrySetException(error));
        }

        private void LoadOrInitCancelled(VirtualPageId pageId)
        {
            RemovePendingLoadOrInitAndComplete(
                pageId, ct => ct.TrySetCanceled());
        }

        private async Task PageBufferFlushThread()
        {
            var flushParams = new FlushCachingDeviceParameters(true, true, DeviceId.Zero);
            while (!_shutdownToken.IsCancellationRequested)
            {
                if (_flushState == CacheFlushState.Idle)
                {
                    // Determine whether we need to start scavenging
                    if (!IsScavenging && _bufferLookup.Count > _cacheSettings.CacheScavengeOnThreshold)
                    {
                        IsScavenging = true;
                    }

                    try
                    {
                        // Flush both reads and writes then update last flush time
                        await FlushPagesAsync(flushParams).ConfigureAwait(false);
                    }
                    finally
                    {
                        // Determine whether we have recovered enough pages to stop scavenging
                        if (IsScavenging && _bufferLookup.Count < _cacheSettings.CacheScavengeOffThreshold)
                        {
                            IsScavenging = false;
                        }
                    }
                }

                if (!_shutdownToken.IsCancellationRequested)
                {
                    await Task
                        .Delay(_cacheSettings.CacheFlushInterval, _shutdownToken.Token)
                        .ConfigureAwait(false);
                }
            }

            // Wait for flush to finish if it is still running
            // NOTE: We will wait for a maximum of 30 seconds
            var start = DateTime.UtcNow;
            while (_flushState != CacheFlushState.Idle &&
                (DateTime.UtcNow - start) < TimeSpan.FromSeconds(30))
            {
                Thread.Sleep(1);
            }

            // Discard buffer cache
            foreach (var info in _bufferLookup.Values)
            {
                // Dispose of every entry in the cache
                info.Dispose();
            }
            _bufferLookup.Clear();
        }

        private async Task FreePoolFillerThread()
        {
            // Keep the free page pool at minimum level
            while (!_shutdownToken.IsCancellationRequested)
            {
                // Fill free pool to high-water mark
                while (!_shutdownToken.IsCancellationRequested &&
                    _freePagePool.Count < _cacheSettings.MaximumFreePoolSize)
                {
                    _freePagePool.PutObject(new PageBuffer(_bufferDevice));
                }

                // Monitor until we see low-water mark
                while (!_shutdownToken.IsCancellationRequested &&
                    _freePagePool.Count > _cacheSettings.MinimumFreePoolSize)
                {
                    await Task
                        .Delay(_cacheSettings.FreePoolMonitorInterval, _shutdownToken.Token)
                        .ConfigureAwait(false);
                }
            }

            // Drain free page pool when we are asked to shutdown
            while (_freePagePool.Count > 0)
            {
                var buffer = _freePagePool.GetObject();
                if (buffer == null)
                {
                    // The only way we will ever have nulls in the pool is if
                    //  the free-pool factory method has detected the call to
                    //  shutdown - so we can stop draining now...
                    break;
                }
                buffer.Dispose();
            }
        }

        private bool HandleFlushPageBuffers(FlushCachingDeviceRequest request)
        {
            // Determine new flush state
            var newState = CacheFlushState.FlushNormal;
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
                VirtualPageId[] keys = null;
                _bufferLookupLock.Execute(
                    () =>
                    {
                        keys = new VirtualPageId[_bufferLookup.Count];
                        _bufferLookup.Keys.CopyTo(keys, 0);
                    });

                // Create cache partitioner
                Parallel.ForEach(
                    ChunkPartitioner.Create(
                        keys,
                        _cacheSettings.MinimumBlockFlushSize,
                        _cacheSettings.MaximumBlockFlushSize),
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _cacheSettings.BlockFlushThreadCount
                    },
                    () => new FlushPageBufferState(request.Message),
                    ProcessCacheBufferEntry,
                    blockState =>
                    {
                        // Issue flush to each device accessed
                        var flushReads = blockState.LoadTasks.Count > 0;
                        var flushWrites = blockState.SaveTasks.Count > 0;
                        if (flushReads || flushWrites)
                        {
                            blockState.FlushAccessedDevices(_bufferDevice).Wait();
                        }
                    });
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
            VirtualPageId pageId,
            ParallelLoopState loopState,
            long index,
            FlushPageBufferState blockState)
        {
            // Retrieve entry from cache - skip if no longer present
            BufferCacheInfo cacheInfo;
            if (_bufferLookup.TryGetValue(pageId, out cacheInfo))
            {
                // Process pages we can load
                if (blockState.Params.FlushReads && cacheInfo.IsReadPending)
                {
                    // Create async task to load the cache info, add to list
                    //  and signal that we have a pending load
                    blockState.LoadTasks.Add(LoadCacheInfo(cacheInfo));
                    blockState.MarkDeviceAsAccessedForLoad(pageId.DeviceId);
                }

                // Process pages we can save
                else if (blockState.Params.FlushWrites && cacheInfo.IsWritePending)
                {
                    // This may throw if another thread changes the
                    //	buffer state before it begins the write operation
                    blockState.SaveTasks.Add(cacheInfo.BufferInternal.SaveAsync());
                    blockState.MarkDeviceAsAccessedForSave(pageId.DeviceId);
                }
                else if (cacheInfo.CanFree && IsScavenging)
                {
                    // Free the buffer (may throw)
                    cacheInfo.BufferInternal.SetFreeAsync();

                    // Remove cache item and decrement count
                    _bufferLookup.Remove(pageId);
                    Interlocked.Decrement(ref _cacheSize);

                    // Add to free pages if we can
                    if (_freePagePool.Count < _cacheSettings.MaximumFreePoolSize)
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

        private async void RequestLoadOrInitPageBuffer(PageBuffer buffer, bool isLoad, VirtualPageId pageId)
        {
            try
            {
                if (isLoad)
                {
                    await buffer.RequestLoadAsync(pageId, LogicalPageId.Zero).ConfigureAwait(false);
                }
                else
                {
                    await buffer.InitAsync(pageId, LogicalPageId.Zero).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                LoadOrInitCancelled(pageId);
                return;
            }
            catch (Exception exception)
            {
                LoadOrInitFailed(pageId, exception);
                return;
            }
            LoadOrInitComplete(pageId, buffer);
        }

        private async Task LoadCacheInfo(BufferCacheInfo cacheInfo)
        {
            try
            {
                await cacheInfo.BufferInternal.LoadAsync().ConfigureAwait(false);
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
