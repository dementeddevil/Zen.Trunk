﻿namespace Zen.Trunk.VirtualMemory.Tests
{
	using System.Collections.Generic;
	using System.IO;
	using System.Threading.Tasks;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Zen.Trunk.Storage;
	using Zen.Trunk.Storage.IO;

	/// <summary>
	/// Summary description for Single Device Unit Test suite
	/// </summary>
	[TestClass]
	public class SingleDeviceUnitTest
	{
		private static TestContext _testContext;
		private IVirtualBufferFactory _bufferFactory;

		[ClassInitialize()]
		public static void ClassInitialize(TestContext testContext)
		{
			_testContext = testContext;
		}

		[TestInitialize]
		public void PreTestInitialize()
		{
			_bufferFactory = new VirtualBufferFactory(32, 8192);
		}

		[TestCleanup]
		public void PostTestCleanup()
		{
			_bufferFactory.Dispose();
		}

		[TestMethod]
		[TestCategory("Virtual Memory: Single Device")]
		public async Task CreateSingleDeviceTest()
		{
			var testFile = Path.Combine(_testContext.TestDir, "sdt.bin");
			var device = new SingleBufferDevice(_bufferFactory, true, "test", testFile, true, 8);
			await device.OpenAsync();

			var initBuffers = new List<VirtualBuffer>();
			var subTasks = new List<Task>();
			for (var index = 0; index < 7; ++index)
			{
				var buffer = AllocateAndFill((byte)index);
				initBuffers.Add(buffer);
				subTasks.Add(device.SaveBufferAsync((uint)index, buffer));
			}
			await device.FlushBuffersAsync(true, true);
			await Task.WhenAll(subTasks.ToArray());

			subTasks.Clear();
			var loadBuffers = new List<VirtualBuffer>();
			for (var index = 0; index < 7; ++index)
			{
				var buffer = _bufferFactory.AllocateBuffer();
				loadBuffers.Add(buffer);
				subTasks.Add(device.LoadBufferAsync((uint)index, buffer));
			}
			await device.FlushBuffersAsync(true, true);
			await Task.WhenAll(subTasks.ToArray());

			await device.CloseAsync();

			// Walk buffers and check contents are the same
			for (var index = 0; index < 7; ++index)
			{
				var lhs = initBuffers[index];
				var rhs = loadBuffers[index];
				Assert.IsTrue(lhs.Compare(rhs) == 0, "Buffer mismatch");
			}
		}

		private VirtualBuffer AllocateAndFill(byte value)
		{
			var buffer = _bufferFactory.AllocateBuffer();
			FillBuffer(buffer, value);
			return buffer;
		}

		private void FillBuffer(VirtualBuffer buffer, byte value)
		{
			using (var stream = buffer.GetBufferStream(0, 8192, true))
			{
				for (var index = 0; index < 8192; ++index)
				{
					stream.WriteByte(value);
				}
			}
		}
	}
}
