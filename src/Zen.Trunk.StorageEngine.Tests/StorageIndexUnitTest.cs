﻿using System;
using System.Threading.Tasks;
using Autofac;
using Serilog;
using Xunit;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Storage.Data.Index;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Logging;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    [Trait("Subsystem", "Storage Engine")]
    [Trait("Class", "Index Manager")]
    public class StorageIndexUnitTest : IClassFixture<StorageEngineTestFixture>, IDisposable
    {
        private readonly ILogger _logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .WriteTo.Debug()
            .CreateLogger();
        private readonly StorageEngineTestFixture _fixture;
        private readonly ILifetimeScope _scope;

        private class TestIndexManager : IndexManager<RootIndexInfo>
        {
            public TestIndexManager(ILifetimeScope parentLifetimeScope)
                : base(parentLifetimeScope)
            {
            }

            public void CreateIndex(RootIndexInfo rootIndexInfo)
            {
                // Create the root index page
                var rootPage =
                    new TestIndexPage
                    {
                        FileGroupId = rootIndexInfo.IndexFileGroupId,
                        ObjectId = rootIndexInfo.ObjectId,
                        IndexType = IndexType.Root | IndexType.Leaf
                    };

                Database
                    .InitFileGroupPageAsync(
                        new InitFileGroupPageParameters(
                            null,
                            rootPage,
                            true,
                            false,
                            true,
                            true))
                    .ConfigureAwait(false);

                // Setup root index page
                rootPage.SetHeaderDirty();
                AddIndexInfo(rootIndexInfo);
            }
        }

        private class TestIndexInfo : IndexInfo
        {
            private readonly BufferFieldUInt64 _logicalId;
            private readonly BufferFieldInt64 _firstKey;
            private readonly BufferFieldInt32 _secondKey;

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
                // ReSharper disable once UnusedMember.Local
                get => _logicalId.Value;
                set => _logicalId.Value = value;
            }

            public DateTime CreatedDate
            {
                get => new DateTime(_firstKey.Value);
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
                get => _secondKey.Value;
                set
                {
                    if (_secondKey.Value != value)
                    {
                        _secondKey.Value = value;
                    }
                }
            }

            protected override BufferField LastField => _secondKey;

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

            protected override void OnRead(SwitchingBinaryReader streamManager)
            {
                base.OnRead(streamManager);
                _firstKey.Read(streamManager);
                _secondKey.Read(streamManager);
            }

            protected override void OnWrite(SwitchingBinaryWriter streamManager)
            {
                base.OnWrite(streamManager);
                _firstKey.Write(streamManager);
                _secondKey.Write(streamManager);
            }
        }

        private class TestIndexPage : IndexPage<TestIndexInfo, RootIndexInfo>
        {
            // ReSharper disable once UnusedMember.Local
            public int CompareIndex(int index, DateTime key1, int key2)
            {
                var lhs = IndexEntries[index];
                var rhs = new TestIndexInfo(key1, key2, 0);
                return lhs.CompareTo(rhs);
            }

            public ushort KeySize => 12;

            public override ushort MaxIndexEntries => (ushort)(DataSize / (2 + KeySize));

            public override IndexManager IndexManager => GetService<TestIndexManager>();

            protected override TestIndexInfo CreateLinkToPage(
                IndexPage<TestIndexInfo, RootIndexInfo> page)
            {
                var createdDate = page.IndexEntries[0].CreatedDate;
                var sequenceIndex = page.IndexEntries[0].SequenceIndex;
                return new TestIndexInfo(createdDate, sequenceIndex, page.LogicalPageId.Value);
            }

            protected override TestIndexInfo CreateIndexEntry()
            {
                return new TestIndexInfo();
            }
        }

        public StorageIndexUnitTest(StorageEngineTestFixture fixture)
        {
            _fixture = fixture;
            _scope = _fixture.Scope.BeginLifetimeScope(
                builder =>
                {
                    builder.RegisterType<DatabaseDevice>()
                        .WithParameter("dbId", DatabaseId.Master)
                        .SingleInstance()
                        .OnActivated(e => e.Instance.InitialiseDeviceLifetimeScope(_fixture.Scope));
                });
        }

        [Fact(DisplayName = "Index test add pages")]
        public async Task IndexTestAddPages()
        {
            var logger = _logger.ForContext<StorageIndexUnitTest>();
            logger.Information("IndexTestAddPages - BEGIN");
            var masterDataPathName = _fixture.GlobalTracker.Get("master_indextest.mddf");
            var masterLogPathName = _fixture.GlobalTracker.Get("master_indextest.mlf");

            var dbDevice = CreateDatabaseDevice();
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

                dbDevice.BeginTransaction(TimeSpan.FromMinutes(10));

                var manager = new TestIndexManager(_scope);
                var indexInfo = new RootIndexInfo
                {
                    Name = "PK_Test",
                    ObjectId = new ObjectId(100),
                    RootIndexDepth = 0,
                    IndexFileGroupId = addFgDevice.FileGroupId,
                };
                manager.CreateIndex(indexInfo);
                //for (var index = 0; index < 1000; ++index)
                //{
                //    manager.AddIndexInfo(
                //        new TestIndexInfo(
                //            new DateTime(2018, 1, 1).AddDays(index),
                //            index,
                //            1000 + index));
                //}

                await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);
            }
            finally
            {
                await dbDevice.CloseAsync().ConfigureAwait(true);
                dbDevice.Dispose();
                logger.Information("IndexTestAddPages - END");
            }
        }

        public void Dispose()
        {
            _scope.Dispose();
        }

        private DatabaseDevice CreateDatabaseDevice()
        {
            return _scope.Resolve<DatabaseDevice>();
        }
    }
}
