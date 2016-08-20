// -----------------------------------------------------------------------
// <copyright file="MasterDatabaseDevice.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Zen.Trunk.Storage.IO;
	using Zen.Trunk.Storage.Locking;
	using Zen.Trunk.Storage.Log;

	public class MasterDatabaseDevice : DatabaseDevice
	{
		#region Public Fields
		public static readonly string[] ReservedDatabaseNames =
			new string[] { "MASTER", "TEMPDB" };
		#endregion

		#region Private Fields
		private GlobalLockManager _globalLockManager;
		private int _nextDatabaseId = 0;
		private readonly Dictionary<string, DatabaseDevice> _userDatabases =
			new Dictionary<string, DatabaseDevice>();
		private bool _hasAttachedMaster;
		private IVirtualBufferFactory _bufferFactory;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MasterDatabaseDevice"/> class.
		/// </summary>
		public MasterDatabaseDevice()
			: base(DatabaseId.Zero)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MasterDatabaseDevice"/> class.
		/// </summary>
		/// <param name="parentServiceProvider">The parent service provider.</param>
		public MasterDatabaseDevice(IServiceProvider parentServiceProvider)
			: base(DatabaseId.Zero, parentServiceProvider)
		{
		}
		#endregion

		#region Public Properties
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the primary file group id.
		/// </summary>
		/// <value>The primary file group id.</value>
		protected override FileGroupId PrimaryFileGroupId => FileGroupId.Master;

	    #endregion

		#region Public Methods
		public async Task AttachDatabase(AttachDatabaseParameters request)
		{
			// Determine whether we are attaching the master database
			var mountingMaster = false;
			DatabaseDevice device;
			if (!_hasAttachedMaster)
			{
				if (!request.Name.Equals("master", StringComparison.OrdinalIgnoreCase))
				{
					throw new ArgumentException("First database attached must be master database.");
				}

				mountingMaster = true;
				device = this;
			}
			else
			{
				if (request.Name.Equals("master", StringComparison.OrdinalIgnoreCase))
				{
					throw new ArgumentException("Master database has already been attached.");
				}

				// Create new database device
				// NOTE: This object is the parent service provider
				var dbId = (ushort)Interlocked.Increment(ref _nextDatabaseId);
				device = new DatabaseDevice(new DatabaseId(dbId), this);
			}

			// Create transaction context for the new database device
			//TrunkTransactionContext.BeginTransaction(device, TimeSpan.FromMinutes(1));

			// Check database name is unique
			if (_userDatabases.ContainsKey(request.Name))
			{
				throw new ArgumentException("Database with same name already exists.");
			}

			// Get the page size so we can calculate the number of pages
			//	needed for the device (in create scenarios only)
			uint pageSize;
			{
				var page = new DataPage();
				pageSize = page.PageSize;
			}

			// Walk the list of file-groups
			var primaryName = string.Empty;
			var primaryFileName = string.Empty;
			var mountingPrimary = true;
			var needToCreateMasterFilegroup = mountingMaster;
			foreach (var fileGroup in request.FileGroups)
			{
				DeviceId deviceId = DeviceId.Primary;
				foreach (var file in fileGroup.Item2)
				{
					// Determine number of pages to use if we are creating devices
					uint createPageCount = 0;
					if (request.IsCreate)
					{
						createPageCount = (uint)(file.Size / pageSize);
					}

					if (mountingPrimary)
					{
						if (needToCreateMasterFilegroup)
						{
							await device
                                .AddFileGroupDevice(
                                    new AddFileGroupDeviceParameters(
								        FileGroupId.Master,
                                        fileGroup.Item1,
                                        file.Name,
                                        file.FileName,
                                        deviceId,
                                        createPageCount))
                                .ConfigureAwait(false);
						}
						else
						{
							await device
                                .AddFileGroupDevice(
                                    new AddFileGroupDeviceParameters(
								        FileGroupId.Primary,
                                        fileGroup.Item1,
                                        file.Name,
                                        file.FileName,
                                        deviceId,
                                        createPageCount))
                                .ConfigureAwait(false);
						}

						primaryName = file.Name;
						primaryFileName = file.FileName;
					}
					else
					{
						await device
                            .AddFileGroupDevice(
                                new AddFileGroupDeviceParameters(
							        FileGroupId.Invalid,
                                    fileGroup.Item1,
                                    file.Name,
                                    file.FileName,
                                    deviceId,
                                    createPageCount))
                            .ConfigureAwait(false);
					}

                    // Advance to next device
					deviceId = deviceId.Next;
					mountingPrimary = needToCreateMasterFilegroup = false;
				}
			}

			// Walk the list of log files
			foreach (var file in request.LogFiles)
			{
				var pageCount = (uint)(file.Size / pageSize);

				var deviceParams = new AddLogDeviceParameters(
                    file.Name, file.FileName, DeviceId.Zero, pageCount);
				await device.AddLogDevice(deviceParams).ConfigureAwait(false);
			}

			// Now mount the device
			await device.OpenAsync(true).ConfigureAwait(false);

			// If we get this far then commit transaction used to create
			//	the database device
			//TrunkTransactionContext.Commit();

			// If we are not attaching the master database then update
			//	the master root page...
			if (!mountingMaster)
			{
				//TrunkTransactionContext.BeginTransaction(this, TimeSpan.FromMinutes(1));

				// Load the master database primary file-group root page
				var masterRootPage =
					new MasterDatabasePrimaryFileGroupRootPage();
				masterRootPage.RootLock = Locking.RootLockType.Shared;

				// Load page from root device
				await LoadFileGroupPage(
					new LoadFileGroupPageParameters(null, masterRootPage, true)).ConfigureAwait(false);

				// Add this database information to the database list
				masterRootPage.ReadOnly = false;
				masterRootPage.RootLock = Locking.RootLockType.Update;
				masterRootPage.RootLock = Locking.RootLockType.Exclusive;
				masterRootPage.AddDatabase(request.Name, primaryName, primaryFileName);
				masterRootPage.Save();

				//TrunkTransactionContext.Commit();

				// If we get this far then add device
				_userDatabases.Add(request.Name, device);
			}
			else
			{
				_hasAttachedMaster = true;
			}
		}

		public Task DetachDatabase(string name)
		{
			// Check for reserved database names
			foreach (var reserved in ReservedDatabaseNames)
			{
				if (string.Equals(reserved, name))
				{
					throw new ArgumentException("System databases are always online.");
				}
			}

			// Locate user database
			DatabaseDevice device;
			if (!_userDatabases.TryGetValue(name, out device))
			{
				throw new ArgumentException("Database not found.");
			}

			// Detach the database
			// TODO: Detaching a database means we must do the following;
			//	1. Place database into pending close mode (no further txns allowed)
			//		(actually this is optional if we do a checkpoint since any
			//		in-flight transactions will be rolledback when the database is
			//		reattached.)
			//	2. Wait for all transactions to complete (this may timeout)
			//	3. Wait for all logs to be written to disk (in practice this means
			//		we must issue a checkpoint and wait for it to complete)
			//	4. Update the master database root page (with the new list of
			//		attached databases)

			// For now do bugger all
			return CompletedTask.Default;
		}

		public Task ChangeDatabaseStatus(ChangeDatabaseStatusParameters request)
		{
			// Check for reserved database names
			foreach (var reserved in ReservedDatabaseNames)
			{
				if (string.Equals(reserved, request.Name))
				{
					throw new ArgumentException("System databases are always online.");
				}
			}

			// Locate user database
			DatabaseDevice device;
			if (!_userDatabases.TryGetValue(request.Name, out device))
			{
				throw new ArgumentException("Database not found.");
			}

			// Change the database status
			// TODO: Marking a database online means we must mount it
			// TODO: Marking a database as offline means we must wait for
			//	all transactions to complete (this may timeout) and wait
			//	for all logs to be written to disk (in practice this means
			//	we must issue a checkpoint and wait for it to complete)

			// Finally save the updated database state in the root page of
			//	the master database.
			return CompletedTask.Default;
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Overridden. Gets the object corresponding to the desired service type.
		/// </summary>
		/// <param name="serviceType"></param>
		/// <returns></returns>
		/// <remarks>
		/// Checks for <b>MasterDatabaseDevice</b> service type and delegates
		/// everything else through the base class.
		/// </remarks>
		protected override object GetService(Type serviceType)
		{
			if (serviceType == typeof(GlobalLockManager))
			{
				if (_globalLockManager == null)
				{
					_globalLockManager = new GlobalLockManager();
				}
				return _globalLockManager;
			}
			if (serviceType == typeof(MasterDatabaseDevice))
			{
				return this;
			}
			if (serviceType == typeof(IVirtualBufferFactory))
			{
				if (_bufferFactory == null)
				{
					_bufferFactory = new VirtualBufferFactory(32, 8192);
				}
				return _bufferFactory;
			}
			return base.GetService(serviceType);
		}

		/// <summary>
		/// Performs a device-specific mount operation.
		/// </summary>
		/// <returns></returns>
		protected override async Task OnOpen()
		{
			// Perform base class mounting
			await base.OnOpen().ConfigureAwait(false);

			// If we are in non-create mode then we need to mount any other
			//	online databases
			if (!IsCreate)
			{
				// Load the master database primary file-group root page
				var masterRootPage =
					new MasterDatabasePrimaryFileGroupRootPage();
				masterRootPage.RootLock = Locking.RootLockType.Shared;

				// Load page from root device
				await LoadFileGroupPage(
					new LoadFileGroupPageParameters(null, masterRootPage, true)).ConfigureAwait(false);

				// Walk the list of databases in the root page
				// NOTE: We exclude offline devices
				foreach (var deviceInfo in masterRootPage
					.GetDatabaseEnumerator()
					.Where((item) => item.IsOnline))
				{
					// Create attach request and post - no need to wait...
					var attach =
						new AttachDatabaseParameters
						{
							Name = deviceInfo.Name,
							IsCreate = false,
						};
					attach.AddDataFile(
						"PRIMARY",
						new FileSpec
						{
							Name = deviceInfo.PrimaryName,
							FileName = deviceInfo.PrimaryFilePathName
						});
					await AttachDatabase(attach);
				}
			}
		}

		/// <summary>
		/// Called when closing the device.
		/// </summary>
		/// <returns></returns>
		protected override async Task OnClose()
		{
			// TODO: Make sure we close TEMPDB last
			// Close user databases first
			var subTasks = new List<Task>();
			foreach (var device in _userDatabases.Values)
			{
				subTasks.Add(device.CloseAsync());
			}
			await TaskExtra
				.WhenAllOrEmpty(subTasks.ToArray())
				.ConfigureAwait(false);

			// Finally close the master device
			await base.OnClose().ConfigureAwait(false);
		}

		/// <summary>
		/// Releases managed resources
		/// </summary>
		protected override void DisposeManagedObjects()
		{
			// TODO: 
			// Do base class dispose actions
			base.DisposeManagedObjects();

			// Finally dispose of the buffer factory
			if (_bufferFactory != null)
			{
				_bufferFactory.Dispose();
				_bufferFactory = null;
			}
		}
		#endregion
	}

	public class AttachDatabaseParameters
	{
		private readonly IList<Tuple<string, IList<FileSpec>>> _fileGroups =
			new List<Tuple<string, IList<FileSpec>>>();
		private readonly IList<FileSpec> _logFiles =
			new List<FileSpec>();

		public string Name
		{
			get;
			set;
		}

		public bool IsCreate
		{
			get;
			set;
		}

		public bool HasPrimaryFileGroup
		{
			get
			{
				return _fileGroups.Any((item) =>
					item.Item1.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase));
			}
		}

		public IEnumerable<Tuple<string, IList<FileSpec>>> FileGroups => _fileGroups;

	    public IEnumerable<FileSpec> LogFiles => _logFiles;

	    public void AddDataFile(string fileGroup, FileSpec file)
		{
			var files = _fileGroups
				.Where((item) => item.Item1.Equals(fileGroup, StringComparison.OrdinalIgnoreCase))
				.Select((item) => item.Item2)
				.FirstOrDefault();
			if (files == null)
			{
				files = new List<FileSpec>();
				_fileGroups.Add(new Tuple<string, IList<FileSpec>>(fileGroup, files));
			}

			if (files.Any((item) => item.Name == file.Name))
			{
				throw new ArgumentException("Data file must have unique logical name.");
			}
			files.Add(file);
		}

		public void AddLogFile(FileSpec file)
		{
			if (_logFiles.Any((item) => item.Name == file.Name))
			{
				throw new ArgumentException("Log file must have unique logical name.");
			}

			_logFiles.Add(file);
		}
	}

	public class ChangeDatabaseStatusParameters
	{
		public ChangeDatabaseStatusParameters(string name, bool isOnline)
		{
			Name = name;
			IsOnline = isOnline;
		}

		public string Name
		{
			get; }

		public bool IsOnline
		{
			get;
			private set;
		}
	}

	public class FileSpec
	{
		public string Name
		{
			get;
			set;
		}

		public string FileName
		{
			get;
			set;
		}

		public long Size
		{
			get;
			set;
		}

		public long MaxSize
		{
			get;
			set;
		}

		public long? ByteGrowth
		{
			get;
			set;
		}

		public double? PercentGrowth
		{
			get;
			set;
		}
	}
}
