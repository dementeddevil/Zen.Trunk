// -----------------------------------------------------------------------
// <copyright file="MasterDatabaseDevice.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zen.Trunk.Storage.IO;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;
using Autofac;

namespace Zen.Trunk.Storage.Data
{

	public class MasterDatabaseDevice : DatabaseDevice
	{
		#region Public Fields
		public static readonly string[] ReservedDatabaseNames =
			new string[] { "MASTER", "TEMPDB" };
		#endregion

		#region Private Fields
		private readonly Dictionary<string, DatabaseDevice> _userDatabases =
			new Dictionary<string, DatabaseDevice>(StringComparer.OrdinalIgnoreCase);
		private DatabaseId _nextDatabaseId;
		private bool _hasAttachedMaster;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MasterDatabaseDevice"/> class.
		/// </summary>
		/// <param name="parentLifetimeScope">The parent lifetime scope.</param>
		/// <remarks>
		/// The parent lifetime scope must be able to resolve the following interfaces;
		/// 1. IVirtualBufferFactory
		/// </remarks>
		public MasterDatabaseDevice(ILifetimeScope parentLifetimeScope)
			: base(parentLifetimeScope, DatabaseId.Zero)
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
				var dbId = _nextDatabaseId = _nextDatabaseId.Next;
				device = ResolveDeviceService<DatabaseDevice>(
                    new NamedParameter("dbId", dbId));
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
			    var primary = fileGroup.Value.FirstOrDefault(f => f.Name == "PRIMARY");

				foreach (var file in fileGroup.Value.Where(f=>f.Name != "PRIMARY"))
				{
					await AttachDatabaseFileGroupDeviceAsync(request, file, pageSize, mountingPrimary, needToCreateMasterFilegroup, device, fileGroup, deviceId);

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

	    private static async Task AttachDatabaseFileGroupDeviceAsync(
            AttachDatabaseParameters request,
            FileSpec file,
            uint pageSize,
	        bool mountingPrimary,
            bool needToCreateMasterFilegroup,
            DatabaseDevice device,
            KeyValuePair<string, IList<FileSpec>> fileGroup,
	        DeviceId deviceId)
	    {
	        string primaryName;
	        string primaryFileName;
// Determine number of pages to use if we are creating devices
	        uint createPageCount = 0;
	        if (request.IsCreate)
	        {
	            createPageCount = (uint) (file.Size/pageSize);
	        }

	        if (mountingPrimary)
	        {
	            if (needToCreateMasterFilegroup)
	            {
	                await device
	                    .AddFileGroupDevice(
	                        new AddFileGroupDeviceParameters(
	                            FileGroupId.Master,
	                            fileGroup.Key,
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
	                            fileGroup.Key,
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
	                        fileGroup.Key,
	                        file.Name,
	                        file.FileName,
	                        deviceId,
	                        createPageCount))
	                .ConfigureAwait(false);
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

	    public DatabaseDevice GetDatabaseDevice(string databaseName)
	    {
	        if (string.Equals(databaseName, "master", StringComparison.OrdinalIgnoreCase))
	        {
	            return this;
	        }

	        if (!_userDatabases.ContainsKey(databaseName))
	        {
	            throw new ArgumentException("Database not found", nameof(databaseName));
	        }

	        return _userDatabases[databaseName];
	    }
		#endregion

		#region Protected Methods

	    protected override void BuildDeviceLifetimeScope(ContainerBuilder builder)
	    {
	        base.BuildDeviceLifetimeScope(builder);

	        builder.RegisterType<GlobalLockManager>().As<IGlobalLockManager>().SingleInstance();
	        builder.RegisterType<DatabaseDevice>().AsSelf();
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
			        new MasterDatabasePrimaryFileGroupRootPage
			        {
			            RootLock = RootLockType.Shared
			        };

			    // Load page from root device
				await LoadFileGroupPage(
					new LoadFileGroupPageParameters(null, masterRootPage, true)).ConfigureAwait(false);

				// Walk the list of databases in the root page
				// NOTE: We exclude offline devices
				foreach (var deviceInfo in masterRootPage
					.GetDatabaseEnumerator()
					.Where(item => item.IsOnline))
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
					await AttachDatabase(attach).ConfigureAwait(false);
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
			await TaskExtra
				.WhenAllOrEmpty(_userDatabases.Values
                    .Select(device => device.CloseAsync())
                    .ToArray())
				.ConfigureAwait(false);

			// Finally close the master device
			await base.OnClose().ConfigureAwait(false);
		}
		#endregion
	}

	public class AttachDatabaseParameters
	{
		private readonly IDictionary<string, IList<FileSpec>> _fileGroups =
			new Dictionary<string, IList<FileSpec>>(StringComparer.OrdinalIgnoreCase);
		private readonly List<FileSpec> _logFiles = new List<FileSpec>();

		public string Name { get; set; }

		public bool IsCreate { get; set; }

        public bool HasPrimaryFileGroup => _fileGroups.ContainsKey("PRIMARY");

	    public IDictionary<string, IList<FileSpec>> FileGroups => new ReadOnlyDictionary<string, IList<FileSpec>>(_fileGroups);

	    public ICollection<FileSpec> LogFiles => _logFiles.AsReadOnly();

	    public void AddDataFile(string fileGroup, FileSpec file)
	    {
            // Find or create filegroup entry
	        IList<FileSpec> files;
	        if (!_fileGroups.TryGetValue(fileGroup, out files))
	        {
                files = new List<FileSpec>();
	            _fileGroups.Add(fileGroup, files);
	        }

            // Validate files have unique filename
			if (files.Any(item => string.Equals(item.FileName, file.FileName, StringComparison.OrdinalIgnoreCase)))
			{
				throw new ArgumentException("Data file must have unique filename.");
			}

            // Validate files have unique name
            if (files.Any(item => string.Equals(item.Name, file.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Data file must have unique logical name.");
            }

            files.Add(file);
		}

		public void AddLogFile(FileSpec file)
		{
            // Validate files have unique filename
            if (_logFiles.Any(item => string.Equals(item.FileName, file.FileName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Log file must have unique filename.");
            }

            // Validate files have unique name
            if (_logFiles.Any(item => string.Equals(item.Name, file.Name, StringComparison.OrdinalIgnoreCase)))
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

		public string Name { get; }

		public bool IsOnline { get; }
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
