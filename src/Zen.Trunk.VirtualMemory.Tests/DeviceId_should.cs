using FluentAssertions;
using Xunit;

namespace Zen.Trunk.VirtualMemory.Tests
{
    [Trait("Subsystem", "Virtual Memory")]
    [Trait("Class", "DeviceId")]
    // ReSharper disable once InconsistentNaming
    public class DeviceId_should
    {
        [Theory]
        [InlineData(4, false)]
        [InlineData(5, true)]
        [InlineData(6, false)]
        public void HandleEqualityCorrectly(ushort deviceId, bool expectedToBeEqual)
        {
            var sut = new DeviceId(5);
            sut.Equals(new DeviceId(deviceId)).Should().Be(expectedToBeEqual);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 3)]
        [InlineData(3, 4)]
        [InlineData(4, 5)]
        public void ReturnNextDeviceIdWhenCalled(ushort candidate, ushort expected)
        {
            var sut = new DeviceId(candidate);
            sut.Next.Equals(new DeviceId(expected)).Should().BeTrue();
        }

        [Theory]
        [InlineData(3, 4, 1)]
        [InlineData(4, 4, 0)]
        [InlineData(5, 4, -1)]
        public void CompareCorrectly(ushort lhs, ushort rhs, int expected)
        {
            new DeviceId(lhs).CompareTo(new DeviceId(rhs)).Should().Be(expected);
        }
    }
}
