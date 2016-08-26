using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.VirtualMemory
{

    /// <summary>
    /// Summary description for Multiple Device Unit Test suite
    /// </summary>
    [Trait("Subsystem", "Virtual Memory")]
    [Trait("Class", "Multiple Device")]
    public class MultipleDeviceUnitTest : AutofacVirtualMemoryUnitTests
    {
        [Fact(DisplayName = @"
Given a newly created multi-device with 4 sub-files
When 7 buffers are written to each sub-file and then read into separate buffers
Then the buffer contents are the same")]
        public async Task CreateMultipleDeviceTest()
        {
            using (var tracker = new TempFileTracker())
            {
                var saveBuffers = new List<VirtualBuffer>();
                var loadBuffers = new List<VirtualBuffer>();
                using (var device = BufferDeviceFactory.CreateMultipleBufferDevice(true))
                {
                    var deviceIds = new List<DeviceId>();
                    foreach (var filename in GetChildDeviceList())
                    {
                        var pathName = tracker.Get(filename);
                        deviceIds.Add(await device.AddDeviceAsync(filename, pathName, DeviceId.Zero, 128).ConfigureAwait(true));
                    }
                   
                    await device.OpenAsync().ConfigureAwait(true);

                    // Write a load of buffers across the group of devices
                    var subTasks = new List<Task>();
                    foreach (var deviceId in deviceIds)
                    {
                        for (var index = 0; index < 7; ++index)
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
                        for (var index = 0; index < 7; ++index)
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
                    Assert.True(lhs.Compare(rhs) == 0, "Buffer mismatch");
                }

                DisposeBuffers(saveBuffers);
                DisposeBuffers(loadBuffers);
            }
        }

        private void DisposeBuffers(IEnumerable<VirtualBuffer> buffers)
        {
            foreach (var buffer in buffers)
            {
                buffer.Dispose();
            }
        }

        private string[] GetChildDeviceList()
        {
            return new[]
                {
                    "Device1.bin",
                    "Device2.bin",
                    "Device3.bin",
                    "Device4.bin",
                };
        }
    }
}
