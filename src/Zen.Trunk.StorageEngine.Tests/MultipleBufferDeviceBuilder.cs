using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    public class MultipleBufferDeviceBuilder
    {
        private readonly Mock<IMultipleBufferDevice> _bufferDevice = new Mock<IMultipleBufferDevice>();

        private MultipleBufferDeviceBuilder()
        {
        }

        public static MultipleBufferDeviceBuilder New()
        {
            return new MultipleBufferDeviceBuilder();
        }

        public MultipleBufferDeviceBuilder UsingBufferFactory(IVirtualBufferFactory bufferFactory)
        {
            _bufferDevice
                .SetupGet(mbd => mbd.BufferFactory)
                .Returns(bufferFactory);
            return this;
        }

        public MultipleBufferDeviceBuilder AddKnownDevice(DeviceId deviceId, string deviceName)
        {
            _bufferDevice
                .Setup(mbd => mbd.AddDeviceAsync(
                    deviceName, It.IsAny<string>(), It.IsAny<DeviceId>(), It.IsAny<uint>()))
                .ReturnsAsync(deviceId);
            return this;
        }

        public MultipleBufferDeviceBuilder AddDeviceBuffers(DeviceId deviceId, IList<IVirtualBuffer> buffers)
        {
            _bufferDevice
                .Setup(mbd => mbd.LoadBufferAsync(
                    It.Is<VirtualPageId>(vid => vid.DeviceId == deviceId && vid.PhysicalPageId < buffers.Count),
                    It.IsAny<IVirtualBuffer>()))
                .Callback<VirtualPageId, IVirtualBuffer>(
                    (vid, buffer) =>
                    {
                        buffers[(int)vid.PhysicalPageId].CopyTo(buffer);
                    })
                .Returns(Task.CompletedTask);
            _bufferDevice
                .Setup(mbd => mbd.SaveBufferAsync(
                    It.Is<VirtualPageId>(vid => vid.DeviceId == deviceId && vid.PhysicalPageId < buffers.Count),
                    It.IsAny<IVirtualBuffer>()))
                .Callback<VirtualPageId, IVirtualBuffer>(
                    (vid, buffer) =>
                    {
                        buffer.CopyTo(buffers[(int)vid.PhysicalPageId]);
                    })
                .Returns(Task.CompletedTask);
            return this;
        }

        public Mock<IMultipleBufferDevice> Build()
        {
            return _bufferDevice;
        }
    }
}