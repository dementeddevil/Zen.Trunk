namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;
	using Zen.Trunk.Storage;
	using Zen.Trunk.Storage.Locking;
	using Zen.Trunk.Storage.Log;

	public class DatabaseDevice : PageDevice
	{
		#region Private Types
		private class AddFileGroupDeviceRequest : TransactionContextTaskRequest<AddFileGroupDeviceParameters, ushort>
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

		private class AddFileGroupTableRequest : TransactionContextTaskRequest<AddFileGroupTableParameters, uint>
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

		private class IssueCheckPointRequest : TransactionContextTaskRequest<bool>
		{
		}
		#endregion

		#region Private Fields
		private readonly ushort _dbId;
		private ITargetBlock<AddFileGroupDeviceRequest> _addFileGroupDevicePort;
		private ITargetBlock<RemoveFileGroupDeviceRequest> _removeFileGroupDevicePort;
		private ITargetBlock<InitFileGroupPageRequest> _initFileGroupPagePort;
		private ITargetBlock<LoadFileGroupPageRequest> _loadFileGroupPagePort;
		private ITargetBlock<FlushFileGroupRequest> _flushPageBuffersPort;
		private ITargetBlock<AddFileGroupTableRequest> _addFileGroupTablePort;
		private ITargetBlock<IssueCheckPointRequest> _issueCheckPointPort;
		private ConcurrentExclusiveSchedulerPair _taskInterleave;

		// Underlying page buffer storage
		private MultipleBufferDevice _bufferDevice;
		private CachingPageBufferDevice _dataBufferDevice;

		// Locking manager
		private IDatabaseLockManager _lockManager;

		// File-group mapping
		private readonly Dictionary<byte, FileGroupDevice> _fileGroupById =
			new Dictionary<byte, FileGroupDevice>();
		private readonly Dictionary<string, FileGroupDevice> _fileGroupByName =
			new Dictionary<string, FileGroupDevice>();
		private byte _nextFileGroupId = FileGroupDevice.Primary + 1;

		// Log device
		private MasterLogPageDevice _logDevice;
		private Task _currentCheckPointTask;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseDevice"/> class.
		/// </summary>
		public DatabaseDevice(ushort dbId)
		{
			_dbId = dbId;
			Initialise();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseDevice"/> class.
		/// </summary>
		/// <param name="parentServiceProvider">The parent service provider.</param>
		public DatabaseDevice(ushort dbId, IServiceProvider parentServiceProvider)
			: base(parentServiceProvider)
		{
			_dbId = dbId;
			Initialise();
		}
		#endregion

		#region Public Properties
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the name of the tracer.
		/// </summary>
		/// <value>The name of the tracer.</value>
		protected override string TracerName => base.TracerName;

	    /// <summary>
		/// Gets the primary file group id.
		/// </summary>
		/// <value>The primary file group id.</value>
		protected virtual byte PrimaryFileGroupId => FileGroupDevice.Primary;

	    #endregion

		#region Private Properties
		/// <summary>
		/// Gets the add file group data device port.
		/// </summary>
		/// <value>The add file group data device port.</value>
		private ITargetBlock<AddFileGroupDeviceRequest> AddFileGroupDevicePort => _addFileGroupDevicePort;

	    /// <summary>
		/// Gets the remove file group device port.
		/// </summary>
		/// <value>The remove file group device port.</value>
		private ITargetBlock<RemoveFileGroupDeviceRequest> RemoveFileGroupDevicePort => _removeFileGroupDevicePort;

	    /// <summary>
		/// Gets the init file group page port.
		/// </summary>
		/// <value>The init file group page port.</value>
		private ITargetBlock<InitFileGroupPageRequest> InitFileGroupPagePort => _initFileGroupPagePort;

	    /// <summary>
		/// Gets the load file group page port.
		/// </summary>
		/// <value>The load file group page port.</value>
		private ITargetBlock<LoadFileGroupPageRequest> LoadFileGroupPagePort => _loadFileGroupPagePort;

	    /// <summary>
		/// Gets the flush device buffers port.
		/// </summary>
		/// <value>The flush device buffers port.</value>
		private ITargetBlock<FlushFileGroupRequest> FlushPageBuffersPort => _flushPageBuffersPort;

	    /// <summary>
		/// Gets the add file group table port.
		/// </summary>
		/// <value>The add file group table port.</value>
		private ITargetBlock<AddFileGroupTableRequest> AddFileGroupTablePort => _addFileGroupTablePort;

	    /// <summary>
		/// Gets the issue check point port.
		/// </summary>
		/// <value>The issue check point port.</value>
		private ITargetBlock<IssueCheckPointRequest> IssueCheckPointPort => _issueCheckPointPort;

	    #endregion

		#region Public Methods
		/// <summary>
		/// Gets the file group device with the specified name.
		/// </summary>
		/// <param name="fileGroupName">Name of the file group.</param>
		/// <returns></returns>
		public FileGroupDevice GetFileGroupDevice(string fileGroupName)
		{
			return GetFileGroupDevice(FileGroupDevice.Invalid, fileGroupName);
		}

		/// <summary>
		/// Gets the file group device with the specified id.
		/// </summary>
		/// <param name="fileGroupId">The file group id.</param>
		/// <returns></returns>
		public FileGroupDevice GetFileGroupDevice(byte fileGroupId)
		{
			return GetFileGroupDevice(fileGroupId, null);
		}

		public Task AddFileGroupDevice(AddFileGroupDeviceParameters deviceParams)
		{
			var request = new AddFileGroupDeviceRequest(deviceParams);
			if (!AddFileGroupDevicePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task RemoveFileGroupDevice(RemoveFileGroupDeviceParameters deviceParams)
		{
			var request = new RemoveFileGroupDeviceRequest(deviceParams);
			if (!RemoveFileGroupDevicePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task InitFileGroupPage(InitFileGroupPageParameters initParams)
		{
			var request = new InitFileGroupPageRequest(initParams);
			if (!InitFileGroupPagePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task LoadFileGroupPage(LoadFileGroupPageParameters loadParams)
		{
			var request = new LoadFileGroupPageRequest(loadParams);
			if (!LoadFileGroupPagePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task FlushFileGroupBuffers(FlushCachingDeviceParameters flushParams)
		{
			var request = new FlushFileGroupRequest(flushParams);
			if (!FlushPageBuffersPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task AddFileGroupTable(AddFileGroupTableParameters tableParams)
		{
			var request = new AddFileGroupTableRequest(tableParams);
			if (!AddFileGroupTablePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task IssueCheckPoint()
		{
			var request = new IssueCheckPointRequest();
			if (!IssueCheckPointPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task<ushort> AddLogDevice(AddLogDeviceParameters deviceParams)
		{
			return _logDevice.AddDevice(deviceParams);
		}

		public Task RemoveLogDevice(RemoveLogDeviceParameters deviceParams)
		{
			return _logDevice.RemoveDevice(deviceParams);
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Overridden. Gets the object corresponding to the desired service type.
		/// </summary>
		/// <param name="serviceType"></param>
		/// <returns></returns>
		/// <remarks>
		/// Checks for <b>PageDevice</b> service type and delegates everything
		/// else through the base class.
		/// </remarks>
		protected override object GetService(Type serviceType)
		{
			if (serviceType == typeof(DatabaseDevice))
			{
				return this;
			}
			if (serviceType == typeof(IDatabaseLockManager))
			{
				if (_lockManager == null)
				{
					_lockManager = new DatabaseLockManager(
						GetService<GlobalLockManager>(), _dbId);
				}
				return _lockManager;
			}
			if (serviceType == typeof(LogPageDevice) ||
				serviceType == typeof(MasterLogPageDevice))
			{
				return _logDevice;
			}
			if (serviceType == typeof(IBufferDevice) ||
				serviceType == typeof(IMultipleBufferDevice))
			{
				return _bufferDevice;
			}
			if (serviceType == typeof(CachingPageBufferDevice))
			{
				return _dataBufferDevice;
			}

			// Delegate everything else
			return base.GetService(serviceType);
		}

		/// <summary>
		/// Performs a device-specific mount operation.
		/// </summary>
		/// <returns></returns>
		protected override async Task OnOpen()
		{
			Tracer.WriteInfoLine("DatabaseDevice.OnOpen -> Start");

			// Sanity check
			if (_fileGroupById.Count == 0)
			{
				throw new InvalidOperationException("No file-groups.");
			}

			FileGroupDevice fgDevice;
			if (!_fileGroupById.TryGetValue(PrimaryFileGroupId, out fgDevice))
			{
				throw new InvalidOperationException("No primary file-group device.");
			}

			// Mount the underlying device
			Tracer.WriteVerboseLine("Opening underlying buffer device...");
			await _bufferDevice.OpenAsync().ConfigureAwait(false);

			// Mount the primary file-group device
			Tracer.WriteVerboseLine("Opening primary file-group device...");
			await fgDevice.OpenAsync(IsCreate).ConfigureAwait(false);

			// At this point the primary file-group is mounted and all
			//	secondary file-groups are mounted too (via AddFileGroupDevice)

			// Mount the log device
			Tracer.WriteVerboseLine("Opening log device...");
			await _logDevice.OpenAsync(IsCreate).ConfigureAwait(false);

			// If this is not create then we need to perform recovery
			if (!IsCreate)
			{
				Tracer.WriteVerboseLine("Initiating recovery...");
				await _logDevice.PerformRecovery().ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Called when closing the device.
		/// </summary>
		/// <returns></returns>
		protected override async Task OnClose()
		{
			TrunkTransactionContext.BeginTransaction(this);
			var committed = false;
			try
			{
				// Issue a checkpoint so we close the database in a known state
				var request = new IssueCheckPointRequest();
				if (!IssueCheckPointPort.Post(request))
				{
					throw new BufferDeviceShuttingDownException();
				}
				await request.Task.ConfigureAwait(false);

				await TrunkTransactionContext.Commit();
				committed = true;
			}
			catch
			{
			}
			if (!committed)
			{
				await TrunkTransactionContext.Rollback();
			}

			// Close all secondary file-groups in parallel
			var secondaryDeviceTasks = _fileGroupByName.Values
				.Where((item) => !item.IsPrimaryFileGroup)
				.Select((item) => Task.Run(() => item.CloseAsync()))
				.ToArray();
			await TaskExtra
				.WhenAllOrEmpty(secondaryDeviceTasks)
				.ConfigureAwait(false);

			// Close the primary file-group last
			var primaryDevice = _fileGroupByName.Values
				.FirstOrDefault((item) => item.IsPrimaryFileGroup);
			if (primaryDevice != null)
			{
				await primaryDevice.CloseAsync().ConfigureAwait(false);
			}

			// Close the caching page device
			await _dataBufferDevice.CloseAsync().ConfigureAwait(false);

			// Close the log device
			await _logDevice.CloseAsync().ConfigureAwait(false);

			// Close underlying buffer device
			await _bufferDevice.CloseAsync().ConfigureAwait(false);

			// Invalidate objects
			_logDevice = null;
			_dataBufferDevice = null;
			_bufferDevice = null;
		}
		#endregion

		#region Private Methods
		private void Initialise()
		{
			// Initialise devices
			var bufferFactory = GetService<IVirtualBufferFactory>();
			_bufferDevice = new MultipleBufferDevice(bufferFactory, true);
			_dataBufferDevice = new CachingPageBufferDevice(_bufferDevice);
			_logDevice = new MasterLogPageDevice(string.Empty, this);

			// Setup ports
			_taskInterleave = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default);
			_initFileGroupPagePort =
				new TransactionContextActionBlock<InitFileGroupPageRequest, bool>(
					(request) => InitFileGroupPageHandler(request),
					new ExecutionDataflowBlockOptions
					{
						TaskScheduler = _taskInterleave.ConcurrentScheduler
					});
			_loadFileGroupPagePort =
				new TransactionContextActionBlock<LoadFileGroupPageRequest, bool>(
					(request) => LoadFileGroupPageHandler(request),
					new ExecutionDataflowBlockOptions
					{
						TaskScheduler = _taskInterleave.ConcurrentScheduler
					});
			_addFileGroupDevicePort =
				new TransactionContextActionBlock<AddFileGroupDeviceRequest, ushort>(
					(request) => AddFileGroupDataDeviceHandler(request),
					new ExecutionDataflowBlockOptions
					{
						TaskScheduler = _taskInterleave.ExclusiveScheduler
					});
			_removeFileGroupDevicePort =
				new TransactionContextActionBlock<RemoveFileGroupDeviceRequest, bool>(
					(request) => RemoveFileGroupDeviceHandler(request),
					new ExecutionDataflowBlockOptions
					{
						TaskScheduler = _taskInterleave.ExclusiveScheduler
					});

			_flushPageBuffersPort =
				new TaskRequestActionBlock<FlushFileGroupRequest, bool>(
					(request) => FlushDeviceBuffersHandler(request),
					new ExecutionDataflowBlockOptions
					{
						TaskScheduler = _taskInterleave.ExclusiveScheduler
					});

			// Table action ports
			_addFileGroupTablePort =
				new TransactionContextActionBlock<AddFileGroupTableRequest, uint>(
					(request) => AddFileGroupTableHandler(request),
					new ExecutionDataflowBlockOptions
					{
						TaskScheduler = _taskInterleave.ExclusiveScheduler
					});

			_issueCheckPointPort =
				new TransactionContextActionBlock<IssueCheckPointRequest, bool>(
					(request) => IssueCheckPointHandler(request),
					new ExecutionDataflowBlockOptions
					{
						TaskScheduler = _taskInterleave.ExclusiveScheduler
					});
		}

		private async Task<ushort> AddFileGroupDataDeviceHandler(AddFileGroupDeviceRequest request)
		{
			// Create valid file group ID as needed
			FileGroupDevice fileGroupDevice = null;
			var isFileGroupCreate = false;
			var isFileGroupOpenNeeded = false;
			if ((request.Message.FileGroupIdValid && !_fileGroupById.TryGetValue(request.Message.FileGroupId, out fileGroupDevice)) ||
				(!request.Message.FileGroupIdValid && !_fileGroupByName.TryGetValue(request.Message.FileGroupName, out fileGroupDevice)))
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

				// Determine new file-group id
				var fileGroupId = request.Message.FileGroupId;

				// We only use the supplied file-group id if it is
				//	master or primary
				// Everything else is recoded.
				if (!request.Message.FileGroupIdValid ||
					(fileGroupId != FileGroupDevice.Master &&
					fileGroupId != FileGroupDevice.Primary))
				{
					fileGroupId = _nextFileGroupId++;
				}

				// Create new file group device and add to map
				if (fileGroupId == FileGroupDevice.Master)
				{
					fileGroupDevice = new MasterDatabasePrimaryFileGroupDevice(
						this, fileGroupId, request.Message.FileGroupName);
				}
				else if (fileGroupId == FileGroupDevice.Primary)
				{
					fileGroupDevice = new PrimaryFileGroupDevice(
						this, fileGroupId, request.Message.FileGroupName);
				}
				else
				{
					fileGroupDevice = new SecondaryFileGroupDevice(
						this, fileGroupId, request.Message.FileGroupName);
				}

				_fileGroupById.Add(fileGroupId, fileGroupDevice);
				_fileGroupByName.Add(fileGroupName, fileGroupDevice);

				isFileGroupCreate = (request.Message.CreatePageCount > 0);
				isFileGroupOpenNeeded = true;
			}

			// Add child device to file-group
			var deviceId = await fileGroupDevice.AddDataDevice(request.Message);

			// If this is the first call for a file-group AND database is open or opening
			//	then open the new file-group device too
			if (isFileGroupOpenNeeded && (
				DeviceState == MountableDeviceState.Opening ||
				DeviceState == MountableDeviceState.Open))
			{
				await fileGroupDevice.OpenAsync(isFileGroupCreate).ConfigureAwait(false);
			}

			// We're done
			return deviceId;
		}

		private async Task<bool> RemoveFileGroupDeviceHandler(RemoveFileGroupDeviceRequest request)
		{
			var fileGroupDevice = GetFileGroupDevice(
				FileGroupDevice.Invalid, request.Message.FileGroupName);
			await fileGroupDevice.RemoveDataDevice(request.Message);
			return true;
		}

		private async Task<bool> InitFileGroupPageHandler(InitFileGroupPageRequest request)
		{
			var fileGroupDevice = GetFileGroupDevice(
				request.Message.FileGroupId, request.Message.FileGroupName);

			// Setup page site
			HookupPageSite(request.Message.Page);

			// Pass request onwards
			await fileGroupDevice
				.InitDataPage(request.Message)
				.ConfigureAwait(false);

			// Setup file-group id on page if necessary
			if (!request.Message.FileGroupIdValid)
			{
				request.Message.Page.FileGroupId = fileGroupDevice.FileGroupId;
			}
			return true;
		}

		private async Task<bool> LoadFileGroupPageHandler(LoadFileGroupPageRequest request)
		{
			// Detect attempt to load using only a virtual id
			if (request.Message.VirtualPageIdValid &&
				!request.Message.LogicalPageIdValid &&
				!request.Message.FileGroupIdValid &&
				string.IsNullOrEmpty(request.Message.FileGroupName))
			{
				Tracer.WriteVerboseLine("LoadFileGroupPage - By VirtualId {0}",
					request.Message.Page.VirtualId);

				// This type of load request is typically only ever 
				//	performed during database recovery
				// TODO: Consider making this type of load illegal outside
				//	of recovery - for now it is allowed only if the page
				//	type is DataPage.
				if (request.Message.Page.GetType() != typeof(DataPage))
				{
					throw new StorageEngineException(
						"VirtualId-only page loading only supported for DataPage page type.");
				}

				// Setup page site and notify page of impending load
				HookupPageSite(request.Message.Page);
				request.Message.Page.PreLoadInternal();

				// Load the buffer from the underlying cache
				var pageId = new VirtualPageId(request.Message.Page.VirtualId);
				request.Message.Page.DataBuffer = await _dataBufferDevice.LoadPageAsync(pageId).ConfigureAwait(false);
				request.Message.Page.PostLoadInternal();
			}
			else
			{
				Tracer.WriteVerboseLine("LoadFileGroupPage - By File-Group [{0},{1}]",
					request.Message.FileGroupId, request.Message.FileGroupName);
				var fileGroupDevice = GetFileGroupDevice(
					request.Message.FileGroupId, request.Message.FileGroupName);

				// Setup page site
				HookupPageSite(request.Message.Page);

				// Pass request onwards
				await fileGroupDevice
					.LoadDataPage(request.Message)
					.ConfigureAwait(false);

				// Setup file-group id on page if necessary
				if (!request.Message.FileGroupIdValid)
				{
					request.Message.Page.FileGroupId = fileGroupDevice.FileGroupId;
				}
			}
			return true;
		}

		private async Task<bool> FlushDeviceBuffersHandler(FlushFileGroupRequest request)
		{
			await _dataBufferDevice.FlushPagesAsync(request.Message);
			return true;
		}

		private Task<uint> AddFileGroupTableHandler(AddFileGroupTableRequest request)
		{
			var fileGroupDevice = GetFileGroupDevice(
				request.Message.FileGroupId, request.Message.FileGroupName);

			// Delegate request to file-group device.
			return fileGroupDevice.AddTable(request.Message);
		}

		private Task<bool> IssueCheckPointHandler(IssueCheckPointRequest request)
		{
			var tcs = new TaskCompletionSource<bool>();

			var current = _currentCheckPointTask;
			if (current == null)
			{
				// Setup the completion task object then start the operation
				// NOTE: We do NOT await the child operation here
				current = _currentCheckPointTask = ExecuteCheckPoint();
			}

			// Checkpoint is in progress so attach to current operation
			current.ContinueWith((t) => tcs.SetResult(true), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
			current.ContinueWith((t) => tcs.SetFromTask(t), TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);

			return tcs.Task;
		}

		private async Task ExecuteCheckPoint()
		{
			Tracer.WriteVerboseLine("CheckPoint - Begin");

			// Issue begin checkpoint
			await _logDevice.WriteEntry(new BeginCheckPointLogEntry()).ConfigureAwait(false);

			Exception exception = null;
			try
			{
				// Ask data device to dump all unwritten/logged pages to disk
				// Typically this shouldn't find many pages to write except under
				//	heavy load.
				await _dataBufferDevice.FlushPagesAsync(new FlushCachingDeviceParameters(true)).ConfigureAwait(false);
			}
			catch (Exception error)
			{
				exception = error;
			}

			// Issue end checkpoint
			await _logDevice.WriteEntry(new EndCheckPointLogEntry()).ConfigureAwait(false);

			// Discard current check-point task
			_currentCheckPointTask = null;

			// Throw if we have failed
			if (exception != null)
			{
				Tracer.WriteVerboseLine("CheckPoint - End with exception [{0}]",
					exception.Message);
				throw exception;
			}
			else
			{
				Tracer.WriteVerboseLine("CheckPoint - End");
			}
		}

		private FileGroupDevice GetPrimaryFileGroupDevice()
		{
			return _fileGroupById.Values.FirstOrDefault((item) => item.IsPrimaryFileGroup);
		}

		private FileGroupDevice GetFileGroupDevice(byte fileGroupId, string fileGroupName)
		{
			if (!string.IsNullOrEmpty(fileGroupName))
			{
				fileGroupName = fileGroupName.Trim().ToUpper();
			}

			var idValid = (fileGroupId != FileGroupDevice.Invalid);
			FileGroupDevice fileGroupDevice = null;
			if ((idValid && _fileGroupById.TryGetValue(fileGroupId, out fileGroupDevice)) ||
				(!string.IsNullOrEmpty(fileGroupName) && _fileGroupByName.TryGetValue(fileGroupName, out fileGroupDevice)))
			{
				return fileGroupDevice;
			}

			throw new FileGroupInvalidException(fileGroupId, fileGroupName);
		}
		#endregion
	}
}
