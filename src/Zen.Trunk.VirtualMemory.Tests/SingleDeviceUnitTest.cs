using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.IO;
using Xunit;

namespace Zen.Trunk.VirtualMemory.Tests
{
    /// <summary>
    /// Summary description for Single Device Unit Test suite
    /// </summary>
    [Trait("Subsystem", "Virtual Memory")]
    [Trait("Class", "Single Device")]
    public class SingleDeviceUnitTest
    {
        private const int BufferSize = 8192;
        private IVirtualBufferFactory _bufferFactory = new VirtualBufferFactory(32, BufferSize);

        ~SingleDeviceUnitTest()
        {
            _bufferFactory.Dispose();
            _bufferFactory = null;
        }

        [Fact(DisplayName = @"
Given a newly created single-device
When 7 buffers are written and then read into separate buffers
Then the buffer contents are the same")]
        public async Task SingleDeviceBufferWriteThenReadTest()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var testFile = Path.Combine(assemblyLocation, "sdt.bin");
            var device = new SingleBufferDevice(_bufferFactory, true, "test", testFile, true, 8);
            await device.OpenAsync().ConfigureAwait(true);
            try
            {
                var initBuffers = new List<VirtualBuffer>();
                var subTasks = new List<Task>();
                for (var index = 0; index < 7; ++index)
                {
                    var buffer = _bufferFactory.AllocateAndFill((byte)index);
                    initBuffers.Add(buffer);
                    subTasks.Add(device.SaveBufferAsync((uint)index, buffer));
                }
                await device.FlushBuffersAsync(true, true).ConfigureAwait(true);
                await Task.WhenAll(subTasks.ToArray()).ConfigureAwait(true);

                subTasks.Clear();
                var loadBuffers = new List<VirtualBuffer>();
                for (var index = 0; index < 7; ++index)
                {
                    var buffer = _bufferFactory.AllocateBuffer();
                    loadBuffers.Add(buffer);
                    subTasks.Add(device.LoadBufferAsync((uint)index, buffer));
                }
                await device.FlushBuffersAsync(true, true).ConfigureAwait(true);
                await Task.WhenAll(subTasks.ToArray()).ConfigureAwait(true);

                await device.CloseAsync().ConfigureAwait(true);

                // Walk buffers and check contents are the same
                for (var index = 0; index < 7; ++index)
                {
                    var lhs = initBuffers[index];
                    var rhs = loadBuffers[index];
                    Assert.True(lhs.Compare(rhs) == 0, "Buffer mismatch");
                }

                DisposeBuffers(initBuffers);
                DisposeBuffers(loadBuffers);
            }
            finally
            {
                File.Delete(testFile);
            }
        }

        private void DisposeBuffers(IEnumerable<VirtualBuffer> buffers)
        {
            foreach (var buffer in buffers)
            {
                buffer.Dispose();
            }
        }
    }
}
