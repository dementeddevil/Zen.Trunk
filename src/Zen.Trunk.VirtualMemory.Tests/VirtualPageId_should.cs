// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VirtualPageId_should.cs" company="Zen Design Software">
//   Copyright © Zen Design Software 2019
// </copyright>
// <summary>
//   Zen.Trunk.NoInstaller.Zen.Trunk.VirtualMemory.Tests.VirtualPageId_should.cs
//   Author:   Adrian Lewis
//   Created:   //
//   Updated:  18:56 05/01/2019
// 
//   Summary description
//   (blah)
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using FluentAssertions;
using Xunit;

namespace Zen.Trunk.VirtualMemory.Tests
{
    [Trait("Subsystem", "Virtual Memory")]
    [Trait("Class", "VirtualPageId")]
    // ReSharper disable once InconsistentNaming
    public class VirtualPageId_should
    {
        [Theory]
        [InlineData(4, false)]
        [InlineData(5, true)]
        [InlineData(6, false)]
        public void HandleEqualityCorrectly(uint physicalPageId, bool expectedToBeEqual)
        {
            var sut = new VirtualPageId(5, physicalPageId);
            sut.Equals(new VirtualPageId(5, 5)).Should().Be(expectedToBeEqual);
        }

        [Theory]
        [InlineData(3, 100, 4, 100, -1)]
        [InlineData(4, 90, 4, 100, -1)]
        [InlineData(4, 100, 4, 100, 0)]
        [InlineData(4, 110, 4, 100, 1)]
        [InlineData(5, 100, 4, 100, 1)]
        public void CompareCorrectly(
            ushort lhsDeviceId,
            uint lhsPhysicalPageId,
            ushort rhsDeviceId,
            uint rhsPhysicalPageId,
            int expected)
        {
            new VirtualPageId(lhsDeviceId, lhsPhysicalPageId)
                .CompareTo(
                    new VirtualPageId(rhsDeviceId, rhsPhysicalPageId))
                .Should().Be(expected);
        }
    }
}