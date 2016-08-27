using Autofac;

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
		private class AddFileGroupDeviceRequest : TransactionContextTaskRequest<AddFileGroupDeviceParameters, DeviceId>
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

	    private class AddLogDeviceRequest : TransactionContextTaskRequest<AddLogDeviceParameters, DeviceId>
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

		private readonly DatabaseId _dbId;

	    // Underlying page buffer storage
		private IMultipleBufferDevice _bufferDevice;
		private CachingPageBufferDevice _dataBufferDevice;

		// File-group mapping
		private readonly Dictionary<FileGroupId, FileGroupDevice> _fileGroupById =
			new Dictionary<FileGroupId, FileGroupDevice>();
		private readonly Dictionary<string, FileGroupDevice> _fileGroupByName =
			new Dictionary<string, FileGroupDevice>();
		private FileGroupId _nextFileGroupId = FileGroupId.Primary.Next;

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
                    request => InitFileGroupPageHandler(request),
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ConcurrentScheduler
                    });
            LoadFileGroupPagePort =
                new TransactionContextActionBlock<LoadFileGroupPageRequest, bool>(
                    request => LoadFileGroupPageHandler(request),
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ConcurrentScheduler
                    });
            AddFileGroupDevicePort =
                new TransactionContextActionBlock<AddFileGroupDeviceRequest, DeviceId>(
                    request => AddFileGroupDataDeviceHandler(request),
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });
            RemoveFileGroupDevicePort =
                new TransactionContextActionBlock<RemoveFileGroupDeviceRequest, bool>(
                    request => RemoveFileGroupDeviceHandler(request),
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });

            FlushPageBuffersPort =
                new TaskRequestActionBlock<FlushFileGroupRequest, bool>(
                    request => FlushDeviceBuffersHandler(request),
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });

            // Table action ports
            AddFileGroupTablePort =
                new TransactionContextActionBlock<AddFileGroupTableRequest, ObjectId>(
                    request => AddFileGroupTableHandler(request),
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });

            IssueCheckPointPort =
                new TransactionContextActionBlock<IssueCheckPointRequest, bool>(
                    request => IssueCheckPointHandler(request),
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });

            AddLogDevicePort =
                new TransactionContextActionBlock<AddLogDeviceRequest, DeviceId>(
                    request => AddLogDeviceHandler(request),
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });
            RemoveLogDevicePort =
                new TransactionContextActionBlock<RemoveLogDeviceRequest, bool>(
                    request => RemoveLogDeviceHandler(request),
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = taskInterleave.ExclusiveScheduler
                    });
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
		protected virtual FileGroupId PrimaryFileGroupId => FileGroupId.Primary;
	    #endregion

		#region Private Properties
	    private IMultipleBufferDevice RawBufferDevice
	    {
	        get
	        {
	            if (_bufferDevice == null)
	            {
	                _bufferDevice = ResolveDeviceService<IMultipleBufferDevice>();
	            }
	            return _bufferDevice;
	        }
	    }

	    private CachingPageBufferDevice CachingBufferDevice
	    {
	        get
	        {
	            if (_dataBufferDevice == null)
	            {
	                _dataBufferDevice = ResolveDeviceService<CachingPageBufferDevice>();
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
		/// Gets the flush device buffers port.
		/// </summary>
		/// <value>The flush device buffers port.</value>
		private ITargetBlock<FlushFileGroupRequest> FlushPageBuffersPort { get; }

	    /// <summary>
		/// Gets the add file group table port.
		/// </summary>
		/// <value>The add file group table port.</value>
		private ITargetBlock<AddFileGroupTableRequest> AddFileGroupTablePort { get; }

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
		/// Gets the file group device with the specified name.
		/// </summary>
		/// <param name="fileGroupName">Name of the file group.</param>
		/// <returns></returns>
		public FileGroupDevice GetFileGroupDevice(string fileGroupName)
		{
			return GetFileGroupDevice(FileGroupId.Invalid, fileGroupName);
		}

		/// <summary>
		/// Gets the file group device with the specified id.
		/// </summary>
		/// <param name="fileGroupId">The file group id.</param>
		/// <returns></returns>
		public FileGroupDevice GetFileGroupDevice(FileGroupId fileGroupId)
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

		public Task<DeviceId> AddLogDevice(AddLogDeviceParameters deviceParams)
		{
		    var request = new AddLogDeviceRequest(deviceParams);
		    if (!AddLogDevicePort.Post(request))
		    {
		        throw new BufferDeviceShuttingDownException();
		    }
		    return request.Task;
		}

		public Task RemoveLogDevice(RemoveLogDeviceParameters deviceParams)
		{
            var request = new RemoveLogDeviceRequest(deviceParams);
            if (!RemoveLogDevicePort.Post(request))
            {
                throw new BufferDeviceShuttingDownException();
            }
            return request.Task;
        }
        #endregion

        #region Protected Methods

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
	            .As<IMultipleBufferDevice>();
	        builder.RegisterType<CachingPageBufferDevice>().AsSelf();

	        builder
	            .RegisterType<DatabaseLockManager>()
                .WithParameter("dbId", _dbId)
	            .As<IDatabaseLockManager>()
	            .SingleInstance();

	        builder
                .Register(context => _masterLogPageDevice)
	            .As<LogPageDevice>()
	            .As<MasterLogPageDevice>();

	        builder.RegisterType<MasterDatabasePrimaryFileGroupDevice>()
                .OnActivated(e => e.Instance.InitialiseDeviceLifetimeScope(LifetimeScope));
            builder.RegisterType<PrimaryFileGroupDevice>()
                .OnActivated(e => e.Instance.InitialiseDeviceLifetimeScope(LifetimeScope));
            builder.RegisterType<SecondaryFileGroupDevice>()
                .OnActivated(e => e.Instance.InitialiseDeviceLifetimeScope(LifetimeScope));
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
			await RawBufferDevice.OpenAsync().ConfigureAwait(false);

			// Mount the primary file-group device
			Tracer.WriteVerboseLine("Opening primary file-group device...");
			await fgDevice.OpenAsync(IsCreate).ConfigureAwait(false);

			// At this point the primary file-group is mounted and all
			//	secondary file-groups are mounted too (via AddFileGroupDevice)

			// Mount the log device
			Tracer.WriteVerboseLine("Opening log device...");
			await ResolveDeviceService<MasterLogPageDevice>().OpenAsync(IsCreate).ConfigureAwait(false);

			// If this is not create then we need to perform recovery
			if (!IsCreate)
			{
				Tracer.WriteVerboseLine("Initiating recovery...");
				await ResolveDeviceService<MasterLogPageDevice>().PerformRecovery().ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Called when closing the device.
		/// </summary>
		/// <returns></returns>
		protected override async Task OnClose()
		{
			TrunkTransactionContext.BeginTransaction(LifetimeScope);
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

			// Close the caching page device
			await CachingBufferDevice.CloseAsync().ConfigureAwait(false);

			// Close the log device
			await ResolveDeviceService<MasterLogPageDevice>().CloseAsync().ConfigureAwait(false);

			// Close underlying buffer device
			await RawBufferDevice.CloseAsync().ConfigureAwait(false);

			// Invalidate objects
			_dataBufferDevice = null;
			_bufferDevice = null;
		}
		#endregion

		#region Private Methods
		private async Task<DeviceId> AddFileGroupDataDeviceHandler(AddFileGroupDeviceRequest request)
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
					(fileGroupId != FileGroupId.Master &&
					fileGroupId != FileGroupId.Primary))
				{
					fileGroupId = _nextFileGroupId = _nextFileGroupId.Next;
				}

				// Create new file group device and add to map
				if (fileGroupId == FileGroupId.Master)
				{
				    fileGroupDevice = ResolveDeviceService<MasterDatabasePrimaryFileGroupDevice>(
				        new NamedParameter("id", fileGroupId),
				        new NamedParameter("name", request.Message.FileGroupName));
				}
				else if (fileGroupId == FileGroupId.Primary)
				{
                    fileGroupDevice = ResolveDeviceService<PrimaryFileGroupDevice>(
                        new NamedParameter("id", fileGroupId),
                        new NamedParameter("name", request.Message.FileGroupName));
				}
				else
				{
                    fileGroupDevice = ResolveDeviceService<SecondaryFileGroupDevice>(
                        new NamedParameter("id", fileGroupId),
                        new NamedParameter("name", request.Message.FileGroupName));
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
			var fileGroupDevice = GetFileGroupDevice(FileGroupId.Invalid, request.Message.FileGroupName);
			await fileGroupDevice.RemoveDataDevice(request.Message).ConfigureAwait(false);
			return true;
		}

	    private async Task<DeviceId> AddLogDeviceHandler(AddLogDeviceRequest request)
	    {
	        if (_masterLogPageDevice == null)
	        {
	            _masterLogPageDevice = new MasterLogPageDevice(string.Empty);
                _masterLogPageDevice.InitialiseDeviceLifetimeScope(LifetimeScope);
	        }

            return await _masterLogPageDevice.AddDevice(request.Message).ConfigureAwait(false);
	    }

	    private async Task<bool> RemoveLogDeviceHandler(RemoveLogDeviceRequest request)
	    {
	        if (_masterLogPageDevice != null)
	        {
	            if (request.Message.DeviceIdValid && request.Message.DeviceId == DeviceId.Primary)
	            {
	                throw new InvalidOperationException();
	            }

	            await _masterLogPageDevice.RemoveDevice(request.Message).ConfigureAwait(false);
	            return true;
	        }
	        return false;
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
				request.Message.Page.DataBuffer = await CachingBufferDevice
                    .LoadPageAsync(request.Message.Page.VirtualId)
                    .ConfigureAwait(false);
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
			await CachingBufferDevice.FlushPagesAsync(request.Message);
			return true;
		}

		private Task<ObjectId> AddFileGroupTableHandler(AddFileGroupTableRequest request)
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
			current.ContinueWith(t => tcs.SetResult(true), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
			current.ContinueWith(t => tcs.SetFromTask(t), TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);

			return tcs.Task;
		}

		private async Task ExecuteCheckPoint()
		{
			Tracer.WriteVerboseLine("CheckPoint - Begin");

			// Issue begin checkpoint
			await ResolveDeviceService<MasterLogPageDevice>().WriteEntry(new BeginCheckPointLogEntry()).ConfigureAwait(false);

			Exception exception = null;
			try
			{
				// Ask data device to dump all unwritten/logged pages to disk
				// Typically this shouldn't find many pages to write except under
				//	heavy load.
				await CachingBufferDevice
                    .FlushPagesAsync(new FlushCachingDeviceParameters(true))
                    .ConfigureAwait(false);
			}
			catch (Exception error)
			{
				exception = error;
			}

			// Issue end checkpoint
			await ResolveDeviceService<MasterLogPageDevice>().WriteEntry(new EndCheckPointLogEntry()).ConfigureAwait(false);

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
			return _fileGroupById.Values.FirstOrDefault(item => item.IsPrimaryFileGroup);
		}

		private FileGroupDevice GetFileGroupDevice(FileGroupId fileGroupId, string fileGroupName)
		{
			if (!string.IsNullOrEmpty(fileGroupName))
			{
				fileGroupName = fileGroupName.Trim().ToUpper();
			}

			var idValid = (fileGroupId != FileGroupId.Invalid);
			FileGroupDevice fileGroupDevice;
			if ((idValid && _fileGroupById.TryGetValue(fileGroupId, out fileGroupDevice)) ||
				(!string.IsNullOrEmpty(fileGroupName) && _fileGroupByName.TryGetValue(fileGroupName, out fileGroupDevice)))
			{
				return fileGroupDevice;
			}

			throw new FileGroupInvalidException(DeviceId.Zero, fileGroupId, fileGroupName);
		}
		#endregion
	}
}
