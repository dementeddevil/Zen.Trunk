using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Zen.Trunk.VirtualMemory.Tests
{
    /// <summary>
    /// Summary description for Virtual Memory Unit Test suite
    /// </summary>
    [Trait("Subsystem", "Virtual Memory")]
    [Trait("Class", "Single Device")]
    public class VirtualMemoryUnitTests : AutofacVirtualMemoryUnitTests
    {
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
                        var bufferList = new List<IVirtualBuffer>();
                        try
                        {
                            for (var index = 0; index < 1000; ++index)
                            {
                                bufferList.Add(BufferFactory.AllocateBuffer());
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
        }

        [Fact(DisplayName = @"
Given an advanced file stream instance,
When buffers are written,
Then scatter/gather I/O operations occur as appropriate")]
        public async Task ScatterGatherWriteTest()
        {
            using (var tracker = new TempFileTracker())
            {
                var testFile = tracker.Get("SGWT.bin");
                using (var stream = new AdvancedFileStream(
                    testFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 8192,
                    FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.WriteThrough, true))
                {
                    stream.SetLength(8192 * 16);

                    using (var transfer = new ScatterGatherRequestQueue(stream))
                    {
                        await transfer.WriteBufferAsync(0, BufferFactory.AllocateAndFill(0)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(1, BufferFactory.AllocateAndFill(1)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(2, BufferFactory.AllocateAndFill(2)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(3, BufferFactory.AllocateAndFill(3)).ConfigureAwait(true);

                        await transfer.WriteBufferAsync(10, BufferFactory.AllocateAndFill(0)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(11, BufferFactory.AllocateAndFill(1)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(12, BufferFactory.AllocateAndFill(2)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(13, BufferFactory.AllocateAndFill(3)).ConfigureAwait(true);

                        await transfer.WriteBufferAsync(6, BufferFactory.AllocateAndFill(6)).ConfigureAwait(true);
                        await transfer.WriteBufferAsync(7, BufferFactory.AllocateAndFill(7)).ConfigureAwait(true);
                    }
                }
            }
        }

        [Fact(DisplayName = "Given buffer, when GetBufferStream is called and released, then no exception is thrown")]
        public void VirtualBufferGetAndReleaseStream()
        {
            using (var buffer = BufferFactory.AllocateBuffer())
            {
                using (var stream = buffer.GetBufferStream(0, 1024, true))
                {
                    stream.WriteByte(10);
                }
            }
        }
    }
}