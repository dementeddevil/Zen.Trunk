using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Data.Table;
using Zen.Trunk.Storage.IO;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;
using Autofac;
using Xunit;

namespace Zen.Trunk.StorageEngine.Tests
{
	[Trait("Subsystem", "Storage Engine")]
    [Trait("Class", "Database Device")]
	public class StorageEngineUnitTest
	{
	    private ILifetimeScope _lifetimeScope;

	    ~StorageEngineUnitTest()
	    {
	        _lifetimeScope.Dispose();
	    }

		[Fact(DisplayName = "Validate create database under transaction works as expected")]
		public async Task DatabaseCreateTxnTest()
		{
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var masterDataPathName = Path.Combine(assemblyLocation, "master.mddf");
            var masterLogPathName = Path.Combine(assemblyLocation, "master.mlf");
            try
            {
                var dbDevice = CreateDatabaseDevice();
                try
                {
                    dbDevice.BeginTransaction();

			        var addFgDevice =
				        new AddFileGroupDeviceParameters(
					        FileGroupId.Primary,
					        "PRIMARY",
					        "master",
					        masterDataPathName,
                            DeviceId.Zero,
					        128,
					        true);
			        await dbDevice.AddFileGroupDevice(addFgDevice).ConfigureAwait(true);

			        var addLogDevice =
				        new AddLogDeviceParameters(
					        "MASTER_LOG",
					        masterLogPathName,
                            DeviceId.Zero,
					        2);
			        await dbDevice.AddLogDevice(addLogDevice).ConfigureAwait(true);

                    await dbDevice.OpenAsync(true).ConfigureAwait(true);
                    Trace.WriteLine("DatabaseDevice.Open succeeded");

			        await TrunkTransactionContext.Commit().ConfigureAwait(true);
                    Trace.WriteLine("Transaction commit succeeded");
                }
		        finally
		        {
		            await dbDevice.CloseAsync().ConfigureAwait(true);
                    Trace.WriteLine("DatabaseDevice.Close succeeded");

                    dbDevice.Dispose();
		            dbDevice = null;
		        }
            }
		    finally
		    {
		        if (File.Exists(masterDataPathName))
		        {
		            File.Delete(masterDataPathName);
		        }
		        if (File.Exists(masterLogPathName))
		        {
		            File.Delete(masterLogPathName);
		        }
		    }
		}

        [Fact(DisplayName = "Validate create database and streaming data into several pages under transaction works as expected")]
        public async Task DatabaseCreateStreamTxnTest()
		{
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var masterDataPathName = Path.Combine(assemblyLocation, "master.mddf");
            var masterLogPathName = Path.Combine(assemblyLocation, "master.mlf");
            try
            {
                var dbDevice = CreateDatabaseDevice();
                try
                {
                    dbDevice.BeginTransaction();

                    var addFgDevice =
				        new AddFileGroupDeviceParameters(
					        FileGroupId.Primary,
					        "PRIMARY",
					        "master",
					        masterDataPathName,
                            DeviceId.Zero,
					        128,
					        true);
			        await dbDevice.AddFileGroupDevice(addFgDevice).ConfigureAwait(true);

			        var addLogDevice =
				        new AddLogDeviceParameters(
					        "MASTER_LOG",
					        masterLogPathName,
                            DeviceId.Zero,
					        2);
			        await dbDevice.AddLogDevice(addLogDevice).ConfigureAwait(true);

			        await dbDevice.OpenAsync(true).ConfigureAwait(true);

			        await TrunkTransactionContext.Commit().ConfigureAwait(true);

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
					        objectPage.ObjectLock = ObjectLockType.IntentExclusive;
					        objectPage.PageLock = DataLockType.Exclusive;
					        objectPage.FileGroupId = FileGroupId.Primary;

					        // Create storage in database for new page
					        var initPageParams =
						        new InitFileGroupPageParameters(
							        null, objectPage, true, true, true, true);
					        await dbDevice.InitFileGroupPage(initPageParams).ConfigureAwait(true);

					        // Update prev/next references
					        if (lastPage != null)
					        {
						        lastPage.NextLogicalId = objectPage.LogicalId;
						        objectPage.PrevLogicalId = lastPage.LogicalId;
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

			        await TrunkTransactionContext.Commit().ConfigureAwait(true);
                }
                finally
                {
                    await dbDevice.CloseAsync().ConfigureAwait(true);
                    dbDevice.Dispose();
                    dbDevice = null;
                }
            }
            finally
            {
                if (File.Exists(masterDataPathName))
                {
                    File.Delete(masterDataPathName);
                }
                if (File.Exists(masterLogPathName))
                {
                    File.Delete(masterLogPathName);
                }
            }
        }

        [Fact(DisplayName = "Validate that creating database table under transaction works as expected.")]
		public async Task DatabaseCreateTableTxnTest()
		{
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var masterDataPathName = Path.Combine(assemblyLocation, "master.mddf");
			var masterLogPathName = Path.Combine(assemblyLocation, "master.mlf");
		    try
		    {
                var dbDevice = CreateDatabaseDevice();
		        try
		        {
                    dbDevice.BeginTransaction();

                    var addFgDevice =
				        new AddFileGroupDeviceParameters(
					        FileGroupId.Primary,
					        "PRIMARY",
					        "master",
					        masterDataPathName,
                            DeviceId.Zero,
					        128,
					        true);
			        await dbDevice.AddFileGroupDevice(addFgDevice).ConfigureAwait(true);

			        var addLogDevice =
				        new AddLogDeviceParameters(
					        "MASTER_LOG",
					        masterLogPathName,
                            DeviceId.Zero,
					        2);
			        await dbDevice.AddLogDevice(addLogDevice).ConfigureAwait(true);

			        await dbDevice.OpenAsync(true).ConfigureAwait(true);

			        await TrunkTransactionContext.Commit().ConfigureAwait(true);

                    dbDevice.BeginTransaction();

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
			        await dbDevice.AddFileGroupTable(param).ConfigureAwait(true);

			        /*table.AddIndex(
				        new RootTableIndexInfo
				        {
					        IndexFileGroupId = FileGroupDevice.Primary,
					        IndexSubType = TableIndexSubType.Primary | TableIndexSubType.Unique,
					        Name = "PK_Test",
					        OwnerObjectId = 12,
					        ColumnIDs = new byte[] { 1 }
				        });*/

			        //table.EndColumnUpdate().Wait();

			        await TrunkTransactionContext.Commit().ConfigureAwait(true);

			        // Insert some data
			        /*TrunkTransactionContext.BeginTransaction(dbDevice, TimeSpan.FromMinutes(5));



			        TrunkTransactionContext.Commit();*/
		        }
		        finally
		        {
		            await dbDevice.CloseAsync().ConfigureAwait(true);
		            dbDevice.Dispose();
		            dbDevice = null;
		        }
		    }
		    finally
		    {
		        if (File.Exists(masterDataPathName))
		        {
		            File.Delete(masterDataPathName);
		        }
		        if (File.Exists(masterLogPathName))
		        {
		            File.Delete(masterLogPathName);
		        }
		    }
		}

		private DatabaseDevice CreateDatabaseDevice()
		{
            var builder = new StorageEngineBuilder()
                .WithVirtualBufferFactory()
                .WithGlobalLockManager();
		    _lifetimeScope = builder.Build();

			return new DatabaseDevice(_lifetimeScope, DatabaseId.Zero);
		}
	}
}
