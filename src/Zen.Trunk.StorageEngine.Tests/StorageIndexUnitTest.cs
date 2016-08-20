using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Data.Index;
using Zen.Trunk.Storage.IO;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;

namespace Zen.Trunk.StorageEngine.Tests
{
	[TestClass]
	public class StorageIndexUnitTest
	{
		private class TestIndexManager : IndexManager<RootIndexInfo>
		{
			public TestIndexManager(IServiceProvider parentProvider)
				: base(parentProvider)
			{
			}

			public void CreateIndex(RootIndexInfo rootIndexInfo)
			{
				// Create the root index page
				var rootPage = new TestIndexPage();
				rootPage.FileGroupId = rootIndexInfo.IndexFileGroupId;
				rootPage.ObjectId = rootIndexInfo.OwnerObjectId;
				rootPage.IndexType = IndexType.Root | IndexType.Leaf;
				this.Database.InitFileGroupPage(
					new InitFileGroupPageParameters(null, rootPage, true, false, true, true)).ConfigureAwait(false);

				// Setup root index page
				rootPage.SetHeaderDirty();
				AddIndexInfo(rootIndexInfo);
			}
		}

		private class TestIndexInfo : IndexInfo
		{
			private BufferFieldInt64 _firstKey;
			private BufferFieldInt32 _secondKey;

			public TestIndexInfo()
			{
				_firstKey = new BufferFieldInt64(base.LastField);
				_secondKey = new BufferFieldInt32(_firstKey);
			}

			public TestIndexInfo(DateTime createdDate, int sequenceIndex, ulong logicalId)
				: base(logicalId)
			{
				CreatedDate = createdDate;
				SequenceIndex = sequenceIndex;
			}

			public DateTime CreatedDate
			{
				get
				{
					return new DateTime(_firstKey.Value);
				}
				set
				{
					if (value.Ticks != _firstKey.Value)
					{
						_firstKey.Value = value.Ticks;
					}
				}
			}

			public int SequenceIndex
			{
				get
				{
					return _secondKey.Value;
				}
				set
				{
					if (_secondKey.Value != value)
					{
						_secondKey.Value = value;
					}
				}
			}

			protected override BufferField LastField
			{
				get
				{
					return _secondKey;
				}
			}

			public override int CompareTo(IndexInfo rhs)
			{
				TestIndexInfo tiRhs = (TestIndexInfo)rhs;
				int result = CreatedDate.CompareTo(tiRhs.CreatedDate);
				if (result != 0)
				{
					result = SequenceIndex.CompareTo(tiRhs.SequenceIndex);
				}
				return result;
			}

			protected override void DoRead(BufferReaderWriter streamManager)
			{
				base.DoRead(streamManager);
				_firstKey.Read(streamManager);
				_secondKey.Read(streamManager);
			}

			protected override void DoWrite(BufferReaderWriter streamManager)
			{
				base.DoWrite(streamManager);
				_firstKey.Write(streamManager);
				_secondKey.Write(streamManager);
			}
		}

		private class TestIndexPage : IndexPage<TestIndexInfo, RootIndexInfo>
		{
			public TestIndexPage()
			{
			}

			public int CompareIndex(int index, DateTime key1, int key2)
			{
				TestIndexInfo lhs = IndexEntries[index];
				TestIndexInfo rhs = new TestIndexInfo(key1, key2, 0);
				return lhs.CompareTo(rhs);
			}

			public ushort KeySize
			{
				get
				{
					return 12;
				}
			}

			public override ushort MaxIndexEntries
			{
				get
				{
					return (ushort)(DataSize / (2 + KeySize));
				}
			}

			public override IndexManager IndexManager
			{
				get
				{
					return GetService<TestIndexManager>();
				}
			}

			protected override TestIndexInfo CreateLinkToPage(
				IndexPage<TestIndexInfo, RootIndexInfo> page)
			{
				DateTime createdDate = page.IndexEntries[0].CreatedDate;
				int sequenceIndex = page.IndexEntries[0].SequenceIndex;
				return new TestIndexInfo(createdDate, sequenceIndex, page.LogicalId);
			}

			protected override TestIndexInfo CreateIndexEntry()
			{
				return new TestIndexInfo();
			}
		}

		private static TestContext _testContext;
		private ServiceContainer _parentServices;

		[ClassInitialize]
		public static void TestInitialize(TestContext context)
		{
			_testContext = context;

			StorageEngineBootstrapper bootstrapper = new StorageEngineBootstrapper();
			bootstrapper.Run();
		}

		[TestMethod]
		public async Task IndexTestAddPages()
		{
			DatabaseDevice dbDevice = CreateDatabaseDevice();

			string masterDataPathName =
				Path.Combine(_testContext.TestDir, "master.mddf");
			string masterLogPathName =
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

			TestIndexManager manager = new TestIndexManager(_parentServices);
			var indexInfo = new RootIndexInfo
				{
					Name = "PK_Test",
					OwnerObjectId = 100,
					RootIndexDepth = 0,
					IndexFileGroupId = addFgDevice.FileGroupId,
				};
			manager.CreateIndex(indexInfo);
			//manager.

			await TrunkTransactionContext.Commit();
		}

		private DatabaseDevice CreateDatabaseDevice()
		{
			// We need a service provider to pass to the database device
			//	otherwise it will fail to initialise
			_parentServices = new ServiceContainer();
			_parentServices.AddService(typeof(IVirtualBufferFactory), new VirtualBufferFactory(32, 8192));
			_parentServices.AddService(typeof(GlobalLockManager), new GlobalLockManager());
			return new DatabaseDevice(0, _parentServices);
		}
	}
}
