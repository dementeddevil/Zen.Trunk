namespace Zen.Trunk.VirtualMemory.Tests
{
	using System.Collections.Generic;
	using System.IO;
	using System.Threading.Tasks;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Zen.Trunk.Storage;
	using Zen.Trunk.Storage.IO;

	/// <summary>
	/// Summary description for Multiple Device Unit Test suite
	/// </summary>
	[TestClass]
	public class MultipleDeviceUnitTest
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
		[TestCategory("Virtual Memory: Multiple Device")]
		public async Task CreateMultipleDeviceTest()
		{
			// Create multiple device and add our child devices
			IMultipleBufferDevice device = new MultipleBufferDevice(
				_bufferFactory, true);
			var deviceIds = new List<DeviceId>();
			foreach (var filename in GetChildDeviceList())
			{
				var pathName = Path.Combine(_testContext.TestDir, filename);
				deviceIds.Add(await device.AddDeviceAsync(filename, pathName, DeviceId.Zero, 128));
			}
			await device.OpenAsync();

			// Write a load of buffers across the group of devices
			var bufferTasks = new List<Task>();
			var buffers = new List<VirtualBuffer>();
			foreach (var deviceId in deviceIds)
			{
				for (var index = 0; index < 7; ++index)
				{
					var buffer = AllocateAndFill((byte)index);
					buffers.Add(buffer);
					bufferTasks.Add(device.SaveBufferAsync(
						new VirtualPageId(deviceId, (uint)index), buffer));
				}
			}

			// Flush all buffers to disk
			await device.FlushBuffersAsync(true, true);

			// Close the device
			await device.CloseAsync();

			// TODO: Devise method to determine whether flush/action has succeeded

			foreach (var buffer in buffers)
			{
				buffer.Dispose();
			}
		}

		private string[] GetChildDeviceList()
		{
			return new string[]
				{
					"Device1.bin",
					"Device2.bin",
					"Device3.bin",
					"Device4.bin",
				};
		}

		private VirtualBuffer AllocateAndFill(byte value)
		{
			var buffer = _bufferFactory.AllocateBuffer();
			FillBuffer(buffer, value);
			return buffer;
		}

		private void FillBuffer(VirtualBuffer buffer, byte value)
		{
			using (var stream = buffer.GetBufferStream(0, _bufferFactory.BufferSize, true))
			{
				for (var index = 0; index < _bufferFactory.BufferSize; ++index)
				{
					stream.WriteByte(value);
				}
			}
		}
	}
}
