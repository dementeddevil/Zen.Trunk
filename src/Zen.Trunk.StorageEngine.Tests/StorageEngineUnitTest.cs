namespace Zen.Trunk.StorageEngine.Tests
{
	using System;
	using System.ComponentModel.Design;
	using System.Diagnostics;
	using System.IO;
	using System.Threading.Tasks;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Zen.Trunk.Storage;
	using Zen.Trunk.Storage.Data;
	using Zen.Trunk.Storage.Data.Table;
	using Zen.Trunk.Storage.IO;
	using Zen.Trunk.Storage.Locking;
	using Zen.Trunk.Storage.Log;

	[TestClass]
	public class StorageEngineUnitTest
	{
		private static TestContext _testContext;

		[ClassInitialize]
		public static void TestInitialize(TestContext context)
		{
			_testContext = context;

			StorageEngineBootstrapper bootstrapper = new StorageEngineBootstrapper();
			bootstrapper.Run();
		}

		[TestMethod]
		[TestCategory("Storage Engine: Database Device")]
		public async Task DatabaseCreateTxnTest()
		{
			var dbDevice = CreateDatabaseDevice();

			var masterDataPathName =
				Path.Combine(_testContext.TestDir, "master.mddf");
			var masterLogPathName =
				Path.Combine(_testContext.TestDir, "master.mlf");

			TrunkTransactionContext.BeginTransaction(dbDevice, TimeSpan.FromMinutes(1));

			var addFgDevice =
				new AddFileGroupDeviceParameters(
					FileGroupDevice.Primary,
					"PRIMARY",
					"master",
					masterDataPathName,
					128,
					0,
					true);
			await dbDevice.AddFileGroupDevice(addFgDevice);

			var addLogDevice =
				new AddLogDeviceParameters(
					"MASTER_LOG",
					masterLogPathName,
					2);
			await dbDevice.AddLogDevice(addLogDevice);

			await dbDevice.Open(true);
			Trace.WriteLine("DatabaseDevice.Open succeeded");

			await TrunkTransactionContext.Commit();
			Trace.WriteLine("Transaction commit succeeded");

			await dbDevice.Close();
			Trace.WriteLine("DatabaseDevice.Close succeeded");
		}

		[TestMethod]
		[TestCategory("Storage Engine: Database Device")]
		public async Task DatabaseCreateStreamTxnTest()
		{
			var dbDevice = CreateDatabaseDevice();

			var masterDataPathName =
				Path.Combine(_testContext.TestDir, "master.mddf");
			var masterLogPathName =
				Path.Combine(_testContext.TestDir, "master.mlf");

			TrunkTransactionContext.BeginTransaction(dbDevice, TimeSpan.FromMinutes(1));

			var addFgDevice =
				new AddFileGroupDeviceParameters(
					FileGroupDevice.Primary,
					"PRIMARY",
					"master",
					masterDataPathName,
					128,
					0,
					true);
			await dbDevice.AddFileGroupDevice(addFgDevice);

			var addLogDevice =
				new AddLogDeviceParameters(
					"MASTER_LOG",
					masterLogPathName,
					2);
			await dbDevice.AddLogDevice(addLogDevice);

			await dbDevice.Open(true);

			await TrunkTransactionContext.Commit();

			TrunkTransactionContext.BeginTransaction(dbDevice, TimeSpan.FromMinutes(5));

			// Open a file
			using (var stream = new FileStream(
				Path.Combine(_testContext.TestDir,
				@"..\..\Zen.Trunk.VirtualMemory\Storage\IO\AdvancedFileStream.cs"),	// 83kb plain text
				FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				var byteSize = new ObjectDataPage().DataSize;
				var pageCount = (int)(stream.Length / byteSize);
				if ((stream.Length % byteSize) != 0)
				{
					++pageCount;
				}

				var buffer = new byte[byteSize];
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
					objectPage.ObjectId = 1;
					objectPage.ObjectLock = ObjectLockType.IntentExclusive;
					objectPage.PageLock = DataLockType.Exclusive;
					objectPage.FileGroupId = FileGroupDevice.Primary;

					// Create storage in database for new page
					var initPageParams =
						new InitFileGroupPageParameters(
							null, objectPage, true, true, true, true);
					await dbDevice.InitFileGroupPage(initPageParams);

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

			await TrunkTransactionContext.Commit();

			await dbDevice.Close();
		}

		[TestMethod]
		[TestCategory("Storage Engine: Database Device")]
		public async Task DatabaseCreateTableTxnTest()
		{
			var dbDevice = CreateDatabaseDevice();

			var masterDataPathName =
				Path.Combine(_testContext.TestDir, "master.mddf");
			var masterLogPathName =
				Path.Combine(_testContext.TestDir, "master.mlf");

			TrunkTransactionContext.BeginTransaction(dbDevice, TimeSpan.FromMinutes(1));

			var addFgDevice =
				new AddFileGroupDeviceParameters(
					FileGroupDevice.Primary,
					"PRIMARY",
					"master",
					masterDataPathName,
					128,
					0,
					true);
			await dbDevice.AddFileGroupDevice(addFgDevice);

			var addLogDevice =
				new AddLogDeviceParameters(
					"MASTER_LOG",
					masterLogPathName,
					2);
			await dbDevice.AddLogDevice(addLogDevice);

			await dbDevice.Open(true);

			await TrunkTransactionContext.Commit();

			TrunkTransactionContext.BeginTransaction(dbDevice, TimeSpan.FromMinutes(5));

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
			await dbDevice.AddFileGroupTable(param);

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

			await TrunkTransactionContext.Commit();

			// Insert some data
			/*TrunkTransactionContext.BeginTransaction(dbDevice, TimeSpan.FromMinutes(5));



			TrunkTransactionContext.Commit();*/

			await dbDevice.Close();
		}

		private DatabaseDevice CreateDatabaseDevice()
		{
			// We need a service provider to pass to the database device
			//	otherwise it will fail to initialise
			var parentServices = new ServiceContainer();
			parentServices.AddService(typeof(IVirtualBufferFactory), new VirtualBufferFactory(32, 8192));
			parentServices.AddService(typeof(GlobalLockManager), new GlobalLockManager());
			return new DatabaseDevice(0, parentServices);
		}
	}
}
