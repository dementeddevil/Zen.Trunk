namespace Zen.Trunk.VirtualMemory.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Zen.Trunk.Storage;
	using Zen.Trunk.Storage.IO;

	/// <summary>
	/// Summary description for Virtual Memory Unit Test suite
	/// </summary>
	[TestClass]
	public class VirtualMemoryUnitTest
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
		[TestCategory("Virtual Memory: Virtual Buffer Factory")]
		public void AllocateAndDeallocateTest()
		{
			var parallelRequests = new List<Task>();
			for (var tasks = 0; tasks < 9; ++tasks)
			{
				parallelRequests.Add(Task.Factory.StartNew(
					() =>
					{
						var bufferList = new List<VirtualBuffer>();
						try
						{
							for (var index = 0; index < 1000; ++index)
							{
								bufferList.Add(_bufferFactory.AllocateBuffer());
							}
						}
						catch (OutOfMemoryException)
						{
							Debug.WriteLine("VirtualBufferFactory: Memory exhausted.");
						}
						Thread.Sleep(1000);
						foreach (var buffer in bufferList)
						{
							buffer.Dispose();
						}
					}));
			}
			Task.WaitAll(parallelRequests.ToArray());

			_bufferFactory.Dispose();
		}

		[TestMethod]
		[TestCategory("Virtual Memory: Virtual Buffer Factory")]
		public async Task ScatterGatherWriteTest()
		{
			var testFile = Path.Combine(_testContext.TestRunDirectory, "SGWT.bin");
			using (var stream = new AdvancedFileStream(
				testFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 8192,
				FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.WriteThrough, true))
			{
				stream.SetLength(8192 * 16);

				using (var transfer = new ScatterGatherReaderWriter(stream))
				{
					await transfer.WriteBufferAsync(0, AllocateAndFill(0));
                    await transfer.WriteBufferAsync(1, AllocateAndFill(1));
                    await transfer.WriteBufferAsync(2, AllocateAndFill(2));
                    await transfer.WriteBufferAsync(3, AllocateAndFill(3));

                    await transfer.WriteBufferAsync(10, AllocateAndFill(0));
                    await transfer.WriteBufferAsync(11, AllocateAndFill(1));
                    await transfer.WriteBufferAsync(12, AllocateAndFill(2));
                    await transfer.WriteBufferAsync(13, AllocateAndFill(3));

                    await transfer.WriteBufferAsync(6, AllocateAndFill(6));
                    await transfer.WriteBufferAsync(7, AllocateAndFill(7));
				}
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
