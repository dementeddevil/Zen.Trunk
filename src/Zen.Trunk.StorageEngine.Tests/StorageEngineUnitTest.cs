using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Xunit;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Data.Table;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;
using Zen.Trunk.VirtualMemory;
using Zen.Trunk.VirtualMemory.Tests;

namespace Zen.Trunk.Storage
{
    [Trait("Subsystem", "Storage Engine")]
    [Trait("Class", "Database Device")]
    public class StorageEngineUnitTest : IClassFixture<StorageEngineTestFixture>
    {
        private readonly StorageEngineTestFixture _fixture;

        public StorageEngineUnitTest(StorageEngineTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact(DisplayName = "Validate create database under transaction works as expected")]
        public async Task DatabaseCreateTxnTest()
        {
            using (var tracker = new TempFileTracker())
            {
                var masterDataPathName = tracker.Get("master.mddf");
                var masterLogPathName = tracker.Get("master.mlf");

                using (var dbDevice = new DatabaseDevice(StorageEngineTestFixture.PrimaryDatabaseId))
                {
                    dbDevice.InitialiseDeviceLifetimeScope(_fixture.Scope);

                    var addFgDevice =
                        new AddFileGroupDeviceParameters(
                            FileGroupId.Primary,
                            "PRIMARY",
                            "master",
                            masterDataPathName,
                            DeviceId.Zero,
                            128,
                            true);
                    await dbDevice.AddFileGroupDeviceAsync(addFgDevice).ConfigureAwait(true);

                    var addLogDevice =
                        new AddLogDeviceParameters(
                            "MASTER_LOG",
                            masterLogPathName,
                            DeviceId.Zero,
                            2);
                    await dbDevice.AddLogDeviceAsync(addLogDevice).ConfigureAwait(true);

                    await dbDevice.OpenAsync(true).ConfigureAwait(true);
                    Trace.WriteLine("DatabaseDevice.Open succeeded");

                    await dbDevice.CloseAsync().ConfigureAwait(true);
                    Trace.WriteLine("DatabaseDevice.Close succeeded");
                }
            }
        }

