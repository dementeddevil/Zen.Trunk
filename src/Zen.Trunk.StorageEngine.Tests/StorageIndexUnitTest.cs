using System;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Xunit;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Data.Index;
using Zen.Trunk.Storage.IO;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;

namespace Zen.Trunk.StorageEngine.Tests
{
    [Trait("Subsystem", "Storage Engine")]
    [Trait("Class", "Index Manager")]
    public class StorageIndexUnitTest
    {
        private class TestIndexManager : IndexManager<RootIndexInfo>
        {
            public TestIndexManager(ILifetimeScope parentLifetimeScope)
                : base(parentLifetimeScope)
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
            private BufferFieldUInt64 _logicalId;
            private BufferFieldInt64 _firstKey;
            private BufferFieldInt32 _secondKey;

            public TestIndexInfo()
            {
                _logicalId = new BufferFieldUInt64(base.LastField);
                _firstKey = new BufferFieldInt64(_logicalId);
                _secondKey = new BufferFieldInt32(_firstKey);
            }

            public TestIndexInfo(DateTime createdDate, int sequenceIndex, ulong logicalId)
            {
                LogicalId = logicalId;
                CreatedDate = createdDate;
                SequenceIndex = sequenceIndex;
            }

            public ulong LogicalId
            {
                get { return _logicalId.Value; }
                set { _logicalId.Value = value; }
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
                var tiRhs = (TestIndexInfo)rhs;
                var result = CreatedDate.CompareTo(tiRhs.CreatedDate);
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
                var lhs = IndexEntries[index];
                var rhs = new TestIndexInfo(key1, key2, 0);
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
                var createdDate = page.IndexEntries[0].CreatedDate;
                var sequenceIndex = page.IndexEntries[0].SequenceIndex;
                return new TestIndexInfo(createdDate, sequenceIndex, page.LogicalId.Value);
            }

            protected override TestIndexInfo CreateIndexEntry()
            {
                return new TestIndexInfo();
            }
        }

        private ILifetimeScope _parentServices;

        [Fact(DisplayName = "Index test add pages")]
        public async Task IndexTestAddPages()
        {
            using (var fileTracker = new TempFileTracker())
            {
                var masterDataPathName = fileTracker.Get("master.mddf");
                var masterLogPathName = fileTracker.Get("master.mlf");

                var dbDevice = CreateDatabaseDevice();
                try
                {
                    dbDevice.BeginTransaction(TimeSpan.FromMinutes(10));

                    var addFgDevice =
                        new AddFileGroupDeviceParameters(
                            FileGroupId.Primary,
                            "PRIMARY",
                            "master",
                            masterDataPathName,
                            DeviceId.Zero,
                            128,
                            true);
                    await dbDevice.AddFileGroupDevice(addFgDevice);

                    var addLogDevice =
                        new AddLogDeviceParameters(
                            "MASTER_LOG",
                            masterLogPathName,
                            DeviceId.Zero,
                            2);
                    await dbDevice.AddLogDevice(addLogDevice);

                    await dbDevice.OpenAsync(true);

                    await TrunkTransactionContext.Commit();

                    dbDevice.BeginTransaction(TimeSpan.FromMinutes(10));

                    var manager = new TestIndexManager(_parentServices);
                    var indexInfo = new RootIndexInfo
                    {
                        Name = "PK_Test",
                        OwnerObjectId = new ObjectId(100),
                        RootIndexDepth = 0,
                        IndexFileGroupId = addFgDevice.FileGroupId,
                    };
                    manager.CreateIndex(indexInfo);
                    //manager.

                    await TrunkTransactionContext.Commit();
                }
                finally
                {
                    await dbDevice.CloseAsync().ConfigureAwait(true);
                }
            }
        }

        private DatabaseDevice CreateDatabaseDevice()
        {
            var builder = new StorageEngineBuilder()
                .WithVirtualBufferFactory()
                .WithGlobalLockManager();
            //builder.RegisterType<MasterLogPageDevice>()
            //    .WithParameter("pathName", string.Empty);
            _parentServices = builder.Build();

            return new DatabaseDevice(_parentServices, DatabaseId.Zero);
        }
    }
}
