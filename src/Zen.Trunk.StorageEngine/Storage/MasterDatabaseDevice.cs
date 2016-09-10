// -----------------------------------------------------------------------
// <copyright file="MasterDatabaseDevice.cs" company="Zen Design Software">
// © Zen Design Software 2009 - 2016
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Zen.Trunk.Extensions;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;
using Zen.Trunk.Utils;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>MasterDatabaseDevice</c> represents the master database device.
    /// </summary>
    /// <remarks>
    /// This device contains core system tables and maintains links to all
    /// other databases defined on the instance.
    /// </remarks>
    /// <seealso cref="DatabaseDevice" />
    public class MasterDatabaseDevice : DatabaseDevice
    {
        #region Public Fields
        /// <summary>
        /// The reserved database names
        /// </summary>
        public static readonly string[] ReservedDatabaseNames =
        {
            StorageConstants.MasterDatabaseName,
            StorageConstants.TemporaryDatabaseName,
            StorageConstants.ModelDatabaseName
        };
        #endregion

        #region Private Fields
        private readonly Dictionary<string, DatabaseDevice> _userDatabases =
            new Dictionary<string, DatabaseDevice>(StringComparer.OrdinalIgnoreCase);
        private DatabaseId _nextDatabaseId = DatabaseId.FirstFree;
        private bool _hasAttachedMaster;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MasterDatabaseDevice"/> class.
        /// </summary>
        public MasterDatabaseDevice()
            : base(DatabaseId.Master)
        {
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the primary file group id.
        /// </summary>
        /// <value>The primary file group id.</value>
        protected override FileGroupId PrimaryFileGroupId => FileGroupId.Master;
        #endregion

        #region Public Methods
        /// <summary>
        /// Attaches the database.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// First database attached must be master database.
        /// or
        /// Master database has already been attached.
        /// or
        /// Database with same name already exists.
        /// </exception>
        public async Task AttachDatabaseAsync(AttachDatabaseParameters request)
        {
            // Determine whether we are attaching the master database
            var mountingMaster = false;
            DatabaseDevice device;
            if (!_hasAttachedMaster)
            {
                if (!request.Name.Equals(StorageConstants.MasterDatabaseName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("First database attached must be master database.");
                }

                mountingMaster = true;
                device = this;
            }
            else
            {
                if (request.Name.Equals(StorageConstants.MasterDatabaseName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Master database has already been attached.");
                }

                // Create new database device
                var dbId = _nextDatabaseId;
                _nextDatabaseId = _nextDatabaseId.Next;
                device = GetService<DatabaseDevice>(new NamedParameter("dbId", dbId));
            }

            // Create transaction context for the new database device
            //TrunkTransactionContext.BeginTransaction(device, TimeSpan.FromMinutes(1));

            // Check database name is unique
            if (_userDatabases.ContainsKey(request.Name))
            {
                throw new ArgumentException("Database with same name already exists.");
            }

            // Setup file defaults
            if (request.IsCreate)
            {
                if (request.FileGroups.Count == 0)
                {
                    request.AddDataFile(
                        StorageConstants.PrimaryFileGroupName,
                        new FileSpec
                        {
                            Name = request.Name,
                            FileName = Path.Combine(
                                GetService<StorageEngineConfiguration>().DefaultDataFilePath,
                                $"{request.Name}{StorageConstants.DataFilenameSuffix}{StorageConstants.PrimaryDeviceFileExtension}"),
                            Size = new FileSize(1, FileSize.FileSizeUnit.MegaBytes),
                            FileGrowth = new FileSize(1, FileSize.FileSizeUnit.MegaBytes)
                        });
                }
                if (request.LogFiles.Count == 0)
                {
                    request.AddLogFile(
                        new FileSpec
                        {
                            Name = request.Name,
                            FileName = Path.Combine(
                                GetService<StorageEngineConfiguration>().DefaultLogFilePath,
                                $"{request.Name}{StorageConstants.LogFilenameSuffix}{StorageConstants.MasterLogFileDeviceExtension}"),
                            Size = new FileSize(1, FileSize.FileSizeUnit.MegaBytes),
                            FileGrowth = new FileSize(1, FileSize.FileSizeUnit.MegaBytes)
                        });
                }
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

            // Process the primary filegroup
            var primaryFileGroup = request.FileGroups[StorageConstants.PrimaryFileGroupName];
            var deviceId = DeviceId.Primary;
            foreach (var file in primaryFileGroup)
            {
                await AttachDatabaseFileGroupDeviceAsync(
                    request,
                    file,
                    pageSize,
                    deviceId == DeviceId.Primary,
                    deviceId == DeviceId.Primary,
                    device,
                    StorageConstants.PrimaryFileGroupName,
                    deviceId).ConfigureAwait(false);

                // Advance to next device
                deviceId = deviceId.Next;
            }

            // Process any secondary filegroups
            foreach (var fileGroup in request.FileGroups.Where(f => f.Key != StorageConstants.PrimaryFileGroupName))
            {
                deviceId = DeviceId.Primary;
                foreach (var file in fileGroup.Value.Where(f => f.Name != StorageConstants.PrimaryFileGroupName))
                {
                    await AttachDatabaseFileGroupDeviceAsync(
                        request,
                        file,
                        pageSize,
                        deviceId == DeviceId.Primary,
                        false,
                        device,
                        fileGroup.Key,
                        deviceId).ConfigureAwait(false);

                    // Advance to next device
                    deviceId = deviceId.Next;
                }
            }

            // Walk the list of log files
            foreach (var file in request.LogFiles)
            {
                var pageCount = file.Size?.GetSizeAsPages(pageSize) ?? 0;

                var deviceParams = new AddLogDeviceParameters(
                    file.Name, file.FileName, DeviceId.Zero, pageCount);
                await device.AddLogDeviceAsync(deviceParams).ConfigureAwait(false);
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
                await masterRootPage.SetRootLockAsync(RootLockType.Shared).ConfigureAwait(false);

                // Load page from root device
                await LoadFileGroupPageAsync(
                    new LoadFileGroupPageParameters(null, masterRootPage, true)).ConfigureAwait(false);

                // Add this database information to the database list
                masterRootPage.ReadOnly = false;
                await masterRootPage.SetRootLockAsync(RootLockType.Update).ConfigureAwait(false);
                await masterRootPage.SetRootLockAsync(RootLockType.Exclusive).ConfigureAwait(false);
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

        /// <summary>
        /// Detaches the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// System databases are always online.
        /// or
        /// Database not found.
        /// </exception>
        public Task DetachDatabaseAsync(string name)
        {
            // Check for reserved database names
            foreach (var reserved in ReservedDatabaseNames)
            {
                if (string.Equals(reserved, name, StringComparison.OrdinalIgnoreCase))
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

        /// <summary>
        /// Changes the database status.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// System databases are always online.
        /// or
        /// Database not found.
        /// </exception>
        public Task ChangeDatabaseStatusAsync(ChangeDatabaseStatusParameters request)
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

        /// <summary>
        /// Gets the database device.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Database not found</exception>
        public DatabaseDevice GetDatabaseDevice(string databaseName)
        {
            if (string.Equals(databaseName, StorageConstants.MasterDatabaseName, StringComparison.OrdinalIgnoreCase))
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
        /// <summary>
        /// Builds the device lifetime scope.
        /// </summary>
        /// <param name="builder">The builder.</param>
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
        protected override async Task OnOpenAsync()
        {
            // Perform base class mounting
            await base.OnOpenAsync().ConfigureAwait(false);

            // If we are in non-create mode then we need to mount any other
            //	online databases
            if (!IsCreate)
            {
                // Load the master database primary file-group root page
                var masterRootPage = new MasterDatabasePrimaryFileGroupRootPage();
                await masterRootPage.SetRootLockAsync(RootLockType.Shared).ConfigureAwait(false);

                // Load page from root device
                await LoadFileGroupPageAsync(
                    new LoadFileGroupPageParameters(null, masterRootPage, true)).ConfigureAwait(false);

                // Walk the list of databases in the root page
                // NOTE: We exclude offline devices
                foreach (var deviceInfo in masterRootPage
                    .GetDatabaseEnumerator()
                    .Where(item => item.IsOnline))
                {
                    // Create attach request and post - no need to wait...
                    var attach = new AttachDatabaseParameters(deviceInfo.Name);
                    attach.AddDataFile(
                        "PRIMARY",
                        new FileSpec
                        {
                            Name = deviceInfo.PrimaryName,
                            FileName = deviceInfo.PrimaryFilePathName
                        });
                    await AttachDatabaseAsync(attach).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Called when closing the device.
        /// </summary>
        /// <returns></returns>
        protected override async Task OnCloseAsync()
        {
            // TODO: Make sure we close TEMPDB last

            // Close user databases first
            await TaskExtra
                .WhenAllOrEmpty(_userDatabases.Values
                    .Select(device => device.CloseAsync())
                    .ToArray())
                .ConfigureAwait(false);

            // Finally close the master device
            await base.OnCloseAsync().ConfigureAwait(false);
        }
        #endregion

        #region Private Methods
        private static async Task AttachDatabaseFileGroupDeviceAsync(
            AttachDatabaseParameters request,
            FileSpec file,
            uint pageSize,
            bool mountingPrimary,
            bool needToCreateMasterFilegroup,
            DatabaseDevice device,
            string fileGroupName,
            DeviceId deviceId)
        {
            // Determine number of pages to use if we are creating devices
            uint createPageCount = 0;
            if (request.IsCreate)
            {
                createPageCount =
                    file.Size?.GetSizeAsPages(pageSize) ?? 0;
            }

            if (mountingPrimary)
            {
                if (needToCreateMasterFilegroup)
                {
                    await device
                        .AddFileGroupDeviceAsync(
                            new AddFileGroupDeviceParameters(
                                FileGroupId.Master,
                                fileGroupName,
                                file.Name,
                                file.FileName,
                                deviceId,
                                createPageCount))
                        .ConfigureAwait(false);
                }
                else
                {
                    await device
                        .AddFileGroupDeviceAsync(
                            new AddFileGroupDeviceParameters(
                                FileGroupId.Primary,
                                fileGroupName,
                                file.Name,
                                file.FileName,
                                deviceId,
                                createPageCount))
                        .ConfigureAwait(false);
                }
            }
            else
            {
                await device
                    .AddFileGroupDeviceAsync(
                        new AddFileGroupDeviceParameters(
                            FileGroupId.Invalid,
                            fileGroupName,
                            file.Name,
                            file.FileName,
                            deviceId,
                            createPageCount))
                    .ConfigureAwait(false);
            }
        }
        #endregion
    }
}
