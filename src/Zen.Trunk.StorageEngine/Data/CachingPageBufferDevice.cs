﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Serilog;
using Serilog.Context;
using Zen.Trunk.CoordinationDataStructures;
using Zen.Trunk.Partitioners;
using Zen.Trunk.Storage.Services;
using Zen.Trunk.Utils;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// <c>CachingPageBufferDevice</c> handles the lifetime persistence of
    /// <see cref="PageBuffer"/> objects.
    /// </summary>
    public sealed class CachingPageBufferDevice : ICachingPageBufferDevice
    {
        #region Private Types
        private class PreparePageBufferRequest : TransactionContextTaskRequest<IPageBuffer>
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
            #region Internal Constructors
            internal BufferCacheInfo(IPageBuffer buffer)
            {
                BufferInternal = buffer;
                BufferInternal.AddRef();
            }
            #endregion

            #region Internal Properties
            // ReSharper disable once MemberCanBePrivate.Local
            internal DateTime Created { get; } = DateTime.UtcNow;

            // ReSharper disable once MemberCanBePrivate.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            internal DateTime LastAccess { get; private set; } = DateTime.UtcNow;

            // ReSharper disable once UnusedMember.Local
            internal TimeSpan Age => DateTime.UtcNow - Created;

            internal VirtualPageId PageId => BufferInternal.PageId;

            internal IPageBuffer PageBuffer
            {
                get
                {
                    LastAccess = DateTime.UtcNow;
                    BufferInternal.AddRef();
                    return BufferInternal;
                }
            }

            internal IPageBuffer BufferInternal { get; private set; }

            internal bool IsReadPending => BufferInternal.IsReadPending;

            internal bool IsWritePending => BufferInternal.IsWritePending;

            internal bool CanFree => BufferInternal.CanFree;
            #endregion

            #region Internal Methods
            // ReSharper disable once UnusedMethodReturnValue.Local
            internal IPageBuffer RemoveBufferInternal()
            {
                var returnBuffer = BufferInternal;
                BufferInternal = null;
                return returnBuffer;
            }
            #endregion

            #region IDisposable Members
            public void Dispose()
            {
                if (BufferInternal != null)
                {
                    BufferInternal.Release();
                    BufferInternal = null;
                }
            }
            #endregion
        }

        private class FlushPageBufferState
        {
            private readonly Dictionary<DeviceId, byte> _devicesAccessed = new Dictionary<DeviceId, byte>();
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

            public async Task FlushAccessedDevicesAsync(IMultipleBufferDevice bufferDevice)
            {
                using (LogContext.PushProperty("Method", nameof(FlushAccessedDevicesAsync)))
                {
                    if (LoadTasks.Count == 0 && SaveTasks.Count == 0)
                    {
                        Logger.Debug("FlushAccessedDevicesAsync => nothing to do.");
                        return;
                    }

                    var pendingTasks = _devicesAccessed
                        .Select(
                            entry =>
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
                                return bufferDevice.FlushBuffersAsync(flushReads, flushWrites, entry.Key);
                            })
                        .ToArray();
                    await TaskExtra.WhenAllOrEmpty(pendingTasks).ConfigureAwait(false);
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
        // ReSharper disable once UnusedMember.Local
        private static readonly ILogger Logger = Serilog.Log.ForContext<CachingPageBufferDevice>();
        // ReSharper disable once UnusedMember.Local
        private readonly CachingPageBufferDeviceSettings _cacheSettings;

        private bool _isDisposed;
        private readonly CancellationTokenSource _shutdownToken = new CancellationTokenSource();
        private IMultipleBufferDevice _bufferDevice;
        private readonly IStorageEngineEventService _storageEngineEventService;

        // Buffer load/initialisation
        private readonly ConcurrentDictionary<VirtualPageId, TaskCompletionSource<IPageBuffer>> _pendingLoadOrInit =
            new ConcurrentDictionary<VirtualPageId, TaskCompletionSource<IPageBuffer>>();

        // Buffer cache
        private readonly SpinLockClass _bufferLookupLock = new SpinLockClass();
        private readonly SortedList<VirtualPageId, BufferCacheInfo> _bufferLookup =
            new SortedList<VirtualPageId, BufferCacheInfo>();
        private int _cacheSize;
        private CacheFlushState _flushState = CacheFlushState.Idle;
        private readonly Task _pageBufferFlushTask;

        // Free pool
        private readonly ObjectPool<IPageBuffer> _freePagePool;
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
        /// <param name="storageEngineEventService">Event service.</param>
        /// <param name="cacheSettings">The cache device settings.</param>
        public CachingPageBufferDevice(
            IMultipleBufferDevice bufferDevice,
            IStorageEngineEventService storageEngineEventService,
            CachingPageBufferDeviceSettings cacheSettings)
        {
            _bufferDevice = bufferDevice ?? throw new ArgumentNullException(nameof(bufferDevice));
            _storageEngineEventService = storageEngineEventService ?? throw new ArgumentNullException(nameof(storageEngineEventService));
            _cacheSettings = cacheSettings ?? new CachingPageBufferDeviceSettings();

            // Initialise the free-buffer pool handler
            _freePagePool = new ObjectPool<IPageBuffer>(
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
            _initBufferPort = new TransactionContextActionBlock<PreparePageBufferRequest, IPageBuffer>(
                request => HandleInit(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = TaskScheduler.Default,
                    MaxDegreeOfParallelism = _cacheSettings.InitBufferThreadCount,
                    MaxMessagesPerTask = 3,
                    CancellationToken = _shutdownToken.Token
                });
            _loadBufferPort = new TransactionContextActionBlock<PreparePageBufferRequest, IPageBuffer>(
                request => HandleLoad(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = TaskScheduler.Default,
                    MaxDegreeOfParallelism = _cacheSettings.LoadBufferThreadCount,
                    MaxMessagesPerTask = 3,
                    CancellationToken = _shutdownToken.Token
                });

            // Explicit flush handler
            _flushBuffersPort = new TaskRequestActionBlock<FlushCachingDeviceRequest, bool>(
                request => HandleFlushPageBuffersAsync(request),
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
            CloseAsync().GetAwaiter().GetResult();
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
                    foreach (var item in _pendingLoadOrInit.Values)
                    {
                        item.TrySetException(new BufferDeviceShuttingDownException());
                    }
                    _pendingLoadOrInit.Clear();
                }

                // Invalidate buffer device object
                _bufferDevice = null;
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Adds a file to the underlying multiple buffer device.
        /// </summary>
        /// <param name="name">The name of the device.</param>
        /// <param name="pathName">The pathname of the associated file.</param>
        /// <param name="deviceId">The device id for the new device or <see cref="DeviceId.Zero" />
        /// if the device identifier should be automatically determined.</param>
        /// <param name="createPageCount">The number of pages to allocate when creating the file;
        /// if the file exists then set this to zero.</param>
        /// <returns>
        /// A <see cref="DeviceId" /> representing the new device.
        /// </returns>
        public Task<DeviceId> AddDeviceAsync(string name, string pathName, DeviceId deviceId, uint createPageCount)
        {
            CheckDisposed();
            return _bufferDevice.AddDeviceAsync(name, pathName, deviceId, createPageCount);
        }

        /// <summary>
        /// Removes the file associated with the given device identifier.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public Task RemoveDeviceAsync(DeviceId deviceId)
        {
            CheckDisposed();
            return _bufferDevice.RemoveDeviceAsync(deviceId);
        }

        /// <summary>
        /// Returns an initialised <see cref="PageBuffer" /> associated with the
        /// specified <see cref="VirtualPageId" />.
        /// </summary>
        /// <param name="pageId">The virtual page identifier.</param>
        /// <returns>
        /// An instance of <see cref="PageBuffer" />.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<IPageBuffer> InitPageAsync(VirtualPageId pageId)
        {
            var request = new PreparePageBufferRequest(pageId);
            if (!_initBufferPort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Returns a loaded <see cref="PageBuffer" /> associated with the
        /// specified <see cref="VirtualPageId" />.
        /// </summary>
        /// <param name="pageId">The virtual page identifier.</param>
        /// <returns>
        /// An instance of <see cref="PageBuffer" />.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        /// <remarks>
        /// This method will not return the page buffer until one of the
        /// following has occurred;
        /// 1. the instance has it's pending reads flushed
        /// 2. the queue of pending operations exceeds a certain threshold
        /// 3. a read timeout occurs
        /// </remarks>
        public Task<IPageBuffer> LoadPageAsync(VirtualPageId pageId)
        {
            var request = new PreparePageBufferRequest(pageId);
            if (!_loadBufferPort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Flushes pending operations.
        /// </summary>
        /// <param name="flushParams"></param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        /// <remarks>
        /// If reads are flushed then all pending calls to <see cref="LoadPageAsync" />
        /// will be completed.
        /// </remarks>
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

        private bool TryGetExistingInitOrLoadTask(VirtualPageId pageId, out Task<IPageBuffer> pendingOperation)
        {
            pendingOperation = Task.FromResult<IPageBuffer>(null);

            // If the same page is already queued for pending init or load
            //  then reuse same completion task
            var isAlreadyPending = false;
            var pendingTaskSource = _pendingLoadOrInit.AddOrUpdate(
                pageId,
                new TaskCompletionSource<IPageBuffer>(),
                (key, existingSource) =>
                {
                    isAlreadyPending = true;
                    return existingSource;
                });

            pendingOperation = pendingTaskSource.Task;
            return isAlreadyPending;
        }

        private IPageBuffer GetOrAllocatePageBuffer(VirtualPageId pageId, out bool isNewBuffer)
        {
            IPageBuffer buffer = null;
            var newBuffer = false;

            // Buffer lookup occurs within a lock...
            _bufferLookupLock.Execute(
                () =>
                {
                    // Check whether the buffer is already in our cache
                    if (_bufferLookup.TryGetValue(pageId, out var cacheInfo))
                    {
                        // Retrieve cached buffer
                        // NOTE: Buffer is addref'ed in property accessor (naughty)
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
                        _bufferLookup.Add(pageId, new BufferCacheInfo(buffer));
                        newBuffer = true;
                    }
                });

            isNewBuffer = newBuffer;
            return buffer;
        }

        private Task<IPageBuffer> HandleInit(PreparePageBufferRequest request)
        {
            // Sanity check
            CheckDisposed();

            // If the same page is already queued for pending init or load
            //  then reuse same completion task
            if (TryGetExistingInitOrLoadTask(request.PageId, out var pendingTask))
            {
                return pendingTask;
            }

            // Fetch or allocate the buffer (if it's cached we can complete early)
            var buffer = GetOrAllocatePageBuffer(request.PageId, out var isNewBuffer);
            if (!isNewBuffer)
            {
                NotifyWaitersLoadOrInitTaskCompleted(request.PageId, buffer);
            }
            else
            {
                RequestInitPageBuffer(buffer, request.PageId);
            }

            return pendingTask;
        }

        private Task<IPageBuffer> HandleLoad(PreparePageBufferRequest request)
        {
            // Sanity check
            CheckDisposed();

            // If the same page is already queued for pending init or load
            //  then reuse same completion task
            if (TryGetExistingInitOrLoadTask(request.PageId, out var pendingTask))
            {
                return pendingTask;
            }

            // Fetch or allocate the buffer (if it's cached we can complete early)
            var buffer = GetOrAllocatePageBuffer(request.PageId, out var isNewBuffer);
            if (!isNewBuffer)
            {
                NotifyWaitersLoadOrInitTaskCompleted(request.PageId, buffer);
            }
            else
            {
                RequestLoadPageBuffer(buffer, request.PageId);
            }

            return pendingTask;
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
                        _storageEngineEventService.CachingPageBufferFlushScavengeStart(
                            _bufferLookup.Count,
                            _cacheSettings.CacheScavengeOnThreshold);
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
                            _storageEngineEventService.CachingPageBufferFlushScavengeStart(
                                _bufferLookup.Count,
                                _cacheSettings.CacheScavengeOffThreshold);
                            IsScavenging = false;
                        }
                    }
                }

                if (!_shutdownToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task
                            .Delay(_cacheSettings.CacheFlushInterval, _shutdownToken.Token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }

            // Wait for flush to finish if it is still running
            // NOTE: We will wait for a maximum of 30 seconds
            var start = DateTime.UtcNow;
            while (_flushState != CacheFlushState.Idle &&
                (DateTime.UtcNow - start) < TimeSpan.FromSeconds(30))
            {
                Thread.Sleep(100);
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
                    try
                    {
                        await Task
                            .Delay(_cacheSettings.FreePoolMonitorInterval, _shutdownToken.Token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
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

        private async Task<bool> HandleFlushPageBuffersAsync(FlushCachingDeviceRequest request)
        {
            using (LogContext.PushProperty("Method", nameof(HandleFlushPageBuffersAsync)))
            {
                // Determine new flush state
                var newState = CacheFlushState.FlushNormal;
                if (request.Message.IsForCheckPoint)
                {
                    newState = CacheFlushState.FlushCheckPoint;
                }

                // Switch cache flush state now
                _flushState = newState;
                try
                {
                    // Make copy of cache keys
                    // NOTE: This could be rather expensive...
                    VirtualPageId[] keys = null;
                    _bufferLookupLock.Execute(
                        () =>
                        {
                            keys = new VirtualPageId[_bufferLookup.Count];
                            _bufferLookup.Keys.CopyTo(keys, 0);
                        });

                    // Limit key block to those associated with flush device
                    if (!request.Message.AllDevices)
                    {
                        keys = keys
                            .Where(key => key.DeviceId == request.Message.DeviceId)
                            .ToArray();
                    }

                    if (keys.Length > 0)
                    {
                        // Create parallel operation that acts on chunks of page cache
                        FlushPageBufferState finalFlushState = null;
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
                                finalFlushState = blockState;
                            });

                        // Execute appropriate flush instruction on all accessed devices
                        if (finalFlushState != null)
                        {
                            // Issue flush to each device accessed
                            await finalFlushState
                                .FlushAccessedDevicesAsync(_bufferDevice)
                                .ConfigureAwait(false);
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
        }

        /// <summary>
        /// Processes the cache buffer entry.
        /// </summary>
        /// <param name="pageId">The page identifier.</param>
        /// <param name="loopState">State of the loop.</param>
        /// <param name="index">The index.</param>
        /// <param name="blockState">State of the block.</param>
        /// <returns>
        /// The potentially updated <see cref="FlushPageBufferState"/> that
        /// represents the updated state.
        /// </returns>
        /// <remarks>
        /// The entire cache is partitioned and processed by a set of threads
        /// when pending requests are flushed by the I/O coordination thread.
        /// This method does the actual work for a given cache buffer entry.
        /// </remarks>
        private FlushPageBufferState ProcessCacheBufferEntry(
            VirtualPageId pageId,
            ParallelLoopState loopState,
            long index,
            FlushPageBufferState blockState)
        {
            using (LogContext.PushProperty("Method", nameof(ProcessCacheBufferEntry)))
            {
                // Retrieve entry from cache; skip if no longer present
                if (!_bufferLookup.TryGetValue(pageId, out var cacheInfo))
                {
                    return blockState;
                }

                // Process pages we can load
                if (blockState.Params.FlushReads && cacheInfo.IsReadPending)
                {
                    // Create async task to load the cache info and add to list
                    blockState.LoadTasks.Add(LoadBufferCacheInfoAsync(cacheInfo));

                    // Signal device has pending load
                    blockState.MarkDeviceAsAccessedForLoad(pageId.DeviceId);
                }

                // Process pages we can save
                else if (blockState.Params.FlushWrites && cacheInfo.IsWritePending)
                {
                    // This may throw if another thread changes the
                    //	buffer state before it begins the write operation
                    blockState.SaveTasks.Add(SaveBufferCacheInfoAsync(cacheInfo));

                    // Signal device has pending save
                    blockState.MarkDeviceAsAccessedForSave(pageId.DeviceId);
                }

                // Process pages we can scavenge
                else if (IsScavenging && cacheInfo.CanFree)
                {
                    // Free the buffer (may throw)
                    cacheInfo.BufferInternal.SetFreeAsync();

                    // Remove cache item and decrement count
                    _bufferLookup.Remove(pageId);
                    Interlocked.Decrement(ref _cacheSize);

                    // Add to free pages if we can
                    if (_freePagePool.Count < _cacheSettings.MaximumFreePoolSize)
                    {
                        // Add buffer to free pool and disconnect from cache info
                        _freePagePool.PutObject(cacheInfo.BufferInternal);
                        cacheInfo.RemoveBufferInternal();
                    }

                    // Discard the cache info
                    cacheInfo.Dispose();
                }

                return blockState;
            }
        }

        /// <summary>
        /// Requests the specified buffer is initialised.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="pageId">The page identifier.</param>
        /// <remarks>
        /// Initialisation requests are carried out immediately.
        /// </remarks>
        private async void RequestInitPageBuffer(IPageBuffer buffer, VirtualPageId pageId)
        {
            try
            {
                await buffer.InitAsync(pageId, LogicalPageId.Zero).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                NotifyWaitersLoadOrInitTaskCancelled(pageId);
                return;
            }
            catch (Exception exception)
            {
                NotifyWaitersLoadOrInitTaskFailed(pageId, exception);
                return;
            }

            NotifyWaitersLoadOrInitTaskCompleted(pageId, buffer);
        }

        /// <summary>
        /// Requests the specified buffer is loaded from storage.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="pageId">The page identifier.</param>
        /// <remarks>
        /// Load requests are deferred until the read-request queue is flushed.
        /// </remarks>
        private async void RequestLoadPageBuffer(IPageBuffer buffer, VirtualPageId pageId)
        {
            try
            {
                await buffer.RequestLoadAsync(pageId, LogicalPageId.Zero).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                NotifyWaitersLoadOrInitTaskCancelled(pageId);
                return;
            }
            catch (Exception exception)
            {
                NotifyWaitersLoadOrInitTaskFailed(pageId, exception);
                return;
            }

            NotifyWaitersLoadOrInitTaskCompleted(pageId, buffer);
        }

        /// <summary>
        /// Passes the request to load the cache entry to the underlying
        /// storage subsystem
        /// </summary>
        /// <param name="cacheInfo">The cache information.</param>
        /// <returns></returns>
        private async Task LoadBufferCacheInfoAsync(BufferCacheInfo cacheInfo)
        {
            try
            {
                await cacheInfo.BufferInternal.LoadAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                NotifyWaitersLoadOrInitTaskCancelled(cacheInfo.PageId);
                return;
            }
            catch (Exception exception)
            {
                NotifyWaitersLoadOrInitTaskFailed(cacheInfo.PageId, exception);
                return;
            }
            NotifyWaitersLoadOrInitTaskCompleted(cacheInfo.PageId, cacheInfo.PageBuffer);
        }

        private async Task SaveBufferCacheInfoAsync(BufferCacheInfo cacheInfo)
        {
            try
            {
                await cacheInfo.BufferInternal.SaveAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // ignored
                Debug.WriteLine(
                    "SaveBufferCacheInfo failed - likely saved by another thread [{ExceptionMessage}]",
                    e.Message);
            }
        }

        private void NotifyWaitersLoadOrInitTaskCompleted(VirtualPageId pageId, IPageBuffer buffer)
        {
            RemovePendingLoadOrInitTaskAndNotifyWaiters(
                pageId, ct => ct.TrySetResult(buffer));
        }

        private void NotifyWaitersLoadOrInitTaskFailed(VirtualPageId pageId, Exception error)
        {
            RemovePendingLoadOrInitTaskAndNotifyWaiters(
                pageId, ct => ct.TrySetException(error));
        }

        private void NotifyWaitersLoadOrInitTaskCancelled(VirtualPageId pageId)
        {
            RemovePendingLoadOrInitTaskAndNotifyWaiters(
                pageId, ct => ct.TrySetCanceled());
        }

        private void RemovePendingLoadOrInitTaskAndNotifyWaiters(
            VirtualPageId pageId, Action<TaskCompletionSource<IPageBuffer>> completionAction)
        {
            if (_pendingLoadOrInit.TryRemove(pageId, out var task))
            {
                completionAction(task);
            }
        }
        #endregion
    }
}
