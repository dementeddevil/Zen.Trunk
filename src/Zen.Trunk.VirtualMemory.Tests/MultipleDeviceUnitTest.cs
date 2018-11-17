using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Zen.Trunk.VirtualMemory.Tests
{
    /// <summary>
    /// Summary description for Multiple Device Unit Test suite
    /// </summary>
    [Trait("Subsystem", "Virtual Memory")]
    [Trait("Class", "Multiple Device")]
    public class MultipleDeviceUnitTest : AutofacVirtualMemoryUnitTests
    {
        [Theory(DisplayName = @"
Given a newly created multi-device with 16 sub-files
When 128 buffers are written to each sub-file and then read into separate buffers
Then the buffer contents are the same")]
        [InlineData(16, 128)]
        public async Task CreateMultipleDeviceTest(int deviceCount, uint pagesPerDevice)
        {
            var saveBuffers = new List<IVirtualBuffer>();
            var loadBuffers = new List<IVirtualBuffer>();
            using (var device = BufferDeviceFactory.CreateMultipleBufferDevice(true))
            {
                var deviceIds = new List<DeviceId>();
                for (int deviceIndex = 0; deviceIndex < deviceCount; ++deviceIndex)
                {
                    var filename = $"mdt{deviceIndex}.bin";
                    var pathName = GlobalTracker.Get(filename);
                    var deviceId = await device
                        .AddDeviceAsync(filename, pathName, DeviceId.Zero, pagesPerDevice)
                        .ConfigureAwait(true);
                    deviceIds.Add(deviceId);
                }
               
                await device.OpenAsync().ConfigureAwait(true);

                // Write a load of buffers across the group of devices
                var subTasks = new List<Task>();
                foreach (var deviceId in deviceIds)
                {
                    for (var index = 0; index < pagesPerDevice; ++index)
                    {
                        var buffer = BufferFactory.AllocateAndFill((byte)index);
                        saveBuffers.Add(buffer);
                        subTasks.Add(device.SaveBufferAsync(
                            new VirtualPageId(deviceId, (uint)index), buffer));
                    }
                }
                await device.FlushBuffersAsync(true, true).ConfigureAwait(true);
                await Task.WhenAll(subTasks.ToArray()).ConfigureAwait(true);

                subTasks.Clear();
                foreach (var deviceId in deviceIds)
                {
                    for (var index = 0; index < pagesPerDevice; ++index)
                    {
                        var buffer = BufferFactory.AllocateBuffer();
                        loadBuffers.Add(buffer);
                        subTasks.Add(device.LoadBufferAsync(
                            new VirtualPageId(deviceId, (uint)index), buffer));
                    }
                }
                await device.FlushBuffersAsync(true, true).ConfigureAwait(true);
                await Task.WhenAll(subTasks.ToArray()).ConfigureAwait(true);

                // Close the device
                await device.CloseAsync().ConfigureAwait(true);
            }

            // Walk buffers and check contents are the same
            for (var index = 0; index < saveBuffers.Count; ++index)
            {
                var lhs = saveBuffers[index];
                var rhs = loadBuffers[index];
                Assert.True(lhs.CompareTo(rhs) == 0, "Buffer mismatch");
            }

            DisposeBuffers(saveBuffers);
            DisposeBuffers(loadBuffers);
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
