using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Moq;
using Xunit;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    [Trait("Subsystem", "Storage Engine")]
    // ReSharper disable once InconsistentNaming
    public class CachingPageBufferDevice_should : IClassFixture<StorageEngineTestFixture>
    {
        private readonly ILifetimeScope _scope;
        private readonly List<IVirtualBuffer> _primaryDeviceBuffers = new List<IVirtualBuffer>();
        private readonly List<IVirtualBuffer> _secondaryDeviceBuffers = new List<IVirtualBuffer>();

        public CachingPageBufferDevice_should(StorageEngineTestFixture fixture)
        {
            _scope = fixture.Scope.BeginLifetimeScope(
                builder =>
                {
                    builder
                        .Register(scope =>
                            new CachingPageBufferDevice(
                                MockedMultipleBufferDevice.Object,
                                scope.Resolve<CachingPageBufferDeviceSettings>()))
                        .As<ICachingPageBufferDevice>()
                        .SingleInstance();
                });

            var bufferFactory = _scope.Resolve<IVirtualBufferFactory>();
            for (int index = 0; index < 16; ++index)
            {
                _primaryDeviceBuffers.Add(bufferFactory.AllocateBuffer());
                _secondaryDeviceBuffers.Add(bufferFactory.AllocateBuffer());
            }

            MockedMultipleBufferDevice = MultipleBufferDeviceBuilder.New()
                .UsingBufferFactory(bufferFactory)
                .AddKnownDevice(TestCases.PrimaryDeviceId, TestCases.PrimarayDeviceName)
                .AddKnownDevice(TestCases.SecondaryDeviceId, TestCases.SecondaryDeviceName)
                .AddDeviceBuffers(TestCases.PrimaryDeviceId, _primaryDeviceBuffers)
                .AddDeviceBuffers(TestCases.SecondaryDeviceId, _secondaryDeviceBuffers)
                .Build();

            //MockedMultipleBufferDevice
            //    .SetupGet(mbd => mbd.BufferFactory)
            //    .Returns(bufferFactory);

            //MockedMultipleBufferDevice
            //    .Setup(mbd => mbd.AddDeviceAsync(
            //        TestCases.PrimarayDeviceName, It.IsAny<string>(), It.IsAny<DeviceId>(), It.IsAny<uint>()))
            //    .ReturnsAsync(TestCases.PrimaryDeviceId);
            //MockedMultipleBufferDevice
            //    .Setup(mbd => mbd.AddDeviceAsync(
            //        TestCases.SecondaryDeviceName, It.IsAny<string>(), It.IsAny<DeviceId>(), It.IsAny<uint>()))
            //    .ReturnsAsync(TestCases.SecondaryDeviceId);
            //MockedMultipleBufferDevice
            //    .Setup(mbd => mbd.LoadBufferAsync(
            //        It.IsAny<VirtualPageId>(), It.IsAny<IVirtualBuffer>()))
            //    .Callback<VirtualPageId, IVirtualBuffer>(
            //        (vid, buffer) =>
            //        {
            //            if (vid.DeviceId == TestCases.PrimaryDeviceId && vid.PhysicalPageId < _primaryDeviceBuffers.Count)
            //            {
            //                _primaryDeviceBuffers[(int)vid.PhysicalPageId].CopyTo(buffer);
            //            }
            //            else if (vid.DeviceId == TestCases.SecondaryDeviceId && vid.PhysicalPageId < _secondaryDeviceBuffers.Count)
            //            {
            //                _secondaryDeviceBuffers[(int)vid.PhysicalPageId].CopyTo(buffer);
            //            }
            //        })
            //    .Returns(Task.FromResult(true));
            //MockedMultipleBufferDevice
            //    .Setup(mbd => mbd.SaveBufferAsync(
            //        It.IsAny<VirtualPageId>(), It.IsAny<IVirtualBuffer>()))
            //    .Callback<VirtualPageId, IVirtualBuffer>(
            //        (vid, buffer) =>
            //        {
            //            if (vid.DeviceId == TestCases.PrimaryDeviceId && vid.PhysicalPageId < _primaryDeviceBuffers.Count)
            //            {
            //                buffer.CopyTo(_primaryDeviceBuffers[(int)vid.PhysicalPageId]);
            //            }
            //            else if (vid.DeviceId == TestCases.SecondaryDeviceId && vid.PhysicalPageId < _secondaryDeviceBuffers.Count)
            //            {
            //                buffer.CopyTo(_secondaryDeviceBuffers[(int)vid.PhysicalPageId]);
            //            }
            //        })
            //    .Returns(Task.FromResult(true));
        }

        private Mock<IMultipleBufferDevice> MockedMultipleBufferDevice { get; }

        [Theory(DisplayName = nameof(CachingPageBufferDevice_should) + "_" + nameof(verify_load_on_buffer_device_is_called_when_load_request_is_flushed))]
        [MemberData(nameof(TestCases.GetValidDevicePagesForLoad), MemberType = typeof(TestCases))]
        public async Task verify_load_on_buffer_device_is_called_when_load_request_is_flushed(DeviceId deviceId, uint physicalPage)
        {
            // Arrange
            using (var sut = _scope.Resolve<ICachingPageBufferDevice>())
            {
                // Act
                await sut
                        .LoadPageAsync(new VirtualPageId(deviceId, physicalPage))
                        .ConfigureAwait(true);
                await sut
                    .FlushPagesAsync(
                        new FlushCachingDeviceParameters(true, false, DeviceId.Zero))
                    .ConfigureAwait(true);
            }

            // Assert
            MockedMultipleBufferDevice
                .Verify(mbd => mbd.LoadBufferAsync(new VirtualPageId(deviceId, physicalPage), It.IsAny<IVirtualBuffer>()));
        }

        [Theory(DisplayName = nameof(CachingPageBufferDevice_should) + "_" + nameof(verify_save_on_buffer_device_is_called_when_save_request_is_flushed))]
        [MemberData(nameof(TestCases.GetValidDevicePagesForSave), MemberType = typeof(TestCases))]
        public async Task verify_save_on_buffer_device_is_called_when_save_request_is_flushed(DeviceId deviceId, uint physicalPage)
        {
            // Arrange
            TrunkTransactionContext.BeginTransaction(_scope);
            using (var sut = _scope.Resolve<ICachingPageBufferDevice>())
            {
                var pb = await sut
                        .LoadPageAsync(new VirtualPageId(deviceId, physicalPage))
                        .ConfigureAwait(true);
                await sut
                    .FlushPagesAsync(
                        new FlushCachingDeviceParameters(true, false, DeviceId.Zero))
                    .ConfigureAwait(true);

                // Act
                await pb.SetDirtyAsync().ConfigureAwait(true);
                await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);
                await sut
                    .FlushPagesAsync(
                        new FlushCachingDeviceParameters(false, true, DeviceId.Zero))
                    .ConfigureAwait(true);
            }

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

        public static IEnumerable<object[]> GetValidDevicePagesForLoad()
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

        public static IEnumerable<object[]> GetValidDevicePagesForSave()
        {
            yield return new object[] { PrimaryDeviceId, 10 };
            yield return new object[] { PrimaryDeviceId, 11 };
            yield return new object[] { PrimaryDeviceId, 12 };
            yield return new object[] { PrimaryDeviceId, 13 };
            yield return new object[] { PrimaryDeviceId, 14 };
            yield return new object[] { PrimaryDeviceId, 15 };
            yield return new object[] { PrimaryDeviceId, 16 };
            yield return new object[] { PrimaryDeviceId, 17 };

            yield return new object[] { SecondaryDeviceId, 10 };
            yield return new object[] { SecondaryDeviceId, 11 };
            yield return new object[] { SecondaryDeviceId, 12 };
            yield return new object[] { SecondaryDeviceId, 13 };
            yield return new object[] { SecondaryDeviceId, 14 };
            yield return new object[] { SecondaryDeviceId, 15 };
            yield return new object[] { SecondaryDeviceId, 16 };
            yield return new object[] { SecondaryDeviceId, 17 };
        }
    }
}
