using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Xunit;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.IO;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    [Trait("Subsystem", "Storage Engine")]
    [Trait("Class", "Page")]
	public class PageUnitTest : AutofacStorageEngineUnitTests
	{
		private class MockPageDevice : PageDevice, IMultipleBufferDevice
		{
			private readonly Dictionary<VirtualPageId, PageBuffer> _pages =
                new Dictionary<VirtualPageId, PageBuffer>();

		    public MockPageDevice(ILifetimeScope parentLifetimeScope)
		    {
                InitialiseDeviceLifetimeScope(parentLifetimeScope);
		    }

			/// <summary>
			/// Creates a page of the specified type.
			/// </summary>
			/// <typeparam name="TPage">The type of the page.</typeparam>
			/// <returns></returns>
			public TPage CreatePage<TPage>(VirtualPageId pageId)
				where TPage : DataPage, new()
			{
				var treatAsInit = false;
				PageBuffer pageBuffer;
				if (!_pages.TryGetValue(pageId, out pageBuffer))
				{
					pageBuffer = new PageBuffer(this);
					pageBuffer.InitAsync(pageId, LogicalPageId.Zero);
					_pages.Add(pageId, pageBuffer);
					treatAsInit = true;
				}

				var page = new TPage();
				HookupPageSite(page);

				page.VirtualId = pageId;

				if (treatAsInit)
				{
					page.PreInitInternal();
					page.DataBuffer = pageBuffer;
					page.OnInitInternal();
				}
				else
				{
					page.PreLoadInternal();
					page.DataBuffer = pageBuffer;
					page.PostLoadInternal();
				}

				return page;
			}

			public IVirtualBufferFactory BufferFactory => ResolveDeviceService<IVirtualBufferFactory>();

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

			public uint ExpandDevice(DeviceId deviceId, int pageCount)
			{
				throw new NotImplementedException();
			}

			public Task LoadBufferAsync(VirtualPageId pageId, VirtualBuffer buffer)
			{
				throw new NotImplementedException();
			}

			public Task SaveBufferAsync(VirtualPageId pageId, VirtualBuffer buffer)
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

		[Fact(DisplayName = @"
Given a distribution page that can store 128 extents with 128 allocated extents
When the 129th extent is allocated,
Then the allocation fails.")]
		public Task DistributionValidExtentNonMixedTest()
		{
			var pageDevice = new MockPageDevice(Scope);

			pageDevice.BeginTransaction();

			var page = pageDevice.CreatePage<DistributionPage>(
				new VirtualPageId(0));
			page.InitialiseValidExtents(129);

			// We should be able to allocate 128 exclusive extents for 128 
			//	objects
			uint extentsToTest = 16;
			VirtualPageId virtualId;
			for (uint index = 0; index < extentsToTest; ++index)
			{
				// This allocation must succeed
				virtualId = page.AllocatePage(
					new AllocateDataPageParameters(
                        new LogicalPageId(1024 + index),
                        new ObjectId(1 + index),
                        ObjectType.Sample,
                        false));
				Assert.True(virtualId != VirtualPageId.Zero, "Expected allocation to succeed.");
			}

			// This allocation must fail
			virtualId = page.AllocatePage(
				new AllocateDataPageParameters(
                    new LogicalPageId(1024 + extentsToTest),
                    new ObjectId(1 + extentsToTest),
                    ObjectType.Sample, 
                    false));
			Assert.True(virtualId == VirtualPageId.Zero, "Expected allocation to fail.");

			return TrunkTransactionContext.Commit();
		}
	}
}
