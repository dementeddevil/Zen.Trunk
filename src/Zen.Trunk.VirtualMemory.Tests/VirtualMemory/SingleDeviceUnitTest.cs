using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Xunit;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// Summary description for Single Device Unit Test suite
    /// </summary>
    [Trait("Subsystem", "Virtual Memory")]
    [Trait("Class", "Single Device")]
    public class SingleDeviceUnitTest : AutofacVirtualMemoryUnitTests
    {
        [Fact(DisplayName = @"
Given a newly created single-device
When 7 buffers are written and then read into separate buffers
Then the buffer contents are the same")]
        public async Task SingleDeviceBufferWriteThenReadTest()
        {
            using (var tracker = new TempFileTracker())
            {
                // Arrange
                var initBuffers = new List<IVirtualBuffer>();
                var loadBuffers = new List<IVirtualBuffer>();
                var testFile = tracker.Get("sdt.bin");
                using (var device = BufferDeviceFactory.CreateSingleBufferDevice("test", testFile, 8, true))
                {
                    await device.OpenAsync().ConfigureAwait(true);

                    var subTasks = new List<Task>();
                    for (var index = 0; index < 7; ++index)
                    {
                        var buffer = BufferFactory.AllocateAndFill((byte)index);
                        initBuffers.Add(buffer);
                        subTasks.Add(device.SaveBufferAsync((uint)index, buffer));
                    }
                    await device.FlushBuffersAsync(true, true).ConfigureAwait(true);
                    await Task.WhenAll(subTasks.ToArray()).ConfigureAwait(true);

                    subTasks.Clear();
                    for (var index = 0; index < 7; ++index)
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
                for (var index = 0; index < 7; ++index)
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
