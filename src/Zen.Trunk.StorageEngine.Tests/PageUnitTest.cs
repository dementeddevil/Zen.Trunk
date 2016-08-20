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
			private Dictionary<DevicePageId, PageBuffer> _pages = new Dictionary<DevicePageId, PageBuffer>();
			private GlobalLockManager _globalLockManager;
			private DatabaseLockManager _lockManager;

			/// <summary>
			/// Creates a page of the specified type.
			/// </summary>
			/// <typeparam name="TPage">The type of the page.</typeparam>
			/// <returns></returns>
			public TPage CreatePage<TPage>(DevicePageId pageId)
				where TPage : DataPage, new()
			{
				bool treatAsInit = false;
				PageBuffer pageBuffer;
				if (!_pages.TryGetValue(pageId, out pageBuffer))
				{
					pageBuffer = new PageBuffer(this);
					pageBuffer.Init(pageId, 0);
					_pages.Add(pageId, pageBuffer);
					treatAsInit = true;
				}

				TPage page = new TPage();
				HookupPageSite(page);

				page.VirtualId = pageId.VirtualPageId;

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
						_lockManager = new DatabaseLockManager(GetService<GlobalLockManager>(), 1);
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

			public Task Open()
			{
				throw new NotImplementedException();
			}

			public Task<ushort> AddDevice(string name, string pathName, ushort deviceId = 0, uint createPageCount = 0)
			{
				throw new NotImplementedException();
			}

			public Task RemoveDevice(ushort deviceId)
			{
				throw new NotImplementedException();
			}

			public uint ExpandDevice(ushort deviceId, int pageCount)
			{
				throw new NotImplementedException();
			}

			public Task LoadBuffer(DevicePageId pageId, VirtualBuffer buffer)
			{
				throw new NotImplementedException();
			}

			public Task SaveBuffer(DevicePageId pageId, VirtualBuffer buffer)
			{
				throw new NotImplementedException();
			}

			public Task FlushBuffers(bool flushReads, bool flushWrites, params ushort[] deviceIds)
			{
				throw new NotImplementedException();
			}

			public IEnumerable<IBufferDeviceInfo> GetDeviceInfo()
			{
				throw new NotImplementedException();
			}

			public IBufferDeviceInfo GetDeviceInfo(ushort deviceId)
			{
				throw new NotImplementedException();
			}
		}

		[TestMethod]
		[TestCategory("Storage Engine: PageDevice")]
		public void DistributionValidExtentNonMixedTest()
		{
			MockPageDevice pageDevice = new MockPageDevice();

			TrunkTransactionContext.BeginTransaction(pageDevice);

			DistributionPage page = pageDevice.CreatePage<DistributionPage>(
				new DevicePageId(0));
			page.InitialiseValidExtents(129);

			// We should be able to allocate 128 exclusive extents for 128 
			//	objects
			uint extentsToTest = 16;
			ulong virtualId;
			for (uint index = 0; index < extentsToTest; ++index)
			{
				// This allocation must succeed
				virtualId = page.AllocatePage(
					new AllocateDataPageParameters(1024 + index, 1 + index, 1, false));
				Assert.IsTrue(virtualId != 0, "Expected allocation to succeed.");
			}

			// This allocation must fail
			virtualId = page.AllocatePage(
				new AllocateDataPageParameters(1024 + extentsToTest, 1 + extentsToTest, 1, false));
			Assert.IsTrue(virtualId == 0, "Expected allocation to fail.");

			TrunkTransactionContext.Commit();
		}
	}
}
