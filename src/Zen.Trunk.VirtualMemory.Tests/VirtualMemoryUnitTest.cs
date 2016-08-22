using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.IO;
using Xunit;

namespace Zen.Trunk.VirtualMemory.Tests
{
    /// <summary>
    /// Summary description for Virtual Memory Unit Test suite
    /// </summary>
    [Trait("Subsystem", "Virtual Memory")]
    [Trait("Class", "Single Device")]
    public class VirtualMemoryUnitTest
    {
        private const int BufferSize = 8192;
        private IVirtualBufferFactory _bufferFactory = new VirtualBufferFactory(32, BufferSize);

        ~VirtualMemoryUnitTest()
        {
            _bufferFactory.Dispose();
            _bufferFactory = null;
        }

        [Fact(DisplayName = @"
Given a virtual buffer factory,
When 9 threads allocate and deallocate 1000 buffers simultaneously,
Then no corruption or deadlocks occur")]
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

        [Fact(DisplayName = @"
Given an advanced file stream instance,
When buffers are written,
Then scatter/gather I/O operations occur as appropriate")]
        public async Task ScatterGatherWriteTest()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var testFile = Path.Combine(assemblyLocation, "SGWT.bin");
            try
            {
                using (var stream = new AdvancedFileStream(
                    testFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 8192,
                    FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.WriteThrough, true))
                {
                    stream.SetLength(8192 * 16);

                    using (var transfer = new ScatterGatherReaderWriter(stream))
                    {
                        await transfer.WriteBufferAsync(0, _bufferFactory.AllocateAndFill(0)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(1, _bufferFactory.AllocateAndFill(1)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(2, _bufferFactory.AllocateAndFill(2)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(3, _bufferFactory.AllocateAndFill(3)).ConfigureAwait(true);

                        await transfer.WriteBufferAsync(10, _bufferFactory.AllocateAndFill(0)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(11, _bufferFactory.AllocateAndFill(1)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(12, _bufferFactory.AllocateAndFill(2)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(13, _bufferFactory.AllocateAndFill(3)).ConfigureAwait(true);

                        await transfer.WriteBufferAsync(6, _bufferFactory.AllocateAndFill(6)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(7, _bufferFactory.AllocateAndFill(7)).ConfigureAwait(true);
                    }
                }
            }
            finally
            {
                File.Delete(testFile);
            }
        }
    }
}