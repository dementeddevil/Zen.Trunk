using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Zen.Trunk.VirtualMemory.Tests
{
    /// <summary>
    /// Summary description for Single Device Unit Test suite
    /// </summary>
    [Trait("Subsystem", "Virtual Memory")]
    [Trait("Class", "Single Device")]
    public class SingleDeviceUnitTest : AutofacVirtualMemoryUnitTests
    {
        [Theory(DisplayName = @"
Given a newly created single-device
When 128 buffers are written and then read into separate buffers
Then the buffer contents are the same")]
        [InlineData(128)]
        public async Task SingleDeviceBufferWriteThenReadTest(uint pageCount)
        {
            using (var tracker = new TempFileTracker())
            {
                // Arrange
                var initBuffers = new List<IVirtualBuffer>();
                var loadBuffers = new List<IVirtualBuffer>();
                var testFile = tracker.Get("sdt.bin");
                using (var device = BufferDeviceFactory.CreateSingleBufferDevice("test", testFile, pageCount, true))
                {
                    await device.OpenAsync().ConfigureAwait(true);

                    var subTasks = new List<Task>();
                    for (var index = 0; index < pageCount; ++index)
                    {
                        var buffer = BufferFactory.AllocateAndFill((byte)index);
                        initBuffers.Add(buffer);
                        subTasks.Add(device.SaveBufferAsync((uint)index, buffer));
                    }
                    await device.FlushBuffersAsync(true, true).ConfigureAwait(true);
                    await Task.WhenAll(subTasks.ToArray()).ConfigureAwait(true);

                    subTasks.Clear();
                    for (var index = 0; index < pageCount; ++index)
                    {
                        var buffer = BufferFactory.AllocateBuffer();
                        loadBuffers.Add(buffer);
                        subTasks.Add(device.LoadBufferAsync((uint)index, buffer));
                    }
                    await device.FlushBuffersAsync(true, true).ConfigureAwait(true);
                    await Task.WhenAll(subTasks.ToArray()).ConfigureAwait(true);

                    await device.CloseAsync().ConfigureAwait(true);
                }

                // Walk buffers and check contents are the same
                for (var index = 0; index < pageCount; ++index)
                {
                    var lhs = initBuffers[index];
                    var rhs = loadBuffers[index];
                    Assert.True(lhs.CompareTo(rhs) == 0, "Buffer mismatch");
                }

                DisposeBuffers(initBuffers);
                DisposeBuffers(loadBuffers);
            }
        }

        private void DisposeBuffers(IEnumerable<IVirtualBuffer> buffers)
        {
            foreach (var buffer in buffers)
            {
                buffer.Dispose();
            }
        }
    }
}
