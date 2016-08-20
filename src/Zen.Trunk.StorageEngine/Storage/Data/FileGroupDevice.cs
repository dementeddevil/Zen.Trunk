namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;
	using Zen.Trunk.Storage;
	using Zen.Trunk.Storage.Data.Table;
	using Zen.Trunk.Storage.Locking;
	using Zen.Trunk.Utils;

	/// <summary>
	/// Represents a group of related physical devices.
	/// </summary>
	/// <remarks>
	/// Logical Page Ids are scoped to the containing file-group hence each
	/// <b>FileGroupDevice</b> has a private Logical/Virtual Page Id mapper.
	/// </remarks>
	public abstract class FileGroupDevice : PageDevice
	{
		#region Private Types
		private class AddDataDeviceRequest : TransactionContextTaskRequest<AddDataDeviceParameters, ushort>
		{
			#region Public Constructors
			/// <summary>
			/// Initializes a new instance of the <see cref="AddDevice" /> class.
			/// </summary>
			/// <param name="name">The name.</param>
			/// <param name="pathName">Name of the path.</param>
			/// <param name="createPageCount">The create page count.</param>
			/// <param name="deviceId">The device id.</param>
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
			public CreateDistributionPagesRequest(
				ushort deviceId, uint startPhysicalId, uint endPhysicalId)
			{
				DeviceId = deviceId;
				StartPhysicalId = startPhysicalId;
				EndPhysicalId = endPhysicalId;
			}

			public ushort DeviceId
			{
				get;
				private set;
			}

			public uint StartPhysicalId
			{
				get;
				private set;
			}

			public uint EndPhysicalId
			{
				get;
				private set;
			}
		}

		private class AllocateDataPageRequest : TransactionContextTaskRequest<AllocateDataPageParameters, VirtualPageId>
		{
			public AllocateDataPageRequest(AllocateDataPageParameters allocParams)
				: base(allocParams)
			{
			}
		}

		private class ImportDistributionPageRequest : TransactionContextTaskRequest<bool>
		{
			public ImportDistributionPageRequest(DistributionPage page)
			{
				Page = page;
			}

			public DistributionPage Page
			{
				get;
				private set;
			}
		}

		private class ExpandDataDeviceRequest : TransactionContextTaskRequest<bool>
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="ExpandDeviceRequest" /> class.
			/// </summary>
			/// <param name="pageCount">The page count.</param>
			/// <param name="deviceId">The device id.</param>
			public ExpandDataDeviceRequest(uint pageCount, ushort deviceId = 0)
			{
				DeviceId = deviceId;
				PageCount = pageCount;
			}

			/// <summary>
			/// Gets or sets the device id.
			/// </summary>
			/// <value>The device id.</value>
			public ushort DeviceId
			{
				get;
				private set;
			}

			/// <summary>
			/// Gets or sets a value indicating whether the device id is valid.
			/// </summary>
			/// <value>
			/// 	<c>true</c> if the device id is valid; otherwise, <c>false</c>.
			/// </value>
			public bool IsDeviceIdValid => (DeviceId != 0);

		    /// <summary>
			/// Gets or sets an integer that will be added to the existing page 
			/// count of the target device to determine the new page capacity.
			/// </summary>
			/// <value>The page count.</value>
			public uint PageCount
			{
				get;
				private set;
			}
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

		private class AddTableIndexRequest : TransactionContextTaskRequest<AddTableIndexParameters, uint>
		{
			#region Public Constructors
			public AddTableIndexRequest(AddTableIndexParameters indexParams)
				: base(indexParams)
			{
			}
			#endregion
		}
		#endregion

		#region Private Fields
		private DatabaseDevice _owner;

		private ITargetBlock<AddDataDeviceRequest> _addDataDevicePort;
		private ITargetBlock<RemoveDataDeviceRequest> _removeDataDevicePort;
		private ITargetBlock<InitDataPageRequest> _initDataPagePort;
		private ITargetBlock<LoadDataPageRequest> _loadDataPagePort;
		private ITargetBlock<CreateDistributionPagesRequest> _createDistributionPagesPort;
		private ITargetBlock<ExpandDataDeviceRequest> _expandDataDevicePort;
		private ITargetBlock<AllocateDataPageRequest> _allocateDataPagePort;
		private ITargetBlock<ImportDistributionPageRequest> _importDistributionPagePort;
		private ITargetBlock<AddTableRequest> _addTablePort;
		private ITargetBlock<AddTableIndexRequest> _addTableIndexPort;
		private ConcurrentExclusiveSchedulerPair _taskInterleave;

		private ushort? _primaryDeviceId;
		private PrimaryDistributionPageDevice _primaryDevice;
		private readonly Dictionary<ushort, SecondaryDistributionPageDevice> _devices =
			new Dictionary<ushort, SecondaryDistributionPageDevice>();
		private readonly HashSet<uint> _assignedObjectIds = new HashSet<uint>();

		// Logical id mapping
		private ILogicalVirtualManager _logicalVirtual;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="FileGroupDevice"/> class.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="id">The id.</param>
		/// <param name="name">The name.</param>
		public FileGroupDevice(IServiceProvider parentServiceProvider, FileGroupId id, string name)
			: base(parentServiceProvider)
		{
			FileGroupId = id;
			FileGroupName = name;

			Initialise();
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the primary device id.
		/// </summary>
		/// <value>The primary device id.</value>
		public ushort PrimaryDeviceId
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
		public FileGroupId FileGroupId
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the name of the file group.
		/// </summary>
		/// <value>The name of the file group.</value>
		public string FileGroupName
		{
			get;
			private set;
		}

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
		/// Gets the name of the tracer.
		/// </summary>
		/// <value>The name of the tracer.</value>
		protected override string TracerName => GetType().Name + ":" + FileGroupId + ":" + FileGroupName;

	    #endregion

		#region Private Properties
		/// <summary>
		/// Gets the add data device port.
		/// </summary>
		/// <value>The add data device port.</value>
		private ITargetBlock<AddDataDeviceRequest> AddDataDevicePort => _addDataDevicePort;

	    /// <summary>
		/// Gets the remove data device port.
		/// </summary>
		/// <value>The remove data device port.</value>
		private ITargetBlock<RemoveDataDeviceRequest> RemoveDataDevicePort => _removeDataDevicePort;

	    /// <summary>
		/// Gets the init data page port.
		/// </summary>
		/// <value>The init data page port.</value>
		private ITargetBlock<InitDataPageRequest> InitDataPagePort => _initDataPagePort;

	    /// <summary>
		/// Gets the load data page port.
		/// </summary>
		/// <value>The load data page port.</value>
		private ITargetBlock<LoadDataPageRequest> LoadDataPagePort => _loadDataPagePort;

	    /// <summary>
		/// Gets the create distribution pages port.
		/// </summary>
		/// <value>The create distribution pages port.</value>
		private ITargetBlock<CreateDistributionPagesRequest> CreateDistributionPagesPort => _createDistributionPagesPort;

	    /// <summary>
		/// Gets the expand data device port.
		/// </summary>
		/// <value>The expand data device port.</value>
		private ITargetBlock<ExpandDataDeviceRequest> ExpandDataDevicePort => _expandDataDevicePort;

	    /// <summary>
		/// Gets the allocate data page port.
		/// </summary>
		/// <value>The allocate data page port.</value>
		private ITargetBlock<AllocateDataPageRequest> AllocateDataPagePort => _allocateDataPagePort;

	    /// <summary>
		/// Gets the import distribution page port.
		/// </summary>
		/// <value>The import distribution page.</value>
		private ITargetBlock<ImportDistributionPageRequest> ImportDistributionPagePort => _importDistributionPagePort;

	    /// <summary>
		/// Gets the add table port.
		/// </summary>
		/// <value>The add table port.</value>
		private ITargetBlock<AddTableRequest> AddTablePort => _addTablePort;

	    /// <summary>
		/// Gets the add table index port.
		/// </summary>
		/// <value>
		/// The add table index port.
		/// </value>
		private ITargetBlock<AddTableIndexRequest> AddTableIndexPort => _addTableIndexPort;

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
		public virtual RootPage CreateRootPage(bool isPrimaryFile)
		{
			RootPage rootPage;
			if (isPrimaryFile)
			{
				rootPage = new PrimaryFileGroupRootPage();
			}
			else
			{
				rootPage = new SecondaryFileGroupRootPage();
			}

			rootPage.FileGroupId = FileGroupId;
			return rootPage;
		}

		public Task<ushort> AddDataDevice(AddDataDeviceParameters deviceParams)
		{
			var request = new AddDataDeviceRequest(deviceParams);
			if (!AddDataDevicePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task RemoveDataDevice(RemoveDataDeviceParameters deviceParams)
		{
			var request = new RemoveDataDeviceRequest(deviceParams);
			if (!RemoveDataDevicePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task InitDataPage(InitDataPageParameters initParams)
		{
			var request = new InitDataPageRequest(initParams);
			if (!InitDataPagePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task LoadDataPage(LoadDataPageParameters loadParams)
		{
			var request = new LoadDataPageRequest(loadParams);
			if (!LoadDataPagePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task CreateDistributionPages(ushort deviceId, uint startPhysicalId, uint endPhysicalId)
		{
			var request = new CreateDistributionPagesRequest(deviceId, startPhysicalId, endPhysicalId);
			if (!CreateDistributionPagesPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task<VirtualPageId> AllocateDataPage(AllocateDataPageParameters allocParams)
		{
			var request = new AllocateDataPageRequest(allocParams);
			if (!AllocateDataPagePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task ImportDistributionPage(DistributionPage page)
		{
			var request = new ImportDistributionPageRequest(page);
			if (!ImportDistributionPagePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task ExpandDataDevice(uint pageCount, ushort deviceId = 0)
		{
			var request = new ExpandDataDeviceRequest(pageCount, deviceId);
			if (!ExpandDataDevicePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task<ObjectId> AddTable(AddTableParameters tableParams)
		{
			var request = new AddTableRequest(tableParams);
			if (!AddTablePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task<uint> AddIndex(AddTableIndexParameters indexParams)
		{
			var request = new AddTableIndexRequest(indexParams);
			if (!AddTableIndexPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Performs a device-specific mount operation.
		/// </summary>
		/// <returns></returns>
		protected override async Task OnOpen()
		{
			Tracer.WriteInfoLine("OnOpen: Enter");

			if (_primaryDevice == null)
			{
				throw new InvalidOperationException(
					"Cannot mount without primary device.");
			}

			// Open/create the primary device
			Tracer.WriteInfoLine("OnOpen: Opening primary device");
			await _primaryDevice.OpenAsync(IsCreate);

			// Load or create the root page
			Tracer.WriteInfoLine("OnOpen: Opening secondary devices");
			using (var rootPage = (PrimaryFileGroupRootPage)
				await _primaryDevice.LoadOrCreateRootPage().ConfigureAwait(false))
			{
				var bufferDevice = GetService<IMultipleBufferDevice>();
				if (IsCreate)
				{
					// TODO: We need to initialise the root page device list with
					//	information from the current devices in our collection
					//	We can only do this once we have extended the 
					//	DistributionPageDevice class to store all the information
					//	needed by DeviceInfo.
					rootPage.ReadOnly = false;

					if (rootPage.RootLock != RootLockType.Exclusive)
					{
						rootPage.RootLock = RootLockType.Update;
						rootPage.RootLock = RootLockType.Exclusive;
					}

					rootPage.AllocatedPages = bufferDevice.GetDeviceInfo(_primaryDevice.DeviceId).PageCount;
					foreach (var distPageDevice in _devices.Values)
					{
						await distPageDevice.OpenAsync(IsCreate);
						rootPage.AllocatedPages += bufferDevice.GetDeviceInfo(distPageDevice.DeviceId).PageCount;
					}
				}
				else
				{
					// Walk the list of devices recorded in the root page
					foreach (var deviceInfo in rootPage.Devices)
					{
						await AddDataDevice(new AddDataDeviceParameters(deviceInfo.Name, deviceInfo.PathName));
					}
				}
			}

			Tracer.WriteVerboseLine("OnOpen: Leave");
		}

		/// <summary>
		/// Called when closing the device.
		/// </summary>
		/// <returns></returns>
		protected override async Task OnClose()
		{
			// Close secondary distribution page devices
			foreach (DistributionPageDevice device in _devices.Values)
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
		/// Overridden. Gets the service corresponding to the specified type.
		/// </summary>
		/// <param name="serviceType"></param>
		/// <returns></returns>
		protected override object GetService(Type serviceType)
		{
			// Trap requests for BufferDevice objects
			if (serviceType == typeof(FileGroupDevice))
			{
				return this;
			}
			if (serviceType == typeof(ILogicalVirtualManager))
			{
				return _logicalVirtual;
			}
			return base.GetService(serviceType);
		}

		/// <summary>
		/// Releases managed resources
		/// </summary>
		protected override void DisposeManagedObjects()
		{
			if (_logicalVirtual != null)
			{
				_logicalVirtual.Dispose();
				_logicalVirtual = null;
			}
		}
		#endregion

		#region Private Methods
		private void Initialise()
		{
			// Create logical/virtual lookup manager
            // TODO: Get this from IoC
			_logicalVirtual = new LogicalVirtualManager();

			// Setup ports
			_taskInterleave = new ConcurrentExclusiveSchedulerPair();
			_addDataDevicePort = new TransactionContextActionBlock<AddDataDeviceRequest, ushort>(
				(request) => AddDataDeviceHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ExclusiveScheduler
				});
			_removeDataDevicePort = new TransactionContextActionBlock<RemoveDataDeviceRequest, bool>(
				(request) => RemoveDataDeviceHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ExclusiveScheduler
				});
			_allocateDataPagePort = new TransactionContextActionBlock<AllocateDataPageRequest, VirtualPageId>(
				(request) => AllocateDataPageHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ExclusiveScheduler
				});
			_createDistributionPagesPort = new TransactionContextActionBlock<CreateDistributionPagesRequest, bool>(
				(request) => CreateDistributionPagesHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ExclusiveScheduler
				});
			_expandDataDevicePort = new TransactionContextActionBlock<ExpandDataDeviceRequest, bool>(
				(request) => ExpandDataDeviceHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ExclusiveScheduler
				});
			_initDataPagePort = new TransactionContextActionBlock<InitDataPageRequest, bool>(
				(request) => InitDataPageHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ConcurrentScheduler
				});
			_loadDataPagePort = new TransactionContextActionBlock<LoadDataPageRequest, bool>(
				(request) => LoadDataPageHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ConcurrentScheduler
				});
			_importDistributionPagePort = new TransactionContextActionBlock<ImportDistributionPageRequest, bool>(
				(request) => ImportDistributionPageHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ConcurrentScheduler
				});
			_addTablePort = new TransactionContextActionBlock<AddTableRequest, ObjectId>(
				(request) => AddTableHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ExclusiveScheduler
				});
			_addTableIndexPort = new TransactionContextActionBlock<AddTableIndexRequest, uint>(
				(request) => AddTableIndexHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ExclusiveScheduler
				});
		}

		private DistributionPageDevice GetDistributionPageDevice(ushort deviceId)
		{
			DistributionPageDevice pageDevice = null;
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

		private List<ushort> GetDistributionPageDeviceKeys()
		{
			var deviceIds = new List<ushort>();
			deviceIds.Add(_primaryDevice.DeviceId);
			deviceIds.AddRange(_devices.Keys);
			return deviceIds;
		}

		private async Task<ushort> AddDataDeviceHandler(AddDataDeviceRequest request)
		{
			Tracer.WriteVerboseLine("AddDataDevice");

			// TODO: Update this method to pass the information to the
			//	distribution device.
			// When the dist device is opened

			// Determine whether this is the first device in a file-group
			var priFileGroupDevice = false;
			if (_devices.Count == 0)
			{
				priFileGroupDevice = true;
			}

			// Determine file-extension for DBF
			var extn = ".sdf";
			if (priFileGroupDevice)
			{
				if (IsPrimaryFileGroup)
				{
					extn = ".mddf";
				}
				else
				{
					extn = ".mfdf";
				}
			}

			// Rewrite filename and extension as required
			string fileName = null;
			if (IsPrimaryFileGroup && priFileGroupDevice)
			{
				fileName = "master" + extn;
			}
			else
			{
				fileName = Path.GetFileNameWithoutExtension(request.Message.PathName) + extn;
			}
			var fullPathName = Path.Combine(Path.Combine(
				Path.GetPathRoot(request.Message.PathName),
				Path.GetDirectoryName(request.Message.PathName)), fileName);

			// Enforce minimum size
			uint allocationPages = 0;
			if (request.Message.IsCreate)
			{
				allocationPages = Math.Max(request.Message.CreatePageCount, 128);
			}

			var pageBufferDevice = GetService<CachingPageBufferDevice>();

			// Add buffer device
			ushort deviceId;
			if (priFileGroupDevice && IsPrimaryFileGroup)
			{
				deviceId = await pageBufferDevice.AddDeviceAsync("MASTER", fullPathName, 1, allocationPages).ConfigureAwait(false);
			}
			else
			{
				deviceId = await pageBufferDevice.AddDeviceAsync(request.Message.Name, fullPathName, request.Message.DeviceId, allocationPages).ConfigureAwait(false);
			}
			Tracer.WriteVerboseLine("AddDevice -> device id = {0}", deviceId);

			// Create distribution page device wrapper and add to list
			DistributionPageDevice newDevice = null;
			if (priFileGroupDevice)
			{
				_primaryDeviceId = deviceId;
				_primaryDevice = new PrimaryDistributionPageDevice(this, deviceId);
				newDevice = _primaryDevice;
			}
			else
			{
				var device = new SecondaryDistributionPageDevice(this, deviceId);
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
			return deviceId;
		}

		private Task<bool> RemoveDataDeviceHandler(RemoveDataDeviceRequest request)
		{
			var tcs = new TaskCompletionSource<bool>();
			tcs.SetResult(true);
			return tcs.Task;
		}

		private async Task<bool> InitDataPageHandler(InitDataPageRequest request)
		{
			if (!typeof(DataPage).IsAssignableFrom(request.Message.Page.GetType()))
			{
				throw new Exception("Page object must be derived from DataPage.");
			}

			// Setup page file-group id
			request.Message.Page.FileGroupId = FileGroupId;

			// Stage #1: Assign logical id
			var logicalPage = request.Message.Page as LogicalPage;
			if (logicalPage != null && request.Message.AssignAutomaticLogicalId)
			{
				// Get next logical id from the logical/virtual manager
				logicalPage.LogicalId = await _logicalVirtual.GetNewLogicalAsync().ConfigureAwait(false);
			}

			// Stage #2: Assign virtual id
			VirtualPageId pageId;
			if (!request.Message.AssignVirtualId)
			{
				pageId = new VirtualPageId(request.Message.Page.VirtualId);
			}
			else
			{
				// Post allocation request to file-group device.
				var objectPage = request.Message.Page as ObjectPage;
				pageId = await AllocateDataPage(
                    new AllocateDataPageParameters(
					    (logicalPage != null) ? logicalPage.LogicalId : LogicalPageId.Zero,
					    (objectPage != null) ? objectPage.ObjectId : ObjectId.Zero,
					    new ObjectType((byte)request.Message.Page.PageType),
					    request.Message.IsNewObject))
					.ConfigureAwait(false);

				// Setup the page virtual id
				request.Message.Page.VirtualId = pageId.Value;
			}

			// Stage #3: Add virtual/logical mapping
			if (logicalPage != null &&
				(request.Message.AssignLogicalId || request.Message.AssignAutomaticLogicalId))
			{
				// Post request to logical/virtual manager
				var logicalId = await _logicalVirtual.AddLookupAsync(pageId, logicalPage.LogicalId).ConfigureAwait(false);

				// Update page with new logical id as necessary
				if (!request.Message.AssignAutomaticLogicalId)
				{
					logicalPage.LogicalId = logicalId;
				}
			}

			// Stage #3: Initialise page object passed in request
			var pageBufferDevice =
				GetService<CachingPageBufferDevice>();
			request.Message.Page.PreInitInternal();
			using (var scope =
				new StatefulBufferScope<PageBuffer>(
					await pageBufferDevice.InitPageAsync(pageId)
						.ConfigureAwait(false)))
			{
				// Stage #4: Setup logical id in page buffer as required
				if (logicalPage != null)
				{
					// Save the logical id in the buffer if we are bound to
					//	a logical page
					scope.Buffer.LogicalId = logicalPage.LogicalId;
				}

				// Stage #5: Attach buffer to page object and conclude initialisation
				request.Message.Page.DataBuffer = scope.Buffer;
				request.Message.Page.OnInitInternal();
				return true;
			}
		}

		private async Task<bool> LoadDataPageHandler(LoadDataPageRequest request)
		{
			// Map filegroup Id
			if (!typeof(DataPage).IsAssignableFrom(request.Message.Page.GetType()))
			{
				throw new Exception("Page object must be derived from DataPage.");
			}

			request.Message.Page.FileGroupId = FileGroupId;

			// Setup virtual and logical defaults
			var pageId = new VirtualPageId(request.Message.Page.VirtualId);

			// Stage #1: Determine virtual id if we only have logical id.
			var logicalPage = request.Message.Page as LogicalPage;
			if (!request.Message.VirtualPageIdValid && request.Message.LogicalPageIdValid)
			{
				if (logicalPage == null)
				{
					throw new InvalidOperationException("Logical id can only be read from LogicalPage derived page objects.");
				}

				// Map from logical page to virtual page
				pageId = await _logicalVirtual.GetVirtualAsync(logicalPage.LogicalId).ConfigureAwait(false);
				request.Message.Page.VirtualId = pageId.Value;
			}

			// Stage #2: Load the buffer from the underlying cache
			var pageBufferDevice =
				GetService<CachingPageBufferDevice>();
			request.Message.Page.PreLoadInternal();
			using (var scope =
				new StatefulBufferScope<PageBuffer>(
					await pageBufferDevice.LoadPageAsync(pageId)
						.ConfigureAwait(false)))
			{
				// Setup logical id in page buffer as required
				if (logicalPage != null)
				{
					// Assign the buffer logical Id then assign buffer to page
					scope.Buffer.LogicalId = logicalPage.LogicalId;
				}

				// Assign buffer to the page and conclude load process
				request.Message.Page.DataBuffer = scope.Buffer;
				request.Message.Page.PostLoadInternal();
			}

			return true;
		}

		private async Task<bool> ImportDistributionPageHandler(ImportDistributionPageRequest request)
		{
			await request.Page.Import(_logicalVirtual);
			return true;
		}

		private async Task<bool> CreateDistributionPagesHandler(CreateDistributionPagesRequest request)
		{
			// Get distribution page device
			var pageDevice =
				GetDistributionPageDevice(request.DeviceId);

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
			}

			for (; distPhyId <= request.EndPhysicalId; distPhyId += strideLength)
			{
				// Create distribution page
				var pageId = new VirtualPageId(request.DeviceId, distPhyId);
				using (var page = new DistributionPage())
				{
					page.VirtualId = pageId.Value;
					page.DistributionLock = ObjectLockType.Exclusive;

					// Add page to the device
					var initPage = new InitDataPageParameters(page);
					await InitDataPage(initPage).ConfigureAwait(false);

					// TODO: Check - page should already be dirty.
					// Make page explicitly dirty
					page.SetHeaderDirty();
					page.SetDataDirty();
				}
			}

			// At this point our task is complete
			return true;
		}

		private async Task ExpandDevice(ushort deviceId, RootPage rootPage, uint growthPages)
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
						((double)rootPage.AllocatedPages *
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

			uint oldPageCount = 0;
			uint newPageCount = 0;

			// Place root page into update mode
			rootPage.RootLock = RootLockType.Update;
			try
			{
				// Transition root page into exclusive mode
				rootPage.RootLock = RootLockType.Exclusive;

				// Delegate the request to the underlying device
				var bufferDevice = GetService<IMultipleBufferDevice>();
				oldPageCount = bufferDevice.GetDeviceInfo(deviceId).PageCount;
				newPageCount = bufferDevice.ExpandDevice(deviceId, (int)growthPages);
			}
			catch
			{
				// Assume expand failed and revert lock
				//	don't know if I really have to do this now
				rootPage.RootLock = RootLockType.Shared;
				throw;
			}

			// Create distribution pages as necessary
			if (newPageCount > oldPageCount)
			{
				await CreateDistributionPages(deviceId, oldPageCount, newPageCount - 1).ConfigureAwait(false);
			}

			// Finally update the root page.
			rootPage.ReadOnly = false;
			rootPage.AllocatedPages = newPageCount;
			rootPage.Save();
		}

		private async Task<bool> ExpandDataDeviceHandler(ExpandDataDeviceRequest request)
		{
			RootPage rootPage;

			// Do underlying device expansion
			if (request.IsDeviceIdValid)
			{
				// Load the root page and obtain update lock before we start
				var pageDevice = GetDistributionPageDevice(request.DeviceId);
				rootPage = await pageDevice.LoadOrCreateRootPage().ConfigureAwait(false);
				rootPage.RootLock = RootLockType.Shared;

				await ExpandDevice(request.DeviceId, rootPage, request.PageCount).ConfigureAwait(false);
			}
			else
			{
				// TODO: Load root page for each device in our list
				// TODO: Sort pages into "allocated pages" ascending
				//	excluding all non-expandable devices
				var deviceIds = GetDistributionPageDeviceKeys();
				var rootPages =
					new Dictionary<ushort, RootPage>();
				foreach (var deviceId in deviceIds)
				{
					// Get distribution page device
					var pageDevice =
						GetDistributionPageDevice(deviceId);
					rootPage = await pageDevice
						.LoadOrCreateRootPage()
						.ConfigureAwait(false);
					if (rootPage.IsExpandable)
					{
						rootPage.RootLock = RootLockType.Shared;
						rootPages.Add(deviceId, rootPage);
					}
					else
					{
						rootPage.Dispose();
					}
					rootPage = null;
				}

				// Walk sorted list of devices
				var hasExpanded = false;
				var failedDueToLock = false;
				var failedDueToFull = false;
				foreach (var pair in rootPages.OrderBy((item) => item.Value.AllocatedPages))
				{
					// Hook root page
					try
					{
						await ExpandDevice(pair.Key, pair.Value, request.PageCount).ConfigureAwait(false);
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
						throw new FileGroupFullException(0, FileGroupName, "Failed to expand file-group device; device is full.");
					}
					else
					{
						throw new InvalidOperationException("Failed to expand file-group device due to unknown issue.");
					}
				}
			}

			return true;
		}

		private async Task<VirtualPageId> AllocateDataPageHandler(AllocateDataPageRequest request)
		{
			// Get device keys and perform randomisation
			var deviceIds = GetDistributionPageDeviceKeys();
			deviceIds.Randomize();

			// Walk each device and attempt to allocate page
			foreach (var deviceId in deviceIds)
			{
				// Get distribution page device
				var pageDevice =
					GetDistributionPageDevice(deviceId);

				try
				{
					// Attempt to allocate (may fail)
					return await pageDevice.AllocateDataPage(request.Message).ConfigureAwait(false);
				}
				catch
				{
					// Failed either because full or busy
					//	no need to handle or log unless we want to keep
					//	statistics on dist page contention
				}
			}

			// File group must be full if we reach this point!
			throw new FileGroupFullException(FileGroupId, null);
		}

		private async Task<ObjectId> AddTableHandler(AddTableRequest request)
		{
			var objectId = ObjectId.Zero;

			// Load primary file-group root page
			using (var rootPage = (PrimaryFileGroupRootPage)
				await _primaryDevice.LoadOrCreateRootPage())
			{
				// Obtain object id for this table
				rootPage.RootLock = RootLockType.Exclusive;
				rootPage.ReadOnly = false;
				var objectRef =
					new ObjectRefInfo
					{
						Name = request.Message.TableName,
						ObjectType = ObjectType.Table
					};
				for (uint candidateObjectId = 1; ; ++candidateObjectId)
				{
					if (!_assignedObjectIds.Contains(candidateObjectId))
					{
                        objectId = new ObjectId(candidateObjectId);
						objectRef.ObjectId = objectId;
						break;
					}
				}
				rootPage.AddObjectInfo(objectRef);

				// Create database table helper and setup object
			    var table = new DatabaseTable(this)
			    {
			        FileGroupId = FileGroupId,
			        ObjectId = objectRef.ObjectId,
			        IsNewTable = true
			    };

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

				// Finalise the object information
				objectRef.FileGroupId = FileGroupId;
				objectRef.FirstPageId = table.SchemaFirstLogicalId;
				rootPage.Save();
			}

			// Set operation result
			return objectId;
		}

		private async Task<uint> AddTableIndexHandler(AddTableIndexRequest request)
		{
			uint indexId = 0;

		    var table = new DatabaseTable(this)
		    {
		        FileGroupId = FileGroupId,
		        ObjectId = request.Message.OwnerObjectId,
		        IsNewTable = false
		    };
		    //table.AddIndex

			return indexId;
		}
		#endregion
	}
}
