using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Autofac;
using Zen.Trunk.Logging;
using Zen.Trunk.Storage.Configuration;
using Zen.Trunk.Storage.Data.Table;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Utils;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// Represents a group of related physical data devices.
    /// </summary>
    /// <remarks>
    /// Logical Page Ids are scoped to the containing file-group hence each
    /// <b>FileGroupDevice</b> has a private Logical/Virtual Page Id mapper.
    /// </remarks>
    public abstract class FileGroupDevice : PageDevice
    {
        #region Private Types
        private class AddDataDeviceRequest : TransactionContextTaskRequest<AddDataDeviceParameters, Tuple<DeviceId, string>>
        {
            #region Public Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="AddDataDeviceRequest" /> class.
            /// </summary>
            /// <param name="deviceParams">The add data device parameters.</param>
            public AddDataDeviceRequest(AddDataDeviceParameters deviceParams)
                : base(deviceParams)
            {
            }
            #endregion
        }

        private class RemoveDataDeviceRequest : TransactionContextTaskRequest<RemoveDataDeviceParameters, bool>
        {
            #region Public Constructors
            public RemoveDataDeviceRequest(RemoveDataDeviceParameters deviceParams)
                : base(deviceParams)
            {
            }
            #endregion
        }

        private class InitDataPageRequest : TransactionContextTaskRequest<InitDataPageParameters, bool>
        {
            #region Public Constructors
            public InitDataPageRequest(InitDataPageParameters initParams)
                : base(initParams)
            {
            }
            #endregion
        }

        private class LoadDataPageRequest : TransactionContextTaskRequest<LoadDataPageParameters, bool>
        {
            #region Public Constructors
            public LoadDataPageRequest(LoadDataPageParameters loadParams)
                : base(loadParams)
            {
            }
            #endregion
        }

        private class CreateDistributionPagesRequest : TransactionContextTaskRequest<bool>
        {
            #region Public Constructors
            public CreateDistributionPagesRequest(
                    DeviceId deviceId, uint startPhysicalId, uint endPhysicalId)
            {
                DeviceId = deviceId;
                StartPhysicalId = startPhysicalId;
                EndPhysicalId = endPhysicalId;
            }
            #endregion

            #region Public Properties
            public DeviceId DeviceId { get; }

            public uint StartPhysicalId { get; }

            public uint EndPhysicalId { get; }
            #endregion
        }

        private class AllocateDataPageRequest : TransactionContextTaskRequest<AllocateDataPageParameters, VirtualPageId>
        {
            #region Public Constructors
            public AllocateDataPageRequest(AllocateDataPageParameters allocParams)
                : base(allocParams)
            {
            }
            #endregion
        }

        private class DeallocateDataPageRequest : TransactionContextTaskRequest<DeallocateDataPageParameters, bool>
        {
            #region Public Constructors
            public DeallocateDataPageRequest(DeallocateDataPageParameters deallocParams)
                : base(deallocParams)
            {
            }
            #endregion
        }

        private class ProcessDistributionPageRequest : TransactionContextTaskRequest<DistributionPage, bool>
        {
            #region Public Constructors
            public ProcessDistributionPageRequest(DistributionPage page)
                : base(page)
            {
            }
            #endregion
        }

        private class ExpandDataDeviceRequest : TransactionContextTaskRequest<ExpandDataDeviceParameters, bool>
        {
            #region Public Constructors
            public ExpandDataDeviceRequest(ExpandDataDeviceParameters parameters)
                : base(parameters)
            {
            }
            #endregion
        }

        private class AddTableRequest : TransactionContextTaskRequest<AddTableParameters, ObjectId>
        {
            #region Public Constructors
            public AddTableRequest(AddTableParameters tableParams)
                : base(tableParams)
            {
            }
            #endregion
        }

        private class AddTableIndexRequest : TransactionContextTaskRequest<AddTableIndexParameters, IndexId>
        {
            #region Public Constructors
            public AddTableIndexRequest(AddTableIndexParameters indexParams)
                : base(indexParams)
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
        #endregion

        #region Private Fields
        private static readonly ILog Logger = LogProvider.For<FileGroupDevice>();

        private DatabaseDevice _owner;

        private DeviceId? _primaryDeviceId;
        private PrimaryDistributionPageDevice _primaryDevice;
        private readonly Dictionary<DeviceId, SecondaryDistributionPageDevice> _devices =
            new Dictionary<DeviceId, SecondaryDistributionPageDevice>();

        private ObjectId _nextObjectId = new ObjectId(1);

        // Logical id mapping
        private ILogicalVirtualManager _logicalVirtual;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FileGroupDevice"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="name">The name.</param>
        protected FileGroupDevice(FileGroupId id, string name)
        {
            FileGroupId = id;
            FileGroupName = name;

            // Setup ports
            var taskInterleave = new ConcurrentExclusiveSchedulerPair();
            AddDataDevicePort = new TransactionContextActionBlock<AddDataDeviceRequest, Tuple<DeviceId, string>>(
                request => AddDataDeviceHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            RemoveDataDevicePort = new TransactionContextActionBlock<RemoveDataDeviceRequest, bool>(
                request => RemoveDataDeviceHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            AllocateDataPagePort = new TransactionContextActionBlock<AllocateDataPageRequest, VirtualPageId>(
                request => AllocateDataPageHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            DeallocateDataPagePort = new TransactionContextActionBlock<DeallocateDataPageRequest, bool>(
                request => DeallocateDataPageHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler
                });
            CreateDistributionPagesPort = new TransactionContextActionBlock<CreateDistributionPagesRequest, bool>(
                request => CreateDistributionPagesHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            ExpandDataDevicePort = new TransactionContextActionBlock<ExpandDataDeviceRequest, bool>(
                request => ExpandDataDeviceHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            InitDataPagePort = new TransactionContextActionBlock<InitDataPageRequest, bool>(
                request => InitDataPageHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler
                });
            LoadDataPagePort = new TransactionContextActionBlock<LoadDataPageRequest, bool>(
                request => LoadDataPageHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler
                });
            ProcessDistributionPagePort = new TransactionContextActionBlock<ProcessDistributionPageRequest, bool>(
                request => ProcessDistributionPageHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler
                });
            CreateObjectReferencePort = new TransactionContextActionBlock<CreateObjectReferenceRequest, ObjectId>(
                request => CreateObjectReferenceHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler
                });
            AddTablePort = new TransactionContextActionBlock<AddTableRequest, ObjectId>(
                request => AddTableHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            AddTableIndexPort = new TransactionContextActionBlock<AddTableIndexRequest, IndexId>(
                request => AddTableIndexHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the primary device id.
        /// </summary>
        /// <value>The primary device id.</value>
        public DeviceId PrimaryDeviceId
        {
            get
            {
                if (!_primaryDeviceId.HasValue)
                {
                    throw new InvalidOperationException("File-group has no primary device.");
                }
                return _primaryDeviceId.Value;
            }
        }

        /// <summary>
        /// Gets the file group id.
        /// </summary>
        /// <value>The file group id.</value>
        public FileGroupId FileGroupId { get; }

        /// <summary>
        /// Gets or sets the name of the file group.
        /// </summary>
        /// <value>The name of the file group.</value>
        public string FileGroupName { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is primary file group.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is primary file group; otherwise, <c>false</c>.
        /// </value>
        public bool IsPrimaryFileGroup => (FileGroupId == FileGroupId.Primary || FileGroupId == FileGroupId.Master);
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the logical virtual manager.
        /// </summary>
        /// <value>
        /// The logical virtual manager.
        /// </value>
        protected ILogicalVirtualManager LogicalVirtualManager
        {
            get
            {
                if (_logicalVirtual == null)
                {
                    _logicalVirtual = GetService<ILogicalVirtualManager>();
                }
                return _logicalVirtual;
            }
        }
        #endregion

        #region Private Properties
        /// <summary>
        /// Gets the add data device port.
        /// </summary>
        /// <value>The add data device port.</value>
        private ITargetBlock<AddDataDeviceRequest> AddDataDevicePort { get; }

        /// <summary>
        /// Gets the remove data device port.
        /// </summary>
        /// <value>The remove data device port.</value>
        private ITargetBlock<RemoveDataDeviceRequest> RemoveDataDevicePort { get; }

        /// <summary>
        /// Gets the init data page port.
        /// </summary>
        /// <value>The init data page port.</value>
        private ITargetBlock<InitDataPageRequest> InitDataPagePort { get; }

        /// <summary>
        /// Gets the load data page port.
        /// </summary>
        /// <value>The load data page port.</value>
        private ITargetBlock<LoadDataPageRequest> LoadDataPagePort { get; }

        /// <summary>
        /// Gets the create distribution pages port.
        /// </summary>
        /// <value>The create distribution pages port.</value>
        private ITargetBlock<CreateDistributionPagesRequest> CreateDistributionPagesPort { get; }

        /// <summary>
        /// Gets the expand data device port.
        /// </summary>
        /// <value>The expand data device port.</value>
        private ITargetBlock<ExpandDataDeviceRequest> ExpandDataDevicePort { get; }

        /// <summary>
        /// Gets the allocate data page port.
        /// </summary>
        /// <value>The allocate data page port.</value>
        private ITargetBlock<AllocateDataPageRequest> AllocateDataPagePort { get; }

        /// <summary>
        /// Gets the deallocate data page port.
        /// </summary>
        /// <value>
        /// The deallocate data page port.
        /// </value>
        private ITargetBlock<DeallocateDataPageRequest> DeallocateDataPagePort { get; }

        /// <summary>
        /// Gets the process distribution page port.
        /// </summary>
        /// <value>The import distribution page.</value>
        private ITargetBlock<ProcessDistributionPageRequest> ProcessDistributionPagePort { get; }

        /// <summary>
        /// Gets the create object reference port.
        /// </summary>
        /// <value>
        /// The create object reference port.
        /// </value>
        private ITargetBlock<CreateObjectReferenceRequest> CreateObjectReferencePort { get; }

        /// <summary>
        /// Gets the add table port.
        /// </summary>
        /// <value>The add table port.</value>
        private ITargetBlock<AddTableRequest> AddTablePort { get; }

        /// <summary>
        /// Gets the add table index port.
        /// </summary>
        /// <value>
        /// The add table index port.
        /// </value>
        private ITargetBlock<AddTableIndexRequest> AddTableIndexPort { get; }

        private DatabaseDevice Owner
        {
            get
            {
                if (_owner == null)
                {
                    _owner = GetService<DatabaseDevice>();
                }
                return _owner;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates the root page for this file-group device.
        /// </summary>
        /// <returns></returns>
        public abstract RootPage CreateRootPage();

        /// <summary>
        /// Adds the data device.
        /// </summary>
        /// <param name="deviceParams">The device parameters.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<Tuple<DeviceId, string>> AddDataDeviceAsync(AddDataDeviceParameters deviceParams)
        {
            var request = new AddDataDeviceRequest(deviceParams);
            if (!AddDataDevicePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Removes the data device.
        /// </summary>
        /// <param name="deviceParams">The device parameters.</param>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> representing the asynchronous operation.
        /// The task result will be <c>true</c> if the resultant file-group is empty;
        /// otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<bool> RemoveDataDeviceAsync(RemoveDataDeviceParameters deviceParams)
        {
            var request = new RemoveDataDeviceRequest(deviceParams);
            if (!RemoveDataDevicePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Initializes the data page.
        /// </summary>
        /// <param name="initParams">The initialize parameters.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task InitDataPageAsync(InitDataPageParameters initParams)
        {
            var request = new InitDataPageRequest(initParams);
            if (!InitDataPagePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Loads the data page.
        /// </summary>
        /// <param name="loadParams">The load parameters.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task LoadDataPageAsync(LoadDataPageParameters loadParams)
        {
            var request = new LoadDataPageRequest(loadParams);
            if (!LoadDataPagePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Creates the distribution pages.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="startPhysicalId">The start physical identifier.</param>
        /// <param name="endPhysicalId">The end physical identifier.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task CreateDistributionPagesAsync(DeviceId deviceId, uint startPhysicalId, uint endPhysicalId)
        {
            var request = new CreateDistributionPagesRequest(deviceId, startPhysicalId, endPhysicalId);
            if (!CreateDistributionPagesPort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Allocates the data page.
        /// </summary>
        /// <param name="allocParams">The alloc parameters.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<VirtualPageId> AllocateDataPageAsync(AllocateDataPageParameters allocParams)
        {
            var request = new AllocateDataPageRequest(allocParams);
            if (!AllocateDataPagePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Deallocates the data page.
        /// </summary>
        /// <param name="deallocParams">The dealloc parameters.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task DeallocateDataPageAsync(DeallocateDataPageParameters deallocParams)
        {
            var request = new DeallocateDataPageRequest(deallocParams);
            if (!DeallocateDataPagePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Process the distribution page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task ProcessDistributionPageAsync(DistributionPage page)
        {
            var request = new ProcessDistributionPageRequest(page);
            if (!ProcessDistributionPagePort.Post(request))
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
        /// Expands the data device.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task ExpandDataDeviceAsync(ExpandDataDeviceParameters parameters)
        {
            var request = new ExpandDataDeviceRequest(parameters);
            if (!ExpandDataDevicePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Adds the table.
        /// </summary>
        /// <param name="tableParams">The table parameters.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<ObjectId> AddTableAsync(AddTableParameters tableParams)
        {
            var request = new AddTableRequest(tableParams);
            if (!AddTablePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Adds the index.
        /// </summary>
        /// <param name="indexParams">The index parameters.</param>
        /// <returns></returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        public Task<IndexId> AddTableIndexAsync(AddTableIndexParameters indexParams)
        {
            var request = new AddTableIndexRequest(indexParams);
            if (!AddTableIndexPort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }

        /// <summary>
        /// Using the previous page as a marker, loads the next linked page or
        /// creates a new linked page if one does not currently exist.
        /// </summary>
        /// <typeparam name="TPageType">The type of the page type.</typeparam>
        /// <param name="previousPage">The previous page.</param>
        /// <returns>
        /// A <see cref="Task"/> that when completed will contain the new page.
        /// </returns>
        /// <remarks>
        /// It is assumed the caller has an exclusive lock on the previous page
        /// although we could relax this to an IX lock and only force an X lock
        /// when the need arises to actually create a new page.
        /// </remarks>
        public async Task<TPageType> LoadOrCreatePageAndLinkAsync<TPageType>(TPageType previousPage)
            where TPageType : LogicalPage, new()
        {
            // If previous page has next logical page identifier then load
            if (previousPage.NextLogicalPageId != LogicalPageId.Zero)
            {
                // Load the next page and return
                var nextPage = new TPageType { LogicalPageId = previousPage.NextLogicalPageId };
                await LoadDataPageAsync(new LoadDataPageParameters(nextPage, false, true))
                    .ConfigureAwait(false);
                return nextPage;
            }

            // Create new next page and link up
            var newPage = new TPageType();
            newPage.PrevLogicalPageId = previousPage.LogicalPageId;
            await InitDataPageAsync(new InitDataPageParameters(newPage, true, true, true))
                .ConfigureAwait(false);
            previousPage.NextLogicalPageId = newPage.LogicalPageId;

            // Force save of both pages
            previousPage.Save();
            newPage.Save();
            return newPage;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Performs a device-specific mount operation.
        /// </summary>
        /// <returns></returns>
        protected override async Task OnOpenAsync()
        {
            if (Logger.IsInfoEnabled())
            {
                Logger.Info("OnOpen : Enter");
            }

            if (_primaryDevice == null)
            {
                throw new InvalidOperationException(
                    "Cannot mount without primary device.");
            }

            // Open/create the primary device
            if (Logger.IsInfoEnabled())
            {
                Logger.Info("OnOpen : Opening primary device");
            }
            await _primaryDevice.OpenAsync(IsCreate).ConfigureAwait(false);

            // Load or create the root page(s)
            if (Logger.IsInfoEnabled())
            {
                Logger.Info("OnOpen : Opening secondary devices");
            }
            if (IsCreate)
            {
                using (var rootPage = (PrimaryFileGroupRootPage)
                    await _primaryDevice.LoadOrCreateRootPageAsync().ConfigureAwait(false))
                {
                    var bufferDevice = GetService<IMultipleBufferDevice>();

                    // TODO: We need to initialise the root page device list with
                    //	information from the current devices in our collection
                    //	We can only do this once we have extended the 
                    //	DistributionPageDevice class to store all the information
                    //	needed by DeviceInfo.
                    rootPage.ReadOnly = false;

                    if (rootPage.FileGroupLock != FileGroupLockType.Exclusive)
                    {
                        await rootPage.SetRootLockAsync(FileGroupLockType.Update).ConfigureAwait(false);
                        await rootPage.SetRootLockAsync(FileGroupLockType.Exclusive).ConfigureAwait(false);
                    }

                    rootPage.AllocatedPages = bufferDevice.GetDeviceInfo(_primaryDevice.DeviceId).PageCount;
                    foreach (var distPageDevice in _devices.Values)
                    {
                        await distPageDevice.OpenAsync(IsCreate).ConfigureAwait(false);
                        rootPage.AllocatedPages += bufferDevice.GetDeviceInfo(distPageDevice.DeviceId).PageCount;
                    }
                }
            }
            else
            {
                var rootPage = (PrimaryFileGroupRootPage)await _primaryDevice
                    .LoadOrCreateRootPageAsync().ConfigureAwait(false);
                while (true)
                {
                    // Process the root page
                    await ProcessPrimaryRootPageAsync(rootPage);

                    // If we have run out of root pages then exit loop
                    if (rootPage.NextLogicalPageId == LogicalPageId.Zero)
                    {
                        break;
                    }

                    // Load the next primary file group root page
                    var nextLogicalPage = new PrimaryFileGroupRootPage { LogicalPageId = rootPage.NextLogicalPageId };
                    await LoadDataPageAsync(new LoadDataPageParameters(nextLogicalPage, false, true))
                        .ConfigureAwait(false);
                    rootPage = nextLogicalPage;
                }
            }

            if (Logger.IsInfoEnabled())
            {
                Logger.Info("OnOpen : Exit");
            }
        }

        /// <summary>
        /// Processes the primary root page during open handling.
        /// </summary>
        /// <param name="rootPage">The root page.</param>
        /// <returns></returns>
        protected virtual async Task ProcessPrimaryRootPageAsync(PrimaryFileGroupRootPage rootPage)
        {
            // TODO: Master database device on primary filegroup must enumerate
            //  list of databases and handle mounting the primary data/log devices
            // This means we need to refactor the contents of this while loop so
            //  it is in a protected virtual function that can be overridden to
            //  deal with this somewhat special case.

            // We need to adjust our "next object identifier" so we skip over existing object ids
            foreach (var objRef in rootPage.Objects)
            {
                if (objRef.ObjectId > _nextObjectId)
                {
                    _nextObjectId = new ObjectId(objRef.ObjectId.Value + 1);
                }
            }

            // Walk the list of devices recorded in the root page
            foreach (var deviceInfo in rootPage.Devices)
            {
                await
                    AddDataDeviceAsync(new AddDataDeviceParameters(deviceInfo.Name, deviceInfo.PathName, deviceInfo.Id))
                        .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Called when closing the device.
        /// </summary>
        /// <returns></returns>
        protected override async Task OnCloseAsync()
        {
            // Close secondary distribution page devices
            foreach (var device in _devices.Values)
            {
                await device.CloseAsync().ConfigureAwait(false);
            }

            // Close primary distribution page device
            if (_primaryDevice != null)
            {
                await _primaryDevice.CloseAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Builds the device lifetime scope.
        /// </summary>
        /// <param name="builder">The builder.</param>
        protected override void BuildDeviceLifetimeScope(ContainerBuilder builder)
        {
            base.BuildDeviceLifetimeScope(builder);

            builder.RegisterType<LogicalVirtualManager>()
                .As<ILogicalVirtualManager>()
                .SingleInstance();
            builder.RegisterInstance(this).As<FileGroupDevice>();
            builder.RegisterType<DatabaseTable>().AsSelf();
            builder.RegisterType<PrimaryDistributionPageDevice>()
                .OnActivated(e => e.Instance.InitialiseDeviceLifetimeScope(LifetimeScope));
            builder.RegisterType<SecondaryDistributionPageDevice>()
                .OnActivated(e => e.Instance.InitialiseDeviceLifetimeScope(LifetimeScope));
        }

        /// <summary>
        /// Releases managed resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _logicalVirtual?.Dispose();
            _logicalVirtual = null;
        }
        #endregion

        #region Private Methods
        private DistributionPageDevice GetDistributionPageDevice(DeviceId deviceId)
        {
            DistributionPageDevice pageDevice;
            if (deviceId == _primaryDevice.DeviceId)
            {
                pageDevice = _primaryDevice;
            }
            else
            {
                pageDevice = _devices[deviceId];
            }
            return pageDevice;
        }

        private List<DeviceId> GetDistributionPageDeviceKeys()
        {
            var deviceIds = new List<DeviceId>();
            deviceIds.Add(_primaryDevice.DeviceId);
            deviceIds.AddRange(_devices.Keys);
            return deviceIds;
        }

        private async Task<Tuple<DeviceId, string>> AddDataDeviceHandlerAsync(AddDataDeviceRequest request)
        {
            // Determine whether this is the first device in a file-group
            var priFileGroupDevice = _devices.Count == 0;

            // Determine file-extension for DBF
            var extn = StorageConstants.SecondaryDeviceFileExtension;
            if (priFileGroupDevice)
            {
                if (IsPrimaryFileGroup)
                {
                    extn = StorageConstants.PrimaryFileGroupPrimaryDeviceFileExtension;
                }
                else
                {
                    extn = StorageConstants.PrimaryDeviceFileExtension;
                }
            }

            // Rewrite filename and extension as required
            string fileName;
            if (IsPrimaryFileGroup && priFileGroupDevice)
            {
                fileName = StorageConstants.PrimaryFileGroupPrimaryDeviceFilename + extn;
            }
            else
            {
                fileName = Path.GetFileNameWithoutExtension(request.Message.PathName) + extn;
            }

            // Determine the folder for the data file
            var directoryName = Path.GetDirectoryName(request.Message.PathName);
            if (string.IsNullOrEmpty(directoryName))
            {
                // If caller only specified filename then get folder from config
                var config = GetService<ITrunkConfigurationManager>();
                directoryName = config.Root.GetInstanceValue(
                    ConfigurationNames.DefaultDataFolder, string.Empty);
                if (string.IsNullOrEmpty(directoryName))
                {
                    throw new ArgumentException(
                        "Unable to determine folder for file-group device.");
                }
            }

            // Derive full pathname
            var fullPathName = Path.Combine(directoryName, fileName);

            // Enforce minimum size (1MB)
            uint allocationPages = 0;
            if (request.Message.IsCreate)
            {
                allocationPages = Math.Max(request.Message.CreatePageCount, 128);
            }

            var pageBufferDevice = GetService<CachingPageBufferDevice>();

            // Add buffer device
            DeviceId deviceId;
            if (priFileGroupDevice && IsPrimaryFileGroup)
            {
                deviceId = await pageBufferDevice
                    .AddDeviceAsync(StorageConstants.PrimaryFileGroupPrimaryDeviceName, fullPathName, DeviceId.Primary, allocationPages)
                    .ConfigureAwait(false);
            }
            else
            {
                deviceId = await pageBufferDevice
                    .AddDeviceAsync(request.Message.Name, fullPathName, request.Message.DeviceId, allocationPages)
                    .ConfigureAwait(false);
            }

            // Create distribution page device wrapper and add to list
            DistributionPageDevice newDevice;
            if (priFileGroupDevice)
            {
                _primaryDeviceId = deviceId;
                _primaryDevice = GetService<PrimaryDistributionPageDevice>(
                    new NamedParameter("deviceId", deviceId));
                newDevice = _primaryDevice;
            }
            else
            {
                var device = GetService<SecondaryDistributionPageDevice>(
                    new NamedParameter("deviceId", deviceId));
                _devices.Add(deviceId, device);
                newDevice = device;
            }
            newDevice.Name = request.Message.Name;
            newDevice.PathName = fullPathName;

            // If file-group is open or opening then open this device too
            if (Owner.DeviceState == MountableDeviceState.Opening ||
                Owner.DeviceState == MountableDeviceState.Open)
            {
                await newDevice.OpenAsync(request.Message.IsCreate).ConfigureAwait(false);
            }

            // Notify caller that add request completed
            return new Tuple<DeviceId, string>(deviceId, fullPathName);
        }

        // ReSharper disable once UnusedParameter.Local
        private Task<bool> RemoveDataDeviceHandlerAsync(RemoveDataDeviceRequest request)
        {
            var tcs = new TaskCompletionSource<bool>();

            // We assume the following
            // 1. All device data has been relocated prior to calling this method

            // TODO: Cannot remove primary device while there are secondary devices

            // TODO: Find appropriate device based on device id or name

            tcs.SetResult(_devices.Count == 0 && _primaryDevice == null);
            return tcs.Task;
        }

        private async Task<bool> InitDataPageHandlerAsync(InitDataPageRequest request)
        {
            if (request.Message.Page == null)
            {
                throw new ArgumentNullException();
            }

            // Setup page file-group id
            request.Message.Page.FileGroupId = FileGroupId;

            // Stage #1: Assign logical id
            var logicalPage = request.Message.Page as LogicalPage;
            if (logicalPage != null && request.Message.GenerateLogicalPageId)
            {
                // Get next logical id from the logical/virtual manager
                logicalPage.LogicalPageId = await LogicalVirtualManager.GetNewLogicalPageIdAsync().ConfigureAwait(false);
            }

            // Stage #2: Assign virtual id
            VirtualPageId pageId;
            if (!request.Message.AssignVirtualPageId)
            {
                pageId = request.Message.Page.VirtualPageId;
            }
            else
            {
                // Post allocation request to file-group device.
                var objectPage = request.Message.Page as ObjectPage;
                pageId = await AllocateDataPageAsync(
                    new AllocateDataPageParameters(
                        logicalPage?.LogicalPageId ?? LogicalPageId.Zero,
                        objectPage?.ObjectId ?? ObjectId.Zero,
                        new ObjectType((byte)request.Message.Page.PageType),
                        request.Message.IsNewObject,
                        request.Message.Page is RootPage))
                    .ConfigureAwait(false);

                // Setup the page virtual id
                request.Message.Page.VirtualPageId = pageId;
            }

            // Stage #3: Add virtual/logical mapping
            // NOTE: We don't need to do this if a logical mapping does not make sense
            //  which typically only means root and distribution pages
            if (logicalPage != null &&
                (request.Message.AssignLogicalPageId || request.Message.GenerateLogicalPageId))
            {
                // Post request to logical/virtual manager
                await LogicalVirtualManager.AddLookupAsync(pageId, logicalPage.LogicalPageId).ConfigureAwait(false);
            }

            // Stage #3: Initialise page object passed in request
            HookupPageSite(request.Message.Page);
            var pageBufferDevice = GetService<CachingPageBufferDevice>();
            request.Message.Page.PreInitInternal();
            using (var scope = new StatefulBufferScope<PageBuffer>(
                await pageBufferDevice.InitPageAsync(pageId).ConfigureAwait(false)))
            {
                // Stage #4: Setup logical id in page buffer as required
                if (logicalPage != null)
                {
                    // Save the logical id in the buffer if we are bound to
                    //	a logical page
                    scope.Buffer.LogicalPageId = logicalPage.LogicalPageId;
                }

                // Stage #5: Attach buffer to page object and conclude initialisation
                request.Message.Page.DataBuffer = scope.Buffer;
                request.Message.Page.OnInitInternal();
                return true;
            }
        }

        private async Task<bool> LoadDataPageHandlerAsync(LoadDataPageRequest request)
        {
            if (request.Message.Page == null)
            {
                throw new ArgumentNullException();
            }

            // Setup page file-group id
            request.Message.Page.FileGroupId = FileGroupId;

            // Stage #1: Determine virtual page id
            var virtualPageId = request.Message.Page.VirtualPageId;
            var logicalPage = request.Message.Page as LogicalPage;
            if (!request.Message.VirtualPageIdValid && request.Message.LogicalPageIdValid)
            {
                // Sanity check: If logical page id is valid then page must be derived from LogicalPage
                if (logicalPage == null)
                {
                    throw new InvalidOperationException("Logical page id can only be read from LogicalPage derived page objects.");
                }

                // Map from logical page to virtual page
                virtualPageId = await LogicalVirtualManager.GetVirtualAsync(logicalPage.LogicalPageId).ConfigureAwait(false);

                // Update the request page and update the virtual page identifier
                request.Message.Page.VirtualPageId = virtualPageId;
            }

            // Stage #2: Load the buffer from the underlying cache
            HookupPageSite(request.Message.Page);
            var pageBufferDevice = GetService<CachingPageBufferDevice>();
            request.Message.Page.PreLoadInternal();
            using (var scope = new StatefulBufferScope<PageBuffer>(
                await pageBufferDevice.LoadPageAsync(virtualPageId).ConfigureAwait(false)))
            {
                // Setup logical page identifier in page buffer as necessary
                if (logicalPage != null)
                {
                    scope.Buffer.LogicalPageId = logicalPage.LogicalPageId;
                }

                // Assign buffer to the page and conclude load process
                request.Message.Page.DataBuffer = scope.Buffer;
                request.Message.Page.PostLoadInternal();
            }

            return true;
        }

        private async Task<bool> ProcessDistributionPageHandlerAsync(ProcessDistributionPageRequest request)
        {
            await request.Message.ExportPageMappingTo(LogicalVirtualManager).ConfigureAwait(false);
            return true;
        }

        private async Task<bool> CreateDistributionPagesHandlerAsync(CreateDistributionPagesRequest request)
        {
            // Get distribution page device
            var pageDevice = GetDistributionPageDevice(request.DeviceId);

            var strideLength = DistributionPage.PageTrackingCount + 1;
            var distPhyId = pageDevice.DistributionPageOffset;
            if (request.StartPhysicalId > distPhyId)
            {
                var distTemp = request.StartPhysicalId - pageDevice.DistributionPageOffset;
                var remainder = distTemp % strideLength;
                if (remainder == 0)
                {
                    distPhyId += (distTemp + strideLength);
                }
                else
                {
                    distPhyId += distTemp + strideLength - remainder;
                }

                // We must be expanding an existing device so we may need to update
                //  the last distribution page to ensure the valid extents match up
                var lastValidDistPagePhyId = distPhyId - strideLength;
                if (lastValidDistPagePhyId > 0)
                {
                    using (var lastDistPage = new DistributionPage())
                    {
                        lastDistPage.VirtualPageId = new VirtualPageId(request.DeviceId, lastValidDistPagePhyId);
                        await lastDistPage.SetDistributionLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false);

                        // Load existing final distribution page
                        var loadPage = new LoadDataPageParameters(lastDistPage, true);
                        await LoadDataPageAsync(loadPage).ConfigureAwait(false);

                        // Update distribution page information
                        await lastDistPage.UpdateValidExtentsAsync(request.EndPhysicalId).ConfigureAwait(false);
                    }
                }
            }

            for (; distPhyId <= request.EndPhysicalId; distPhyId += strideLength)
            {
                // Create distribution page
                using (var page = new DistributionPage())
                {
                    page.VirtualPageId = new VirtualPageId(request.DeviceId, distPhyId);
                    await page.SetDistributionLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false);

                    // Add page to the device
                    var initPage = new InitDataPageParameters(page);
                    await InitDataPageAsync(initPage).ConfigureAwait(false);

                    // Initialise distribution page information
                    await page.UpdateValidExtentsAsync(request.EndPhysicalId).ConfigureAwait(false);
                }
            }

            // At this point our task is complete
            return true;
        }

        private async Task<bool> ExpandDataDeviceHandlerAsync(ExpandDataDeviceRequest request)
        {
            RootPage rootPage;

            // Do underlying device expansion
            if (request.Message.IsDeviceIdValid)
            {
                // Load the root page and obtain update lock before we start
                var pageDevice = GetDistributionPageDevice(request.Message.DeviceId);
                rootPage = await pageDevice.LoadOrCreateRootPageAsync().ConfigureAwait(false);
                await rootPage.SetRootLockAsync(FileGroupLockType.Shared).ConfigureAwait(false);

                await ExpandDeviceCoreAsync(request.Message.DeviceId, rootPage, request.Message.PageCount).ConfigureAwait(false);
            }
            else
            {
                // TODO: Load root page for each device in our list
                // TODO: Sort pages into "allocated pages" ascending
                //	excluding all non-expandable devices
                var deviceIds = GetDistributionPageDeviceKeys();
                var rootPages =
                    new Dictionary<DeviceId, RootPage>();
                foreach (var deviceId in deviceIds)
                {
                    // Get distribution page device
                    var pageDevice =
                        GetDistributionPageDevice(deviceId);
                    rootPage = await pageDevice
                        .LoadOrCreateRootPageAsync()
                        .ConfigureAwait(false);
                    if (rootPage.IsExpandable)
                    {
                        await rootPage.SetRootLockAsync(FileGroupLockType.Shared).ConfigureAwait(false);
                        rootPages.Add(deviceId, rootPage);
                    }
                    else
                    {
                        rootPage.Dispose();
                    }
                }

                // Walk sorted list of devices
                var hasExpanded = false;
                var failedDueToLock = false;
                var failedDueToFull = false;
                foreach (var pair in rootPages.OrderBy(item => item.Value.AllocatedPages))
                {
                    // Hook root page
                    try
                    {
                        await ExpandDeviceCoreAsync(pair.Key, pair.Value, request.Message.PageCount).ConfigureAwait(false);
                        hasExpanded = true;
                        break;
                    }
                    catch (DeviceFullException)
                    {
                        failedDueToFull = true;
                    }
                    catch (TimeoutException)
                    {
                        failedDueToLock = true;
                    }
                    catch
                    {
                        // TODO: Log this exception
                    }
                }

                if (!hasExpanded)
                {
                    if (failedDueToLock)
                    {
                        throw new TimeoutException("Failed to expand file-group device due to lock timeout.");
                    }
                    else if (failedDueToFull)
                    {
                        throw new FileGroupFullException(DeviceId.Zero, FileGroupId, FileGroupName, "Failed to expand file-group device; device is full.");
                    }
                    else
                    {
                        throw new InvalidOperationException("Failed to expand file-group device due to unknown issue.");
                    }
                }
            }

            return true;
        }

        private async Task<VirtualPageId> AllocateDataPageHandlerAsync(AllocateDataPageRequest request)
        {
            List<DeviceId> deviceIds;
            if (request.Message.OnlyUsePrimaryDevice)
            {
                deviceIds = new List<DeviceId>();
                deviceIds.Add(PrimaryDeviceId);
            }
            else
            {
                // Get device keys and perform randomisation
                deviceIds = GetDistributionPageDeviceKeys();
                deviceIds.Randomize();
            }

            // Walk each device and attempt to allocate page
            foreach (var deviceId in deviceIds)
            {
                // Get distribution page device
                var pageDevice = GetDistributionPageDevice(deviceId);
                try
                {
                    // Attempt to allocate (may fail)
                    return await pageDevice.AllocateDataPageAsync(request.Message).ConfigureAwait(false);
                }
                catch
                {
                    // Failed either because full or busy
                    //	no need to handle or log unless we want to keep
                    //	statistics on dist page contention
                }
            }

            // File group must be full if we reach this point!
            throw new FileGroupFullException(DeviceId.Zero, FileGroupId, null);
        }

        private async Task<bool> DeallocateDataPageHandlerAsync(DeallocateDataPageRequest request)
        {
            // Determine the virtual page identifier
            var virtualPageId = request.Message.VirtualPageId;
            if (virtualPageId == VirtualPageId.Zero)
            {
                // Ask the LVM to translate logical page id
                virtualPageId = await _logicalVirtual
                    .GetVirtualAsync(request.Message.LogicalPageId)
                    .ConfigureAwait(false);
            }

            // Load distribution device for this virtual page
            var distDevice = GetDistributionPageDevice(virtualPageId.DeviceId);

            // Delegate deallocation request via device
            await distDevice
                .DeallocateDataPageAsync(
                    new DeallocateDataPageParameters(virtualPageId, LogicalPageId.Zero))
                .ConfigureAwait(false);
            return true;
        }

        private async Task<ObjectId> CreateObjectReferenceHandlerAsync(CreateObjectReferenceRequest request)
        {
            // Determine object id
            var objectId = _nextObjectId;
            _nextObjectId = new ObjectId(_nextObjectId.Value + 1);

            // Build object reference information.
            var objectRef =
                new ObjectRefInfo
                {
                    Name = request.Message.Name,
                    ObjectType = request.Message.ObjectType,
                    FileGroupId = FileGroupId,
                    FirstLogicalPageId = await request.Message.FirstPageFunc(objectId).ConfigureAwait(false)
                };

            // Load primary file-group root page
            var rootPage = (PrimaryFileGroupRootPage)
                await _primaryDevice.LoadOrCreateRootPageAsync().ConfigureAwait(false);

            // Attempt to write reference information into a root page
            await rootPage.SetRootLockAsync(FileGroupLockType.Exclusive).ConfigureAwait(false);
            while (true)
            {
                // Mark root page as writable and attempt to add object reference
                rootPage.ReadOnly = false;
                try
                {
                    rootPage.Objects.Add(objectRef);

                    // If we get this far then page has space for object reference
                    break;
                }
                catch (PageException)
                {
                }

                // Failed to add to existing root page; prepare to load/create new rootpage
                var nextRootPage = await LoadOrCreatePageAndLinkAsync(rootPage).ConfigureAwait(false);

                // Lock page and try again
                await nextRootPage.SetRootLockAsync(FileGroupLockType.Exclusive).ConfigureAwait(false);
                rootPage = nextRootPage;
            }

            return objectId;
        }

        private async Task<ObjectId> AddTableHandlerAsync(AddTableRequest request)
        {
            // Determine the object identifier for the table
            return await CreateObjectReferenceAsync(
                new CreateObjectReferenceParameters(request.Message.TableName, ObjectType.Table,
                    async (objectId) =>
                    {
                        // Create database table helper and setup object
                        var table = GetService<DatabaseTable>();
                        table.FileGroupId = FileGroupId;
                        table.ObjectId = objectId;
                        table.IsNewTable = true;

                        // Create columns
                        table.BeginColumnUpdate();
                        foreach (var column in request.Message.Columns)
                        {
                            table.AddColumn(column, -1);
                        }

                        // Commit table changes
                        await table
                            .EndColumnUpdate()
                            .ConfigureAwait(false);

                        // Return the first logical page id of the schema
                        return table.SchemaFirstLogicalPageId;
                    })).ConfigureAwait(false);
        }

        private Task<IndexId> AddTableIndexHandlerAsync(AddTableIndexRequest request)
        {
            var indexId = IndexId.Zero;

            var table = GetService<DatabaseTable>();
            table.FileGroupId = FileGroupId;
            table.ObjectId = request.Message.ObjectId;
            table.IsNewTable = false;

            //table.
            //table.AddIndex

            return Task.FromResult(indexId);
        }

        private async Task ExpandDeviceCoreAsync(DeviceId deviceId, RootPage rootPage, uint growthPages)
        {
            // Check device can be expanded
            if (!rootPage.IsExpandable)
            {
                // This device is full...
                throw new DeviceFullException(deviceId);
            }

            // Check for automatic growth calculation
            if (growthPages == 0)
            {
                // Determine amount to increase storage by
                if (!rootPage.IsExpandableByPercent)
                {
                    growthPages = rootPage.GrowthPages;
                    if (growthPages == 0)
                    {
                        // Grow by 1Mb by default
                        //	however this should never be reached
                        growthPages = 128;
                    }
                }
                else
                {
                    growthPages = (uint)
                        (rootPage.AllocatedPages *
                        rootPage.GrowthPercent /
                        100.0);
                }
            }

            // Limit growth amount by maximum page allocation (if defined)
            if (rootPage.MaximumPages > 0 &&
                (rootPage.AllocatedPages + growthPages) > rootPage.MaximumPages)
            {
                growthPages = rootPage.MaximumPages - rootPage.AllocatedPages;
            }

            // Final sanity check
            if (growthPages == 0)
            {
                throw new DeviceFullException(deviceId);
            }

            uint oldPageCount;
            uint newPageCount;

            // Place root page into update mode
            await rootPage.SetRootLockAsync(FileGroupLockType.Update).ConfigureAwait(false);
            try
            {
                // Transition root page into exclusive mode
                await rootPage.SetRootLockAsync(FileGroupLockType.Exclusive).ConfigureAwait(false);

                // Delegate the request to the underlying device
                var bufferDevice = GetService<IMultipleBufferDevice>();
                oldPageCount = bufferDevice.GetDeviceInfo(deviceId).PageCount;
                newPageCount = oldPageCount + growthPages;
                bufferDevice.ResizeDevice(deviceId, growthPages);
            }
            catch
            {
                // Assume expand failed and revert lock
                //	don't know if I really have to do this now
                await rootPage.SetRootLockAsync(FileGroupLockType.Shared).ConfigureAwait(false);
                throw;
            }

            // Create distribution pages as necessary
            if (newPageCount > oldPageCount)
            {
                await CreateDistributionPagesAsync(deviceId, oldPageCount, newPageCount - 1).ConfigureAwait(false);
            }

            // Finally update the root page.
            rootPage.ReadOnly = false;
            rootPage.AllocatedPages = newPageCount;
            rootPage.Save();
        }
        #endregion
    }
}
