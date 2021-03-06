﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Xunit;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    [Trait("Subsystem", "Storage Engine")]
    [Trait("Class", "Page")]
    // ReSharper disable once InconsistentNaming
    public class Page_should : IClassFixture<StorageEngineTestFixture>
    {
        private readonly StorageEngineTestFixture _fixture;

        public Page_should(StorageEngineTestFixture fixture)
        {
            _fixture = fixture;
        }

        private class MockPageDevice : PageDevice, IMultipleBufferDevice
        {
            private readonly DevicePageTracker _pageTracker;

            public MockPageDevice(ILifetimeScope parentLifetimeScope)
            {
                InitialiseDeviceLifetimeScope(parentLifetimeScope);
                _pageTracker = new DevicePageTracker(LifetimeScope, this);
            }

            /// <summary>
            /// Creates a page of the specified type.
            /// </summary>
            /// <typeparam name="TPage">The type of the page.</typeparam>
            /// <returns></returns>
            public TPage CreatePage<TPage>(VirtualPageId pageId)
                where TPage : DataPage, new()
            {
                return _pageTracker.CreatePage<TPage>(pageId);
            }

            public IVirtualBufferFactory BufferFactory => GetService<IVirtualBufferFactory>();

            public Task OpenAsync()
            {
                throw new NotImplementedException();
            }

            public Task<DeviceId> AddDeviceAsync(string name, string pathName)
            {
                throw new NotImplementedException();
            }

            public Task<DeviceId> AddDeviceAsync(string name, string pathName, DeviceId deviceId, uint createPageCount = 0)
            {
                throw new NotImplementedException();
            }

            public Task RemoveDeviceAsync(DeviceId deviceId)
            {
                throw new NotImplementedException();
            }

            public void ResizeDevice(DeviceId deviceId, uint pageCount)
            {
                throw new NotImplementedException();
            }

            public Task LoadBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer)
            {
                throw new NotImplementedException();
            }

            public Task SaveBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer)
            {
                throw new NotImplementedException();
            }

            public Task FlushBuffersAsync(bool flushReads, bool flushWrites, params DeviceId[] deviceIds)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IBufferDeviceInfo> GetDeviceInfo()
            {
                throw new NotImplementedException();
            }

            public IBufferDeviceInfo GetDeviceInfo(DeviceId deviceId)
            {
                throw new NotImplementedException();
            }
        }

        [Fact(DisplayName = nameof(Page_should) + "_" + nameof(fail_allocation_when_distribution_page_extents_are_exhausted))]
        public async Task fail_allocation_when_distribution_page_extents_are_exhausted()
        {
            var pageDevice = new MockPageDevice(_fixture.Scope);

            pageDevice.BeginTransaction();

            var page = pageDevice.CreatePage<DistributionPage>(new VirtualPageId(0));
            await page.UpdateValidExtentsAsync(129).ConfigureAwait(true);

            // We should be able to allocate 128 exclusive extents for 128 
            //	objects
            uint extentsToTest = 16;
            VirtualPageId virtualId;
            for (uint index = 0; index < extentsToTest; ++index)
            {
                // This allocation must succeed
                virtualId = await page
                    .AllocatePageAsync(
                        new AllocateDataPageParameters(
                            new LogicalPageId(1024 + index),
                            new ObjectId(1 + index),
                            ObjectType.Audio,
                            false,
                            false))
                    .ConfigureAwait(true);
                Assert.True(virtualId != VirtualPageId.Zero, $"Expected allocation {index} to succeed.");
            }

            // This allocation must fail
            virtualId = await page
                .AllocatePageAsync(
                    new AllocateDataPageParameters(
                        new LogicalPageId(1024 + extentsToTest),
                        new ObjectId(1 + extentsToTest),
                        ObjectType.Audio,
                        false,
                        false))
                .ConfigureAwait(true);
            Assert.True(virtualId == VirtualPageId.Zero, "Expected allocation to fail.");

            await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);
        }

        [Fact(DisplayName = nameof(Page_should) + "_" + nameof(verify_all_concrete_page_classes_have_header_block_smaller_than_maximum))]
        public void verify_all_concrete_page_classes_have_header_block_smaller_than_maximum()
        {
            var concretePages = typeof(Page)
                .Assembly
                .GetTypes()
                .Where(t => t.IsAssignableTo<Page>() && !t.IsAbstract && t.IsPublic)
                .Select(pageType => (Page)Activator.CreateInstance(pageType))
                .OrderBy(p => p.GetType().Namespace)
                .ThenBy(p => p.MinHeaderSize);
            foreach (var page in concretePages)
            {
                Debug.WriteLine($"{page.GetType().FullName} => MinHeaderSize={page.MinHeaderSize}");
                Assert.True(page.HeaderSize >= page.MinHeaderSize, "Page header must be large enough to accommodate min header.");
            }
        }
    }
}
