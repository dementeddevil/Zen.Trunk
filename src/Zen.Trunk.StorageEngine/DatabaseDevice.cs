using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Transactions;
using Autofac;
using Serilog;
using Zen.Trunk.Extensions;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Logging;
using Zen.Trunk.Utils;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>DatabaseDevice</c> encapsulates all the operations needed to support
    /// operations on database devices.
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.PageDevice" />
    /// <remarks>
    /// Operations dealt with;
    /// Open, close, grow, shrink, page allocation/deallocation.
    /// </remarks>
    public class DatabaseDevice : PageDevice, IDatabaseDevice
    {
        #region Private Types
        private class AddFileGroupDeviceRequest : TransactionContextTaskRequest<AddFileGroupDeviceParameters, Tuple<DeviceId, string>>
        {
            public AddFileGroupDeviceRequest(AddFileGroupDeviceParameters deviceParams)
                : base(deviceParams)
            {
            }
        }

        private class RemoveFileGroupDeviceRequest : TransactionContextTaskRequest<RemoveFileGroupDeviceParameters, bool>
        {
            public RemoveFileGroupDeviceRequest(RemoveFileGroupDeviceParameters deviceParams)
                : base(deviceParams)
            {
            }
        }

        private class InitFileGroupPageRequest : TransactionContextTaskRequest<InitFileGroupPageParameters, bool>
        {
            #region Public Constructors
            public InitFileGroupPageRequest(InitFileGroupPageParameters initParams)
                : base(initParams)
            {
            }
            #endregion
        }

        private class LoadFileGroupPageRequest : TransactionContextTaskRequest<LoadFileGroupPageParameters, bool>
        {
            #region Public Constructors
            public LoadFileGroupPageRequest(LoadFileGroupPageParameters loadParams)
                : base(loadParams)
            {
            }
            #endregion
        }

        private class DeallocateFileGroupPageRequest : TransactionContextTaskRequest<DeallocateFileGroupDataPageParameters, bool>
        {
            #region Public Constructors
            public DeallocateFileGroupPageRequest(DeallocateFileGroupDataPageParameters parameters)
                : base(parameters)
            {
            }
            #endregion
        }

        private class AddFileGroupAudioRequest : TransactionContextTaskRequest<AddFileGroupAudioParameters, ObjectId>
        {
            #region Public Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="AddFileGroupAudioRequest"/> class.
            /// </summary>
            /// <param name="tableParams">The table parameters.</param>
            public AddFileGroupAudioRequest(AddFileGroupAudioParameters audioParams)
                : base(audioParams)
            {
            }
            #endregion
        }

        private class AddFileGroupAudioIndexRequest : TransactionContextTaskRequest<AddFileGroupAudioIndexParameters, IndexId>
        {
            #region Public Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="AddFileGroupAudioIndexRequest"/> class.
            /// </summary>
            /// <param name="tableIndexParams">The table parameters.</param>
            public AddFileGroupAudioIndexRequest(AddFileGroupAudioIndexParameters parameters)
                : base(parameters)
            {
            }
            #endregion
        }

        private class AddFileGroupTableRequest : TransactionContextTaskRequest<AddFileGroupTableParameters, ObjectId>
        {
            #region Public Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="AddFileGroupTableRequest"/> class.
            /// </summary>
            /// <param name="tableParams">The table parameters.</param>
            public AddFileGroupTableRequest(AddFileGroupTableParameters tableParams)
                : base(tableParams)
            {
            }
            #endregion
        }

        private class AddFileGroupTableIndexRequest : TransactionContextTaskRequest<AddFileGroupTableIndexParameters, IndexId>
        {
            #region Public Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="AddFileGroupTableRequest"/> class.
            /// </summary>
            /// <param name="tableIndexParams">The table parameters.</param>
            public AddFileGroupTableIndexRequest(AddFileGroupTableIndexParameters tableIndexParams)
                : base(tableIndexParams)
            {
            }
            #endregion
        }

        private class FlushFileGroupRequest : TransactionContextTaskRequest<FlushCachingDeviceParameters, bool>
        {
            #region Public Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="FlushFileGroupRequest"/> class.
            /// </summary>
            /// <param name="flushParams">The flush parameters.</param>
            public FlushFileGroupRequest(FlushCachingDeviceParameters flushParams)
                : base(flushParams)
            {
            }
            #endregion
        }

        private class CreateObjectReferenceRequest : TransactionContextTaskRequest<CreateObjectReferenceParameters, ObjectId>
        {
            #region Public Constructors
            public CreateObjectReferenceRequest(CreateObjectReferenceParameters parameters)
                : base(parameters)
            {
            }
            #endregion
        }

        public class GetObjectReferenceRequest : TransactionContextTaskRequest<GetObjectReferenceParameters, ObjectReferenceResult>
        {
            #region Public Constructors
            public GetObjectReferenceRequest(GetObjectReferenceParameters parameters) : base(parameters)
            {
            }
            #endregion
        }

        private class IssueCheckPointRequest : TransactionContextTaskRequest<bool>
        {
        }

        private class AddLogDeviceRequest : TransactionContextTaskRequest<AddLogDeviceParameters, Tuple<DeviceId, string>>
        {
            public AddLogDeviceRequest(AddLogDeviceParameters parameters) : base(parameters)
            {
            }
        }

        private class RemoveLogDeviceRequest : TransactionContextTaskRequest<RemoveLogDeviceParameters, bool>
        {
            public RemoveLogDeviceRequest(RemoveLogDeviceParameters parameters) : base(parameters)
            {
            }
        }
        #endregion

        #region Private Fields
        private static readonly ILogger Logger = Serilog.Log.ForContext<DatabaseDevice>();
        private readonly DatabaseId _dbId;

        // Underlying page buffer storage
        private IMultipleBufferDevice _bufferDevice;
        private ICachingPageBufferDevice _dataBufferDevice;

        // File-group mapping
        private readonly Dictionary<FileGroupId, IFileGroupDevice> _fileGroupById =
            new Dictionary<FileGroupId, IFileGroupDevice>();
        private readonly Dictionary<string, IFileGroupDevice> _fileGroupByName =
            new Dictionary<string, IFileGroupDevice>();
        private FileGroupId _nextFileGroupId = FileGroupId.Primary.Next;

        // Object identifier tracking
        private ObjectId _nextObjectId = new ObjectId(1);
        private readonly Dictionary<ObjectId, ObjectReferenceBufferFieldWrapper> _objects =
            new Dictionary<ObjectId, ObjectReferenceBufferFieldWrapper>();

        // Log device
        private MasterLogPageDevice _masterLogPageDevice;
        private Task _currentCheckPointTask;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseDevice"/> class.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        public DatabaseDevice(DatabaseId dbId)
        {
            _dbId = dbId;

            // Setup ports
            var taskInterleave = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default);
            InitFileGroupPagePort =
                new TransactionContextActionBlock<InitFileGroupPageRequest, bool>(
                    InitFileGroupPageHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ConcurrentScheduler
                    });
            LoadFileGroupPagePort =
                new TransactionContextActionBlock<LoadFileGroupPageRequest, bool>(
                    LoadFileGroupPageHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ConcurrentScheduler
                    });
            DeallocateFileGroupPagePort =
                new TransactionContextActionBlock<DeallocateFileGroupPageRequest, bool>(
                    DeallocateFileGroupPageHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ConcurrentScheduler
                    });
            AddFileGroupDevicePort =
                new TransactionContextActionBlock<AddFileGroupDeviceRequest, Tuple<DeviceId, string>>(
                    AddFileGroupDataDeviceHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });
            RemoveFileGroupDevicePort =
                new TransactionContextActionBlock<RemoveFileGroupDeviceRequest, bool>(
                    RemoveFileGroupDeviceHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });

            FlushPageBuffersPort =
                new TaskRequestActionBlock<FlushFileGroupRequest, bool>(
                    FlushDeviceBuffersHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });
            CreateObjectReferencePort = new TransactionContextActionBlock<CreateObjectReferenceRequest, ObjectId>(
                CreateObjectReferenceHandlerAsync,
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            GetObjectReferencePort = new TransactionContextActionBlock<GetObjectReferenceRequest, ObjectReferenceResult>(
                GetObjectReferenceHandlerAsync,
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler
                });

            // Audio action ports
            AddFileGroupAudioPort =
                new TransactionContextActionBlock<AddFileGroupAudioRequest, ObjectId>(
                    AddFileGroupAudioHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });
            AddFileGroupAudioIndexPort =
                new TransactionContextActionBlock<AddFileGroupAudioIndexRequest, IndexId>(
                    AddFileGroupAudioIndexHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });

            // Table action ports
            AddFileGroupTablePort =
                new TransactionContextActionBlock<AddFileGroupTableRequest, ObjectId>(
                    AddFileGroupTableHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });
            AddFileGroupTableIndexPort =
                new TransactionContextActionBlock<AddFileGroupTableIndexRequest, IndexId>(
                    AddFileGroupTableIndexHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });

            IssueCheckPointPort =
                new TransactionContextActionBlock<IssueCheckPointRequest, bool>(
                    IssueCheckPointHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });

            AddLogDevicePort =
                new TransactionContextActionBlock<AddLogDeviceRequest, Tuple<DeviceId, string>>(
                    AddLogDeviceHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });
            RemoveLogDevicePort =
                new TransactionContextActionBlock<RemoveLogDeviceRequest, bool>(
                    RemoveLogDeviceHandlerAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the primary file group device.
        /// </summary>
        /// <value>
        /// The primary file group device.
        /// </value>
        public IFileGroupDevice PrimaryFileGroupDevice => GetPrimaryFileGroupDevice();

        /// <summary>
        /// Gets the <see cref="FileGroupDevice"/> with the specified file group name.
        /// </summary>
        /// <value>
        /// The <see cref="FileGroupDevice"/>.
        /// </value>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <returns></returns>
        public IFileGroupDevice this[string fileGroupName] => GetFileGroupDevice(fileGroupName);

        /// <summary>
        /// Gets the <see cref="FileGroupDevice"/> with the specified file group identifier.
        /// </summary>
        /// <value>
        /// The <see cref="FileGroupDevice"/>.
        /// </value>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <returns></returns>
        public IFileGroupDevice this[FileGroupId fileGroupId] => GetFileGroupDevice(fileGroupId);
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the primary file group id.
        /// </summary>
        /// <value>The primary file group id.</value>
        protected virtual FileGroupId PrimaryFileGroupId => FileGroupId.Primary;
        #endregion

        #region Private Properties
        private IMultipleBufferDevice RawBufferDevice
        {
            get
            {
                if (_bufferDevice == null)
                {
                    _bufferDevice = GetService<IMultipleBufferDevice>();
                }
                return _bufferDevice;
            }
        }

        private ICachingPageBufferDevice CachingBufferDevice
        {
            get
            {
                if (_dataBufferDevice == null)
                {
                    _dataBufferDevice = GetService<ICachingPageBufferDevice>();
                }
                return _dataBufferDevice;
            }
        }

        /// <summary>
        /// Gets the add file group data device port.
        /// </summary>
        /// <value>The add file group data device port.</value>
        private ITargetBlock<AddFileGroupDeviceRequest> AddFileGroupDevicePort { get; }

        /// <summary>
        /// Gets the remove file group device port.
        /// </summary>
        /// <value>The remove file group device port.</value>
        private ITargetBlock<RemoveFileGroupDeviceRequest> RemoveFileGroupDevicePort { get; }

        /// <summary>
        /// Gets the init file group page port.
        /// </summary>
        /// <value>The init file group page port.</value>
        private ITargetBlock<InitFileGroupPageRequest> InitFileGroupPagePort { get; }

        /// <summary>
        /// Gets the load file group page port.
        /// </summary>
        /// <value>The load file group page port.</value>
        private ITargetBlock<LoadFileGroupPageRequest> LoadFileGroupPagePort { get; }

        /// <summary>
        /// Gets the deallocate file group page port.
        /// </summary>
        /// <value>
        /// The deallocate file group page port.
        /// </value>
        private ITargetBlock<DeallocateFileGroupPageRequest> DeallocateFileGroupPagePort { get; }

        /// <summary>
        /// Gets the flush device buffers port.
        /// </summary>
        /// <value>The flush device buffers port.</value>
        private ITargetBlock<FlushFileGroupRequest> FlushPageBuffersPort { get; }

        /// <summary>
        /// Gets the add file group audio port.
        /// </summary>
        /// <value>The add file group audio port.</value>
        private ITargetBlock<AddFileGroupAudioRequest> AddFileGroupAudioPort { get; }

        /// <summary>
        /// Gets the add file group audio index port.
        /// </summary>
        /// <value>
        /// The add file group audio index port.
        /// </value>
        private ITargetBlock<AddFileGroupAudioIndexRequest> AddFileGroupAudioIndexPort { get; }

        /// <summary>
        /// Gets the add file group table port.
        /// </summary>
        /// <value>The add file group table port.</value>
        private ITargetBlock<AddFileGroupTableRequest> AddFileGroupTablePort { get; }

        /// <summary>
        /// Gets the add file-group table index port.
        /// </summary>
        /// <value>The add file-group table index port.</value>
        private ITargetBlock<AddFileGroupTableIndexRequest> AddFileGroupTableIndexPort { get; }

        /// <summary>
        /// Gets the create object reference port.
        /// </summary>
        /// <value>
        /// The create object reference port.
        /// </value>
        private ITargetBlock<CreateObjectReferenceRequest> CreateObjectReferencePort { get; }

        /// <summary>
        /// Gets the get object reference port.
        /// </summary>
        /// <value>
        /// The get object reference port.
        /// </value>
        private ITargetBlock<GetObjectReferenceRequest> GetObjectReferencePort { get; }

        /// <summary>
        /// Gets the issue check point port.
        /// </summary>
        /// <value>The issue check point port.</value>
        private ITargetBlock<IssueCheckPointRequest> IssueCheckPointPort { get; }

        private ITargetBlock<AddLogDeviceRequest> AddLogDevicePort { get; }

        private ITargetBlock<RemoveLogDeviceRequest> RemoveLogDevicePort { get; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a file group device to this instance.
        /// </summary>
        /// <param name="deviceParams">The device parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<Tuple<DeviceId, string>> AddFileGroupDeviceAsync(AddFileGroupDeviceParameters deviceParams)
        {
            var request = new AddFileGroupDeviceRequest(deviceParams);
            if (!AddFileGroupDevicePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Removes the file group device.
        /// </summary>
        /// <param name="deviceParams">The device parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task RemoveFileGroupDeviceAsync(RemoveFileGroupDeviceParameters deviceParams)
        {
            var request = new RemoveFileGroupDeviceRequest(deviceParams);
            if (!RemoveFileGroupDevicePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Initializes the file group page.
        /// </summary>
        /// <param name="initParams">The initialize parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="FileGroupInvalidException">
        /// Thrown if the appropriate filegroup device cannot be resolved.
        /// </exception>
        /// <exception cref="BufferDeviceShuttingDownException">
        /// Thrown if the buffer device is shutting down.
        /// </exception>
        public Task InitFileGroupPageAsync(InitFileGroupPageParameters initParams)
        {
            var request = new InitFileGroupPageRequest(initParams);
            if (!InitFileGroupPagePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Loads the file group page.
        /// </summary>
        /// <param name="loadParams">The load parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="FileGroupInvalidException">
        /// Thrown if the appropriate filegroup device cannot be resolved.
        /// </exception>
        /// <exception cref="BufferDeviceShuttingDownException">
        /// Thrown if the buffer device is shutting down.
        /// </exception>
        public Task LoadFileGroupPageAsync(LoadFileGroupPageParameters loadParams)
        {
            var request = new LoadFileGroupPageRequest(loadParams);
            if (!LoadFileGroupPagePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Deallocates the file group page.
        /// </summary>
        /// <param name="deallocParams">The dealloc parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="FileGroupInvalidException">
        /// Thrown if the appropriate filegroup device cannot be resolved.
        /// </exception>
        /// <exception cref="BufferDeviceShuttingDownException">
        /// Thrown if the buffer device is shutting down.
        /// </exception>
        public Task DeallocateFileGroupPageAsync(DeallocateFileGroupDataPageParameters deallocParams)
        {
            var request = new DeallocateFileGroupPageRequest(deallocParams);
            if (!DeallocateFileGroupPagePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Flushes the file group buffers.
        /// </summary>
        /// <param name="flushParams">The flush parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task FlushFileGroupBuffersAsync(FlushCachingDeviceParameters flushParams)
        {
            var request = new FlushFileGroupRequest(flushParams);
            if (!FlushPageBuffersPort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Creates the object reference.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<ObjectId> CreateObjectReferenceAsync(CreateObjectReferenceParameters parameters)
        {
            var request = new CreateObjectReferenceRequest(parameters);
            if (!CreateObjectReferencePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Gets the object reference asynchronous.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<ObjectReferenceResult> GetObjectReferenceAsync(GetObjectReferenceParameters parameters)
        {
            var request = new GetObjectReferenceRequest(parameters);
            if (!GetObjectReferencePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Adds the file group audio.
        /// </summary>
        /// <param name="parameters">The audio parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<ObjectId> AddFileGroupAudioAsync(AddFileGroupAudioParameters parameters)
        {
            var request = new AddFileGroupAudioRequest(parameters);
            if (!AddFileGroupAudioPort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Adds the file group audio index.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<IndexId> AddFileGroupAudioIndexAsync(AddFileGroupAudioIndexParameters parameters)
        {
            var request = new AddFileGroupAudioIndexRequest(parameters);
            if (!AddFileGroupAudioIndexPort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Adds the file group table.
        /// </summary>
        /// <param name="parameters">The table parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<ObjectId> AddFileGroupTableAsync(AddFileGroupTableParameters parameters)
        {
            var request = new AddFileGroupTableRequest(parameters);
            if (!AddFileGroupTablePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Adds the file group table index.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<IndexId> AddFileGroupTableIndexAsync(AddFileGroupTableIndexParameters parameters)
        {
            var request = new AddFileGroupTableIndexRequest(parameters);
            if (!AddFileGroupTableIndexPort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Issues the check point.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task IssueCheckPointAsync()
        {
            var request = new IssueCheckPointRequest();
            if (!IssueCheckPointPort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Adds the log device.
        /// </summary>
        /// <param name="deviceParams">The device parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<Tuple<DeviceId, string>> AddLogDeviceAsync(AddLogDeviceParameters deviceParams)
        {
            var request = new AddLogDeviceRequest(deviceParams);
            if (!AddLogDevicePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Removes the log device.
        /// </summary>
        /// <param name="deviceParams">The device parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task RemoveLogDeviceAsync(RemoveLogDeviceParameters deviceParams)
        {
            var request = new RemoveLogDeviceRequest(deviceParams);
            if (!RemoveLogDevicePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Locks the database.
        /// </summary>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="lockTimeout">The lock timeout.</param>
        /// <returns></returns>
        public Task LockDatabaseAsync(DatabaseLockType lockType, TimeSpan lockTimeout)
        {
            var dlm = GetService<IDatabaseLockManager>();
            return dlm.LockDatabaseAsync(lockType, lockTimeout);
        }

        /// <summary>
        /// Unlocks the database.
        /// </summary>
        /// <returns></returns>
        public Task UnlockDatabaseAsync()
        {
            var dlm = GetService<IDatabaseLockManager>();
            return dlm.UnlockDatabaseAsync();
        }

        /// <summary>
        /// Uses the database.
        /// </summary>
        /// <param name="lockTimeout">The lock timeout.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// The difference between this and <see cref="LockDatabaseAsync"/>
        /// is that this method executes without a current transaction context
        /// and therefore relies upon the session context for locking.
        /// </remarks>
        public async Task UseDatabaseAsync(TimeSpan lockTimeout)
        {
            // Obtain lock on database via session lock - not transaction lock
            using (TrunkTransactionContext.SwitchTransactionContext(null))
            {
                await LockDatabaseAsync(DatabaseLockType.Shared, lockTimeout).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Unuses the database.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// The difference between this and <see cref="UnlockDatabaseAsync"/>
        /// is that this method executes without a current transaction context
        /// and therefore relies upon the session context for locking.
        /// </remarks>
        public async Task UnuseDatabaseAsync()
        {
            // Obtain lock on database via session lock - not transaction lock
            using (TrunkTransactionContext.SwitchTransactionContext(null))
            {
                await UnlockDatabaseAsync().ConfigureAwait(false);
            }
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// Processes the object references following the initial load of a set of
        /// file-group root pages.
        /// </summary>
        /// <param name="objectReferences">The object references.</param>
        internal void ProcessObjectReferences(ICollection<ObjectReferenceBufferFieldWrapper> objectReferences)
        {
            foreach (var objRef in objectReferences)
            {
                _objects.Add(objRef.ObjectId, objRef);
                if (objRef.ObjectId > _nextObjectId)
                {
                    _nextObjectId = new ObjectId(objRef.ObjectId.Value + 1);
                }
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Builds the device lifetime scope.
        /// </summary>
        /// <param name="builder">The builder.</param>
        protected override void BuildDeviceLifetimeScope(ContainerBuilder builder)
        {
            base.BuildDeviceLifetimeScope(builder);

            builder
                .Register(
                    context =>
                    {
                        var deviceFactory = context.Resolve<IBufferDeviceFactory>();
                        return deviceFactory.CreateMultipleBufferDevice(true);
                    })
                .As<IBufferDevice>()
                .As<IMultipleBufferDevice>()
                .SingleInstance();
            builder.RegisterType<CachingPageBufferDevice>()
                .As<ICachingPageBufferDevice>()
                .SingleInstance();

            builder
                .RegisterType<DatabaseLockManager>()
                .WithParameter("dbId", _dbId)
                .As<IDatabaseLockManager>()
                .SingleInstance();

            builder
                .Register(context => _masterLogPageDevice)
                .As<ILogPageDevice>()
                .As<IMasterLogPageDevice>();

            builder.RegisterType<MasterDatabasePrimaryFileGroupDevice>()
                .OnActivated(e => e.Instance.InitialiseDeviceLifetimeScope(LifetimeScope));
            builder.RegisterType<PrimaryFileGroupDevice>()
                .OnActivated(e => e.Instance.InitialiseDeviceLifetimeScope(LifetimeScope));
            builder.RegisterType<SecondaryFileGroupDevice>()
                .OnActivated(e => e.Instance.InitialiseDeviceLifetimeScope(LifetimeScope));
        }

        /// <summary>
        /// Called when opening the device.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// No file-groups.
        /// or
        /// No primary file-group device.
        /// </exception>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected override async Task OnOpenAsync()
        {
            // Sanity check
            if (_fileGroupById.Count == 0)
            {
                throw new InvalidOperationException("No file-groups.");
            }
            var fgDevice = PrimaryFileGroupDevice;
            if (fgDevice == null)
            {
                throw new InvalidOperationException("No primary file-group device.");
            }

            // Mount the underlying device
            Logger.Debug("Opening underlying buffer device...");
            await RawBufferDevice.OpenAsync().ConfigureAwait(false);

            // Mount the log device(s) first
            // This is so that transaction log is available when creating database
            Logger.Debug("Opening log device...");
            await GetService<IMasterLogPageDevice>().OpenAsync(IsCreate).ConfigureAwait(false);

            // If this is a create, then we want to create a transaction so
            //  that once the file-group devices are created we can commit
            //  their initial presentation and then issue a checkpoint.
            if (IsCreate)
            {
                BeginTransaction(IsolationLevel.Serializable, TimeSpan.FromSeconds(5));
            }

            // Mount the primary file-group device
            Logger.Debug("Opening primary file-group device...");
            await fgDevice.OpenAsync(IsCreate).ConfigureAwait(false);

            // At this point the primary file-group is mounted and all
            //	secondary file-groups are mounted too (via AddFileGroupDevice)

            // If this is not create then we need to perform recovery
            if (!IsCreate)
            {
                Logger.Debug("Initiating recovery...");
                await GetService<IMasterLogPageDevice>().PerformRecoveryAsync().ConfigureAwait(false);
            }
            else
            {
                Logger.Debug("Committing created pages on new database...");

                // Commit the transaction handling initialisation of new database
                await TrunkTransactionContext.CommitAsync().ConfigureAwait(false);

                Logger.Debug("Initiating first checkpoint on new database...");

                // Issue full checkpoint
                await IssueCheckPointAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Called when closing the device.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected override async Task OnCloseAsync()
        {
            using (Serilog.Context.LogContext.PushProperty("DatabaseDevice.OnCloseAsync", _dbId))
            {
                TrunkTransactionContext.BeginTransaction(LifetimeScope);
                var committed = false;
                Logger.Debug("Issuing checkpoint...");
                try
                {
                    // TODO: Skip checkpointing if device is read-only
                    // Issue a checkpoint so we close the database in a known state
                    var request = new IssueCheckPointRequest();
                    if (!IssueCheckPointPort.Post(request))
                    {
                        throw new BufferDeviceShuttingDownException();
                    }
                    await request.Task.ConfigureAwait(false);

                    await TrunkTransactionContext.CommitAsync().ConfigureAwait(false);
                    committed = true;
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
                if (!committed)
                {
                    await TrunkTransactionContext.RollbackAsync().ConfigureAwait(false);
                }

                // Close the caching page device
                Logger.Debug("Closing caching buffer device...");
                await CachingBufferDevice.CloseAsync().ConfigureAwait(false);

                // Close all secondary file-groups in parallel
                var secondaryDeviceTasks = _fileGroupByName.Values
                    .Where(item => !item.IsPrimaryFileGroup)
                    .Select(item => Task.Run(item.CloseAsync))
                    .ToArray();
                await TaskExtra
                    .WhenAllOrEmpty(secondaryDeviceTasks)
                    .ConfigureAwait(false);

                // Close the primary file-group last
                var primaryDevice = _fileGroupByName.Values
                    .FirstOrDefault(item => item.IsPrimaryFileGroup);
                if (primaryDevice != null)
                {
                    await primaryDevice.CloseAsync().ConfigureAwait(false);
                }

                // Close the log device
                await GetService<IMasterLogPageDevice>().CloseAsync().ConfigureAwait(false);

                // Close underlying buffer device
                await RawBufferDevice.CloseAsync().ConfigureAwait(false);

                // Invalidate objects
                _dataBufferDevice = null;
                _bufferDevice = null;
            }
        }
        #endregion

        #region Private Methods
        private async Task<Tuple<DeviceId, string>> AddFileGroupDataDeviceHandlerAsync(AddFileGroupDeviceRequest request)
        {
            // Create valid file group ID as needed
            IFileGroupDevice fileGroupDevice = null;
            var isFileGroupCreate = false;
            var isFileGroupOpenNeeded = false;
            if ((request.Message.FileGroupId.IsValid && !_fileGroupById.TryGetValue(request.Message.FileGroupId, out fileGroupDevice)) ||
                (request.Message.FileGroupId.IsInvalid && !_fileGroupByName.TryGetValue(request.Message.FileGroupName, out fileGroupDevice)))
            {
                // Since we are going to create a file-group device we must
                //	have a valid name...
                var fileGroupName = request.Message.FileGroupName;
                if (!string.IsNullOrEmpty(fileGroupName))
                {
                    fileGroupName = fileGroupName.Trim().ToUpper();
                }
                if (string.IsNullOrEmpty(fileGroupName))
                {
                    throw new ArgumentException("Request must have valid file-group name.");
                }

                // Create new file group device and add to map
                var fileGroupId = request.Message.FileGroupId;
                if (fileGroupId == FileGroupId.Master)
                {
                    fileGroupName = StorageConstants.PrimaryFileGroupName;
                    fileGroupDevice = GetService<MasterDatabasePrimaryFileGroupDevice>(
                        new NamedParameter("id", fileGroupId),
                        new NamedParameter("name", fileGroupName));
                }
                else if (fileGroupId == FileGroupId.Primary)
                {
                    fileGroupName = StorageConstants.PrimaryFileGroupName;
                    fileGroupDevice = GetService<PrimaryFileGroupDevice>(
                        new NamedParameter("id", fileGroupId),
                        new NamedParameter("name", fileGroupName));
                }
                else
                {
                    // Derive a new filegroup id if necessary
                    if (fileGroupId.IsInvalid)
                    {
                        fileGroupId = _nextFileGroupId = _nextFileGroupId.Next;
                    }

                    fileGroupDevice = GetService<SecondaryFileGroupDevice>(
                        new NamedParameter("id", fileGroupId),
                        new NamedParameter("name", fileGroupName));
                }

                // Setup container for filegroup device and hookup
                var fileGroupScope = LifetimeScope.BeginLifetimeScope(
                    builder =>
                    {
                        // Downstream requests for DatabaseDevice return this object.
                        builder.RegisterInstance(this).As<DatabaseDevice>();
                    });
                fileGroupDevice.InitialiseDeviceLifetimeScope(fileGroupScope);

                // Add device to our lookup tables
                _fileGroupById.Add(fileGroupId, fileGroupDevice);
                _fileGroupByName.Add(fileGroupName, fileGroupDevice);

                isFileGroupCreate = request.Message.CreatePageCount > 0;
                isFileGroupOpenNeeded = true;
            }

            // Add child device to file-group
            // ReSharper disable once PossibleNullReferenceException
            var deviceInfo = await fileGroupDevice
                .AddDataDeviceAsync(request.Message)
                .ConfigureAwait(false);

            // If this is the first call for a file-group AND database is open or opening
            //	then open the new file-group device too
            if (isFileGroupOpenNeeded && (
                DeviceState == MountableDeviceState.Opening ||
                DeviceState == MountableDeviceState.Open))
            {
                await fileGroupDevice
                    .OpenAsync(isFileGroupCreate)
                    .ConfigureAwait(false);
            }

            // We're done
            return deviceInfo;
        }

        private async Task<bool> RemoveFileGroupDeviceHandlerAsync(RemoveFileGroupDeviceRequest request)
        {
            var fileGroupDevice = GetFileGroupDeviceCore(FileGroupId.Invalid, request.Message.FileGroupName);
            await fileGroupDevice.RemoveDataDeviceAsync(request.Message).ConfigureAwait(false);
            return true;
        }

        private async Task<Tuple<DeviceId, string>> AddLogDeviceHandlerAsync(AddLogDeviceRequest request)
        {
            if (_masterLogPageDevice == null)
            {
                _masterLogPageDevice = new MasterLogPageDevice(string.Empty);
                _masterLogPageDevice.InitialiseDeviceLifetimeScope(LifetimeScope);
            }

            return await _masterLogPageDevice.AddDeviceAsync(request.Message).ConfigureAwait(false);
        }

        private async Task<bool> RemoveLogDeviceHandlerAsync(RemoveLogDeviceRequest request)
        {
            if (_masterLogPageDevice != null)
            {
                if (request.Message.DeviceIdValid && request.Message.DeviceId == DeviceId.Primary)
                {
                    throw new InvalidOperationException();
                }

                await _masterLogPageDevice.RemoveDeviceAsync(request.Message).ConfigureAwait(false);
                return true;
            }
            return false;
        }

        private async Task<bool> InitFileGroupPageHandlerAsync(InitFileGroupPageRequest request)
        {
            // Locate appropriate filegroup device
            var fileGroupDevice = GetFileGroupDeviceCore(
                request.Message.FileGroupId, request.Message.FileGroupName);

            // Pass request onwards
            await fileGroupDevice
                .InitDataPageAsync(request.Message)
                .ConfigureAwait(false);
            return true;
        }

        private async Task<bool> LoadFileGroupPageHandlerAsync(LoadFileGroupPageRequest request)
        {
            // Locate appropriate filegroup device
            var fileGroupDevice = GetFileGroupDeviceCore(
                request.Message.FileGroupId, request.Message.FileGroupName);

            // Pass request onwards
            await fileGroupDevice
                .LoadDataPageAsync(request.Message)
                .ConfigureAwait(false);
            return true;
        }

        private async Task<bool> DeallocateFileGroupPageHandlerAsync(DeallocateFileGroupPageRequest request)
        {
            // Locate appropriate filegroup device
            var fileGroupDevice = GetFileGroupDeviceCore(
                request.Message.FileGroupId, request.Message.FileGroupName);

            // Pass request onwards
            await fileGroupDevice
                .DeallocateDataPageAsync(request.Message)
                .ConfigureAwait(false);
            return true;
        }

        private async Task<bool> FlushDeviceBuffersHandlerAsync(FlushFileGroupRequest request)
        {
            // Delegate request through to caching buffer device
            await CachingBufferDevice.FlushPagesAsync(request.Message).ConfigureAwait(false);
            return true;
        }

        private async Task<ObjectId> CreateObjectReferenceHandlerAsync(CreateObjectReferenceRequest request)
        {
            // Determine object id
            var objectId = _nextObjectId;
            _nextObjectId = new ObjectId(_nextObjectId.Value + 1);

            // Build object reference information.
            var objectRef =
                new InsertObjectReferenceParameters(
                    objectId,
                    request.Message.Name,
                    request.Message.ObjectType,
                    request.Message.FileGroupId,
                    await request.Message.FirstPageFunc(objectId).ConfigureAwait(false)
                );

            // Load primary file-group root page
            var primaryFileGroupDevice = _fileGroupById[request.Message.FileGroupId] as PrimaryFileGroupDevice;
            if (primaryFileGroupDevice == null)
            {
                throw new ArgumentException("File group identifier must link to primary file group device.");
            }

            // Delegate update of primary file-group root pages
            await primaryFileGroupDevice
                .InsertObjectReferenceAsync(objectRef)
                .ConfigureAwait(false);

            // Add new object to our list
            _objects.Add(
                objectId,
                new ObjectReferenceBufferFieldWrapper
                {
                    ObjectId = objectRef.ObjectId,
                    ObjectType = objectRef.ObjectType,
                    Name = objectRef.Name,
                    FileGroupId = objectRef.FileGroupId,
                    FirstLogicalPageId = objectRef.FirstLogicalPageId
                });
            return objectId;
        }

        private Task<ObjectReferenceResult> GetObjectReferenceHandlerAsync(GetObjectReferenceRequest request)
        {
            if (!_objects.TryGetValue(request.Message.ObjectId, out var objectReference))
            {
                throw new ArgumentException("Object identifier not found.");
            }

            if (request.Message.ObjectType.HasValue &&
                request.Message.ObjectType.Value != objectReference.ObjectType)
            {
                throw new InvalidOperationException("Object type mismatch detected.");
            }

            return Task.FromResult(
                new ObjectReferenceResult(
                    objectReference.ObjectId,
                    objectReference.ObjectType,
                    objectReference.Name,
                    objectReference.FileGroupId,
                    objectReference.FirstLogicalPageId));
        }

        private Task<ObjectId> AddFileGroupAudioHandlerAsync(AddFileGroupAudioRequest request)
        {
            // Locate appropriate filegroup device
            var fileGroupDevice = GetFileGroupDeviceCore(
                request.Message.FileGroupId, request.Message.FileGroupName);

            // Delegate request to file-group device.
            return fileGroupDevice.AddAudioAsync(request.Message);
        }

        private Task<IndexId> AddFileGroupAudioIndexHandlerAsync(AddFileGroupAudioIndexRequest request)
        {
            // Locate appropriate filegroup device
            var fileGroupDevice = GetFileGroupDeviceCore(
                request.Message.FileGroupId, request.Message.FileGroupName);

            // Delegate request to file-group device.
            return fileGroupDevice.AddAudioIndexAsync(request.Message);
        }

        private Task<ObjectId> AddFileGroupTableHandlerAsync(AddFileGroupTableRequest request)
        {
            // Locate appropriate filegroup device
            var fileGroupDevice = GetFileGroupDeviceCore(
                request.Message.FileGroupId, request.Message.FileGroupName);

            // Delegate request to file-group device.
            return fileGroupDevice.AddTableAsync(request.Message);
        }

        private Task<IndexId> AddFileGroupTableIndexHandlerAsync(AddFileGroupTableIndexRequest request)
        {
            // Locate appropriate filegroup device
            var fileGroupDevice = GetFileGroupDeviceCore(
                request.Message.FileGroupId, request.Message.FileGroupName);

            // Delegate request to file-group device.
            return fileGroupDevice.AddTableIndexAsync(request.Message);
        }

        private Task<bool> IssueCheckPointHandlerAsync(IssueCheckPointRequest request)
        {
            var current = _currentCheckPointTask;
            if (current == null)
            {
                // Setup the completion task object then start the operation
                // NOTE: We do NOT await the child operation here
                current = _currentCheckPointTask = ExecuteCheckPoint();
            }

            // Checkpoint is in progress so attach to current operation
            current.ContinueWith(
                t => request.SetResult(true),
                TaskContinuationOptions.OnlyOnRanToCompletion |
                TaskContinuationOptions.ExecuteSynchronously);
            current.ContinueWith(
                t => request.SetFromTask(t),
                TaskContinuationOptions.NotOnRanToCompletion |
                TaskContinuationOptions.ExecuteSynchronously);

            return request.Task;
        }

        private async Task ExecuteCheckPoint()
        {
            Logger.Debug("ExecuteCheckPoint - Begin");
            try
            {
                // Issue begin checkpoint
                Logger.Debug("ExecuteCheckPoint - Writing begin checkpoint entry");
                await GetService<IMasterLogPageDevice>()
                    .WriteEntryAsync(new BeginCheckPointLogEntry())
                    .ConfigureAwait(false);

                Exception exception = null;
                try
                {
                    // Ask data device to dump all unwritten/logged pages to disk
                    // Typically this shouldn't find many pages to write except under
                    //	heavy load.
                    Logger.Debug("ExecuteCheckPoint - Flushing device pages");
                    await CachingBufferDevice
                        .FlushPagesAsync(new FlushCachingDeviceParameters(true))
                        .ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    exception = error;
                }

                // Issue end checkpoint
                Logger.Debug("ExecuteCheckPoint - Writing end checkpoint entry");
                await GetService<IMasterLogPageDevice>()
                    .WriteEntryAsync(new EndCheckPointLogEntry())
                    .ConfigureAwait(false);

                // Discard current check-point task
                _currentCheckPointTask = null;

                // Throw if we have failed
                if (exception != null)
                {
                    Logger.Debug($"ExecuteCheckPoint - Exit with exception [{exception.Message}]");
                    throw exception;
                }
            }
            finally
            {
                Logger.Debug("ExecuteCheckPoint - Exit");
            }
        }

        private IFileGroupDevice GetPrimaryFileGroupDevice()
        {
            return _fileGroupById.Values.FirstOrDefault(item => item.IsPrimaryFileGroup);
        }

        private IFileGroupDevice GetFileGroupDevice(string fileGroupName)
        {
            return GetFileGroupDeviceCore(FileGroupId.Invalid, fileGroupName);
        }

        private IFileGroupDevice GetFileGroupDevice(FileGroupId fileGroupId)
        {
            // ReSharper disable once IntroduceOptionalParameters.Local
            return GetFileGroupDeviceCore(fileGroupId, null);
        }

        private IFileGroupDevice GetFileGroupDeviceCore(FileGroupId fileGroupId, string fileGroupName)
        {
            // Get sane filegroup name if specified
            if (!string.IsNullOrEmpty(fileGroupName))
            {
                fileGroupName = fileGroupName.Trim().ToUpper();
            }

            // Perform lookup on filegroup id and/or filegroup name
            IFileGroupDevice fileGroupDevice;
            if ((fileGroupId.IsValid && _fileGroupById.TryGetValue(fileGroupId, out fileGroupDevice)) ||
                (!string.IsNullOrEmpty(fileGroupName) && _fileGroupByName.TryGetValue(fileGroupName, out fileGroupDevice)))
            {
                return fileGroupDevice;
            }

            // Throw when not found
            throw new FileGroupInvalidException(DeviceId.Zero, fileGroupId, fileGroupName);
        }
        #endregion
    }
}