        [Fact(DisplayName = "Validate create database and streaming data into several pages under transaction works as expected")]
        public async Task DatabaseCreateStreamTxnTest()
        {
            using (var tracker = new TempFileTracker())
            {
                var masterDataPathName = tracker.Get("master.mddf");
                var masterLogPathName = tracker.Get("master.mlf");

                using (var dbDevice = new DatabaseDevice(StorageEngineTestFixture.PrimaryDatabaseId))
                {
                    dbDevice.InitialiseDeviceLifetimeScope(_fixture.Scope);
                    //dbDevice.BeginTransaction(); // transaction scope here is unnecessary as it is done inside open call on DatabaseDevice
                    bool rollback = false;
                    try
                    {
                        var addFgDevice =
                            new AddFileGroupDeviceParameters(
                                FileGroupId.Primary,
                                "PRIMARY",
                                "master",
                                masterDataPathName,
                                DeviceId.Zero,
                                128,
                                true);
                        await dbDevice.AddFileGroupDeviceAsync(addFgDevice).ConfigureAwait(true);

                        var addLogDevice =
                            new AddLogDeviceParameters(
                                "MASTER_LOG",
                                masterLogPathName,
                                DeviceId.Zero,
                                2);
                        await dbDevice.AddLogDeviceAsync(addLogDevice).ConfigureAwait(true);

                        await dbDevice.OpenAsync(true).ConfigureAwait(true);

                        //await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);

                        dbDevice.BeginTransaction();

                        // Open a file
                        using (var stream = new MemoryStream())
                        {
                            // Write 83k of random stuff
                            var random = new Random();
                            var buffer = new byte[1024];
                            for (int index = 0; index < 83; ++index)
                            {
                                random.NextBytes(buffer);
                                stream.Write(buffer, 0, buffer.Length);
                            }
                            stream.Flush();
                            stream.Position = 0;

                            // Determine page count
                            var byteSize = new ObjectDataPage().DataSize;
                            var pageCount = (int)(stream.Length / byteSize);
                            if ((stream.Length % byteSize) != 0)
                            {
                                ++pageCount;
                            }

                            buffer = new byte[byteSize];
                            Debug.WriteLine("Preparing to write {0} pages", pageCount);
                            ObjectDataPage lastPage = null;

                            var complete = false;
                            for (var pageIndex = 0; !complete; ++pageIndex)
                            {
                                Debug.WriteLine("Writing object page {0}", pageIndex);

                                // Create object data page
                                var objectPage = new ObjectDataPage();
                                objectPage.IsManagedData = false;
                                objectPage.ReadOnly = false;
                                objectPage.ObjectId = new ObjectId(1);
                                await objectPage.SetObjectLockAsync(ObjectLockType.IntentExclusive).ConfigureAwait(true);
                                await objectPage.SetPageLockAsync(DataLockType.Exclusive).ConfigureAwait(true);
                                objectPage.FileGroupId = FileGroupId.Primary;

                                // Create storage in database for new page
                                var initPageParams =
                                    new InitFileGroupPageParameters(
                                        null, objectPage, true, true, true, true);
                                await dbDevice.InitFileGroupPageAsync(initPageParams).ConfigureAwait(true);

                                // Update prev/next references
                                if (lastPage != null)
                                {
                                    lastPage.NextLogicalPageId = objectPage.LogicalPageId;
                                    objectPage.PrevLogicalPageId = lastPage.LogicalPageId;
                                }
                                lastPage = objectPage;

                                // Determine size of the data page and write blob
                                using (var pageStream = objectPage.CreateDataStream(false))
                                {
                                    var bytesRead = stream.Read(buffer, 0, (int)byteSize);
                                    if (bytesRead > 0)
                                    {
                                        pageStream.Write(buffer, 0, bytesRead);
                                    }
                                    else
                                    {
                                        complete = true;
                                    }

                                    if (bytesRead < byteSize)
                                    {
                                        complete = true;
                                    }
                                }

                                objectPage.SetDirtyState();
                                objectPage.Save();
                            }
                        }
                    }
                    catch
                    {
                        rollback = true;
                    }

                    if (rollback)
                    {
                        await TrunkTransactionContext.RollbackAsync().ConfigureAwait(true);
                    }
                    else
                    {
                        await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);
                    }

                    await dbDevice.CloseAsync().ConfigureAwait(true);
                }
            }
        }

