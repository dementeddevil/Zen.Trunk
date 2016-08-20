namespace Zen.Trunk.StorageEngine.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Zen.Trunk.Storage;
	using Zen.Trunk.Storage.Data;
	using Zen.Trunk.Storage.IO;
	using Zen.Trunk.Storage.Locking;

	[TestClass]
	public class PageUnitTest
	{
		private class MockPageDevice : PageDevice, IMultipleBufferDevice
		{
			private VirtualBufferFactory _bufferFactory = new VirtualBufferFactory(32, 8192);
			private Dictionary<VirtualPageId, PageBuffer> _pages = new Dictionary<VirtualPageId, PageBuffer>();
			private GlobalLockManager _globalLockManager;
			private DatabaseLockManager _lockManager;

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

			protected override object GetService(Type serviceType)
			{
				if (serviceType == typeof(GlobalLockManager))
				{
					if (_globalLockManager == null)
					{
						_globalLockManager = new GlobalLockManager();
					}
					return _globalLockManager;
				}
				if (serviceType == typeof(IDatabaseLockManager))
				{
					if (_lockManager == null)
					{
						_lockManager = new DatabaseLockManager(
                            GetService<GlobalLockManager>(), new DatabaseId(1));
					}
					return _lockManager;
				}
				return base.GetService(serviceType);
			}

			public IVirtualBufferFactory BufferFactory
			{
				get
				{
					return _bufferFactory;
				}
			}

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

		[TestMethod]
		[TestCategory("Storage Engine: PageDevice")]
		public Task DistributionValidExtentNonMixedTest()
		{
			var pageDevice = new MockPageDevice();

			TrunkTransactionContext.BeginTransaction(pageDevice);

			var page = pageDevice.CreatePage<DistributionPage>(
				new VirtualPageId(0));
			page.InitialiseValidExtents(129);

			// We should be able to allocate 128 exclusive extents for 128 
			//	objects
			uint extentsToTest = 16;
			ulong virtualId;
			for (uint index = 0; index < extentsToTest; ++index)
			{
				// This allocation must succeed
				virtualId = page.AllocatePage(
					new AllocateDataPageParameters(
                        new LogicalPageId(1024 + index),
                        new ObjectId(1 + index),
                        ObjectType.Sample,
                        false));
				Assert.IsTrue(virtualId != 0, "Expected allocation to succeed.");
			}

			// This allocation must fail
			virtualId = page.AllocatePage(
				new AllocateDataPageParameters(
                    new LogicalPageId(1024 + extentsToTest),
                    new ObjectId(1 + extentsToTest),
                    ObjectType.Sample, 
                    false));
			Assert.IsTrue(virtualId == 0, "Expected allocation to fail.");

			return TrunkTransactionContext.Commit();
		}
	}
}
