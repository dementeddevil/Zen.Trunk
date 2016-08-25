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
    public class MultipleDeviceUnitTest
    {
        private const int BufferSize = 8192;
        private IVirtualBufferFactory _bufferFactory = new VirtualBufferFactory(32, BufferSize);

        ~MultipleDeviceUnitTest()
        {
            _bufferFactory.Dispose();
            _bufferFactory = null;
        }

        [Fact(DisplayName = @"
Given a newly created multi-device with 4 sub-files
When 7 buffers are written to each sub-file and then read into separate buffers
Then the buffer contents are the same")]
        public async Task CreateMultipleDeviceTest()
        {
            // Create multiple device and add our child devices
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var testFiles = new List<string>();
            var device = new MultipleBufferDevice(_bufferFactory, true);
            var deviceIds = new List<DeviceId>();
            foreach (var filename in GetChildDeviceList())
            {
                var pathName = Path.Combine(assemblyLocation, filename);
                testFiles.Add(pathName);
                deviceIds.Add(await device.AddDeviceAsync(filename, pathName, DeviceId.Zero, 128).ConfigureAwait(true));
            }
            await device.OpenAsync().ConfigureAwait(true);
            try
            {
                // Write a load of buffers across the group of devices
                var subTasks = new List<Task>();
                var saveBuffers = new List<VirtualBuffer>();
                foreach (var deviceId in deviceIds)
                {
                    for (var index = 0; index < 7; ++index)
                    {
                        var buffer = _bufferFactory.AllocateAndFill((byte)index);
                        saveBuffers.Add(buffer);
                        subTasks.Add(device.SaveBufferAsync(
                            new VirtualPageId(deviceId, (uint)index), buffer));
                    }
                }
                await device.FlushBuffersAsync(true, true).ConfigureAwait(true);
                await Task.WhenAll(subTasks.ToArray()).ConfigureAwait(true);

                subTasks.Clear();
                var loadBuffers = new List<VirtualBuffer>();
                foreach (var deviceId in deviceIds)
                {
                    for (var index = 0; index < 7; ++index)
                    {
                        var buffer = _bufferFactory.AllocateBuffer();
                        loadBuffers.Add(buffer);
                        subTasks.Add(device.LoadBufferAsync(
                            new VirtualPageId(deviceId, (uint)index), buffer));
                    }
                }
                await device.FlushBuffersAsync(true, true).ConfigureAwait(true);
                await Task.WhenAll(subTasks.ToArray()).ConfigureAwait(true);

                // Close the device
                await device.CloseAsync().ConfigureAwait(true);

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
            finally
            {
                foreach (var testFile in testFiles)
                {
                    File.Delete(testFile);
                }
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
            return new string[]
                {
                    "Device1.bin",
                    "Device2.bin",
                    "Device3.bin",
                    "Device4.bin",
                };
        }
    }
}