        [Fact(DisplayName = "Validate that creating database table under transaction works as expected.")]
        public async Task DatabaseCreateTableTxnTest()
        {
            using (var tracker = new TempFileTracker())
            {
                var masterDataPathName = tracker.Get("master.mddf");
                var masterLogPathName = tracker.Get("master.mlf");

                using (var dbDevice = new DatabaseDevice(StorageEngineTestFixture.PrimaryDatabaseId))
                {
                    try
                    {
                        dbDevice.InitialiseDeviceLifetimeScope(_fixture.Scope);

                        var addFgDevice =
                            new AddFileGroupDeviceParameters(
                                FileGroupId.Primary,
                                "PRIMARY",
                                "master",
                                masterDataPathName,
                                DeviceId.Zero,
                                128,
                                true);
                        await dbDevice.AddFileGroupDeviceAsync(addFgDevice).ConfigureAwait(true);

                        var addLogDevice =
                            new AddLogDeviceParameters(
                                "MASTER_LOG",
                                masterLogPathName,
                                DeviceId.Zero,
                                2);
                        await dbDevice.AddLogDeviceAsync(addLogDevice).ConfigureAwait(true);

                        await dbDevice.OpenAsync(true).ConfigureAwait(true);

                        dbDevice.BeginTransaction();
                        try
                        {
                            var param =
                                new AddFileGroupTableParameters(
                                    addFgDevice.FileGroupId,
                                    addFgDevice.FileGroupName,
                                    "Test",
                                    new TableColumnInfo(
                                        "Id",
                                        TableColumnDataType.Int,
                                        false,
                                        0,
                                        1,
                                        1),
                                    new TableColumnInfo(
                                        "Name",
                                        TableColumnDataType.NVarChar,
                                        false,
                                        50),
                                    new TableColumnInfo(
                                        "SequenceIndex",
                                        TableColumnDataType.Int,
                                        false),
                                    new TableColumnInfo(
                                        "CreatedDate",
                                        TableColumnDataType.DateTime,
                                        false));
                            await dbDevice.AddFileGroupTableAsync(param).ConfigureAwait(true);

                            /*table.AddIndex(
                                new RootTableIndexInfo
                                {
                                    IndexFileGroupId = FileGroupDevice.Primary,
                                    IndexSubType = TableIndexSubType.Primary | TableIndexSubType.Unique,
                                    Name = "PK_Test",
                                    ObjectId = 12,
                                    ColumnIDs = new byte[] { 1 }
                                });*/

                            //table.EndColumnUpdate().Wait();

                            await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);
                        }
                        catch
                        {
                            await TrunkTransactionContext.RollbackAsync().ConfigureAwait(true);
                        }

                        // Insert some data
                        /*TrunkTransactionContext.BeginTransaction(dbDevice, TimeSpan.FromMinutes(5));
                        TrunkTransactionContext.Commit();*/
                    }
                    finally
                    {
                        await dbDevice.CloseAsync().ConfigureAwait(true);
                    }
                }
            }
        }

        [Fact(DisplayName = "Validate that creating database table and set of indicies under transaction works as expected.")]
        public async Task DatabaseCreateTableAndIndiciesTxnTest()
        {
            using (var tracker = new TempFileTracker())
            {
                var masterDataPathName = tracker.Get("master.mddf");
                var masterLogPathName = tracker.Get("master.mlf");

                using (var dbDevice = new DatabaseDevice(StorageEngineTestFixture.PrimaryDatabaseId))
                {
                    try
                    {
                        dbDevice.InitialiseDeviceLifetimeScope(_fixture.Scope);
                        var addFgDevice =
                            new AddFileGroupDeviceParameters(
                                FileGroupId.Primary,
                                "PRIMARY",
                                "master",
                                masterDataPathName,
                                DeviceId.Zero,
                                128,
                                true);
                        await dbDevice.AddFileGroupDeviceAsync(addFgDevice).ConfigureAwait(true);

                        var addLogDevice =
                            new AddLogDeviceParameters(
                                "MASTER_LOG",
                                masterLogPathName,
                                DeviceId.Zero,
                                2);
                        await dbDevice.AddLogDeviceAsync(addLogDevice).ConfigureAwait(true);

                        Trace.WriteLine("*** Database open ***");
                        await dbDevice.OpenAsync(true).ConfigureAwait(true);

                        // Create table schemas
                        Trace.WriteLine("*** Create table ***");
                        var objectId = ObjectId.Zero;
                        dbDevice.BeginTransaction();
                        try
                        {
                            var param =
                                new AddFileGroupTableParameters(
                                    addFgDevice.FileGroupId,
                                    addFgDevice.FileGroupName,
                                    "Test",
                                    new TableColumnInfo(
                                        "Id",
                                        TableColumnDataType.Int,
                                        false,
                                        0,
                                        1,
                                        1),
                                    new TableColumnInfo(
                                        "Name",
                                        TableColumnDataType.NVarChar,
                                        false,
                                        50),
                                    new TableColumnInfo(
                                        "SequenceIndex",
                                        TableColumnDataType.Int,
                                        false),
                                    new TableColumnInfo(
                                        "CreatedDate",
                                        TableColumnDataType.DateTime,
                                        false));
                            objectId = await dbDevice
                                .AddFileGroupTableAsync(param)
                                .ConfigureAwait(true);

                            await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);
                        }
                        catch
                        {
                            await TrunkTransactionContext.RollbackAsync().ConfigureAwait(true);
                        }

                        // Create table indices
                        Trace.WriteLine("*** Create indices ***");
                        dbDevice.BeginTransaction();
                        try
                        {
                            var primaryIndexParam =
                                    new AddFileGroupTableIndexParameters(
                                        addFgDevice.FileGroupId,
                                        addFgDevice.FileGroupName,
                                        "PK_Test",
                                        TableIndexSubType.Primary,
                                        objectId);
                            primaryIndexParam.AddColumnAndSortDirection(
                                "Id", TableIndexSortDirection.Ascending);
                            await dbDevice
                                .AddFileGroupTableIndexAsync(primaryIndexParam)
                                .ConfigureAwait(true);

                            var secondaryIndexParam =
                                new AddFileGroupTableIndexParameters(
                                    addFgDevice.FileGroupId,
                                    addFgDevice.FileGroupName,
                                    "IX_CreatedDate",
                                    TableIndexSubType.Primary,
                                    objectId);
                            secondaryIndexParam.AddColumnAndSortDirection(
                                "CreatedDate", TableIndexSortDirection.Descending);
                            await dbDevice
                                .AddFileGroupTableIndexAsync(secondaryIndexParam)
                                .ConfigureAwait(true);

                            await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);
                        }
                        catch
                        {
                            await TrunkTransactionContext.RollbackAsync().ConfigureAwait(true);
                        }

                        // Insert table data
                        //dbDevice.BeginTransaction();
                        //try
                        //{

                        //    await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);
                        //}
                        //catch (Exception e)
                        //{
                        //    await TrunkTransactionContext.RollbackAsync().ConfigureAwait(true);
                        //}
                    }
                    finally
                    {
                        await dbDevice.CloseAsync().ConfigureAwait(true);
                    }
                }
            }
        }

        [Fact(DisplayName = "Verify USE DATABASE statement creates shared database lock.")]
        public async Task UseDatabaseLockTest()
        {
            using (TrunkSessionContext.SwitchSessionContext(
                new TrunkSession(new SessionId(1), TimeSpan.FromSeconds(60))))
            {
                using (var tracker = new TempFileTracker())
                {
                    var masterDataPathName = tracker.Get("master.mddf");
                    var masterLogPathName = tracker.Get("master.mlf");

                    using (var dbDevice = new MasterDatabaseDevice())
                    {
                        dbDevice.InitialiseDeviceLifetimeScope(_fixture.Scope);

                        var addFgDevice =
                            new AddFileGroupDeviceParameters(
                                FileGroupId.Primary,
                                "PRIMARY",
                                "master",
                                masterDataPathName,
                                DeviceId.Zero,
                                128,
                                true);
                        await dbDevice.AddFileGroupDeviceAsync(addFgDevice).ConfigureAwait(true);

                        var addLogDevice =
                            new AddLogDeviceParameters(
                                "MASTER_LOG",
                                masterLogPathName,
                                DeviceId.Zero,
                                2);
                        await dbDevice.AddLogDeviceAsync(addLogDevice).ConfigureAwait(true);

                        await dbDevice.OpenAsync(true).ConfigureAwait(true);
                        Trace.WriteLine("DatabaseDevice.Open succeeded");

                        // This will acquire a session based lock
                        await dbDevice.UseDatabaseAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(true);

                        dbDevice.BeginTransaction();

                        var lockObject = dbDevice.LifetimeScope.Resolve<IDatabaseLockManager>().GetDatabaseLock();
                        var hasLock = await lockObject.HasLockAsync(DatabaseLockType.Shared).ConfigureAwait(true);
                        Assert.True(hasLock, "Expected to have shared database lock");

                        await dbDevice.CloseAsync().ConfigureAwait(true);
                        Trace.WriteLine("DatabaseDevice.Close succeeded");
                    }
                }


            }
        }

    }
}
