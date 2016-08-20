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
			List<ushort> deviceIds = new List<ushort>();
			foreach (var filename in GetChildDeviceList())
			{
				string pathName = Path.Combine(_testContext.TestDir, filename);
				deviceIds.Add(await device.AddDevice(filename, pathName, 0, 128));
			}
			await device.Open();

			// Write a load of buffers across the group of devices
			List<Task> bufferTasks = new List<Task>();
			List<VirtualBuffer> buffers = new List<VirtualBuffer>();
			foreach (ushort deviceId in deviceIds)
			{
				for (int index = 0; index < 7; ++index)
				{
					var buffer = AllocateAndFill((byte)index);
					buffers.Add(buffer);
					bufferTasks.Add(device.SaveBuffer(
						new DevicePageId(deviceId, (uint)index), buffer));
				}
			}

			// Flush all buffers to disk
			await device.FlushBuffers(true, true);

			// Close the device
			await device.Close();

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
			VirtualBuffer buffer = _bufferFactory.AllocateBuffer();
			FillBuffer(buffer, value);
			return buffer;
		}

		private void FillBuffer(VirtualBuffer buffer, byte value)
		{
			using (var stream = buffer.GetBufferStream(0, _bufferFactory.BufferSize, true))
			{
				for (int index = 0; index < _bufferFactory.BufferSize; ++index)
				{
					stream.WriteByte(value);
				}
			}
		}
	}
}
