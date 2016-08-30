﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Moq;
using Xunit;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.IO;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    public class CachingPageBufferDeviceUnitTests : AutofacStorageEngineUnitTests
    {
        private readonly List<IVirtualBuffer> _primaryDeviceBuffers = new List<IVirtualBuffer>();
        private readonly List<IVirtualBuffer> _secondaryDeviceBuffers = new List<IVirtualBuffer>();
        private CachingPageBufferDevice _pageBufferDevice;

        public CachingPageBufferDeviceUnitTests()
        {
            var bufferFactory = Scope.Resolve<IVirtualBufferFactory>();
            for (int index = 0; index < 16; ++index)
            {
                _primaryDeviceBuffers.Add(bufferFactory.AllocateBuffer());
                _secondaryDeviceBuffers.Add(bufferFactory.AllocateBuffer());
            }

            MockedMultipleBufferDevice
                .SetupGet(mbd => mbd.BufferFactory)
                .Returns(bufferFactory);

            MockedMultipleBufferDevice
                .Setup(mbd => mbd.AddDeviceAsync(
                    TestCases.PrimarayDeviceName, It.IsAny<string>(), It.IsAny<DeviceId>(), It.IsAny<uint>()))
                .ReturnsAsync(TestCases.PrimaryDeviceId);
            MockedMultipleBufferDevice
                .Setup(mbd => mbd.AddDeviceAsync(
                    TestCases.SecondaryDeviceName, It.IsAny<string>(), It.IsAny<DeviceId>(), It.IsAny<uint>()))
                .ReturnsAsync(TestCases.SecondaryDeviceId);
            MockedMultipleBufferDevice
                .Setup(mbd => mbd.LoadBufferAsync(
                    It.IsAny<VirtualPageId>(), It.IsAny<IVirtualBuffer>()))
                .Callback<VirtualPageId, IVirtualBuffer>(
                    (vid, buffer) =>
                    {
                        if (vid.DeviceId == TestCases.PrimaryDeviceId && vid.PhysicalPageId < _primaryDeviceBuffers.Count)
                        {
                            _primaryDeviceBuffers[(int)vid.PhysicalPageId].CopyTo(buffer);
                        }
                        else if (vid.DeviceId == TestCases.SecondaryDeviceId && vid.PhysicalPageId < _secondaryDeviceBuffers.Count)
                        {
                            _secondaryDeviceBuffers[(int)vid.PhysicalPageId].CopyTo(buffer);
                        }
                    })
                .Returns(Task.FromResult(true));
            MockedMultipleBufferDevice
                .Setup(mbd => mbd.SaveBufferAsync(
                    It.IsAny<VirtualPageId>(), It.IsAny<IVirtualBuffer>()))
                .Callback<VirtualPageId, IVirtualBuffer>(
                    (vid, buffer) =>
                    {
                        if (vid.DeviceId == TestCases.PrimaryDeviceId && vid.PhysicalPageId < _primaryDeviceBuffers.Count)
                        {
                            buffer.CopyTo(_primaryDeviceBuffers[(int)vid.PhysicalPageId]);
                        }
                        else if (vid.DeviceId == TestCases.SecondaryDeviceId && vid.PhysicalPageId < _secondaryDeviceBuffers.Count)
                        {
                            buffer.CopyTo(_secondaryDeviceBuffers[(int)vid.PhysicalPageId]);
                        }
                    })
                .Returns(Task.FromResult(true));
        }

        private Mock<IMultipleBufferDevice> MockedMultipleBufferDevice { get; } = new Mock<IMultipleBufferDevice>();

        private CachingPageBufferDevice Sut
        {
            get
            {
                if (_pageBufferDevice == null)
                {
                    _pageBufferDevice = new CachingPageBufferDevice(MockedMultipleBufferDevice.Object, null);
                }
                return _pageBufferDevice;
            }
        }

        [Theory(DisplayName = "Given a valid load request, when flush is called, then the load method on MBD is called.")]
        [MemberData("GetValidDevicePages", MemberType = typeof(TestCases))]
        public async Task GivenAValidLoadRequest_WhenFlushIsCalled_ThenTheLoadMethodOnMBDIsCalled(DeviceId deviceId, uint physicalPage)
        {
            // Arrange
            // Act
            var pb = await Sut
                .LoadPageAsync(new VirtualPageId(deviceId, physicalPage))
                .ConfigureAwait(true);
            await Sut
                .FlushPagesAsync(
                    new FlushCachingDeviceParameters(true, false, DeviceId.Zero))
                .ConfigureAwait(true);

            // Assert
            MockedMultipleBufferDevice
                .Verify(mbd => mbd.LoadBufferAsync(new VirtualPageId(deviceId, physicalPage), It.IsAny<IVirtualBuffer>()));
        }

        [Theory(DisplayName = "Given a valid loaded and dirty buffer, when the current transaction is committed and flush is called, then the save method on MBD is called.")]
        [MemberData("GetValidDevicePages", MemberType = typeof(TestCases))]
        public async Task GivenAValidSaveRequest_WhenFlushIsCalled_ThenTheLoadMethodOnMBDIsCalled(DeviceId deviceId, uint physicalPage)
        {
            TrunkTransactionContext.BeginTransaction(Scope);

            // Arrange
            var pb = await Sut
                .LoadPageAsync(new VirtualPageId(deviceId, physicalPage))
                .ConfigureAwait(true);
            await Sut
                .FlushPagesAsync(
                    new FlushCachingDeviceParameters(true, false, DeviceId.Zero))
                .ConfigureAwait(true);

            // Act
            await pb.SetDirtyAsync().ConfigureAwait(true);
            await TrunkTransactionContext.Commit().ConfigureAwait(true);
            await Sut
                .FlushPagesAsync(
                    new FlushCachingDeviceParameters(false, true, DeviceId.Zero))
                .ConfigureAwait(true);

            // Assert
            MockedMultipleBufferDevice
                .Verify(mbd => mbd.LoadBufferAsync(new VirtualPageId(deviceId, physicalPage), It.IsAny<IVirtualBuffer>()), Times.Once);
            MockedMultipleBufferDevice
                .Verify(mbd => mbd.LoadBufferAsync(new VirtualPageId(deviceId, physicalPage), It.IsAny<IVirtualBuffer>()), Times.Once);
        }
    }

    public static class TestCases
    {
        public static DeviceId PrimaryDeviceId => new DeviceId(1);

        public static string PrimarayDeviceName => "master";

        public static DeviceId SecondaryDeviceId => new DeviceId(2);

        public static string SecondaryDeviceName => "foobar";

        public static IEnumerable<object[]> GetValidDevicePages()
        {
            yield return new object[] { PrimaryDeviceId, 0 };
            yield return new object[] { PrimaryDeviceId, 1 };
            yield return new object[] { PrimaryDeviceId, 2 };
            yield return new object[] { PrimaryDeviceId, 3 };
            yield return new object[] { PrimaryDeviceId, 4 };
            yield return new object[] { PrimaryDeviceId, 5 };
            yield return new object[] { PrimaryDeviceId, 6 };
            yield return new object[] { PrimaryDeviceId, 7 };

            yield return new object[] { SecondaryDeviceId, 0 };
            yield return new object[] { SecondaryDeviceId, 1 };
            yield return new object[] { SecondaryDeviceId, 2 };
            yield return new object[] { SecondaryDeviceId, 3 };
            yield return new object[] { SecondaryDeviceId, 4 };
            yield return new object[] { SecondaryDeviceId, 5 };
            yield return new object[] { SecondaryDeviceId, 6 };
            yield return new object[] { SecondaryDeviceId, 7 };
        }
    }
}