// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScatterGatherRequestArray_should.cs" company="Zen Design Software">
//   Copyright © Zen Design Software 2020
// </copyright>
// <summary>
//   Zen.Trunk.NoInstaller.Zen.Trunk.VirtualMemory.Tests.ScatterGatherRequestArray_should.cs
//   Author:   Adrian Lewis
//   Created:  21:45 15/05/2020
//   Updated:  21:45 15/05/2020
// 
//   Summary description
//   (blah)
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Zen.Trunk.VirtualMemory.Tests
{
    [Trait("Subsystem", "Virtual Memory")]
    [Trait("Class", "ScatterGatherRequestArray")]
    // ReSharper disable once InconsistentNaming
    public class ScatterGatherRequestArray_should
    {
        private ISystemClock _systemClock = Mock.Of<ISystemClock>();
        private AdvancedStream _stream = Mock.Of<AdvancedStream>();

        // AddRequest succeeds with request that is one less than start block
        // AddRequest succeeds with request that is one more than end block
        // AddRequest fails with request that is two less than start block
        // AddRequest fails with request that is two more than end block
        [Theory(DisplayName = nameof(ScatterGatherRequestArray_should) + "_" + nameof(return_expected_when_addrequest_called_with_given_physical_page))]
        [InlineData(new object[] { 499, true })]
        [InlineData(new object[] { 501, true })]
        [InlineData(new object[] { 498, false })]
        [InlineData(new object[] { 502, false })]
        public void return_expected_when_addrequest_called_with_given_physical_page(uint requestPhysicalPageId, bool expectedResult)
        {
            // Arrange
            var sut = new ReadScatterRequestArray(
                _systemClock, 
                _stream,
                CreateRequest(500));

            // Act
            var result = sut.AddRequest(CreateRequest(requestPhysicalPageId));

            // Assert
            result.Should().Be(expectedResult);
        }

        // Consume succeeds with array that ends one less than the start block
        // Consume succeeds with array that starts one more than the end block
        // Consume fails with empty array
        // Consume fails with array that is not contiguous
        [Theory(DisplayName = nameof(ScatterGatherRequestArray_should) + "_" + nameof(return_expected_when_consume_called_with_given_physical_page_range))]
        [InlineData(new object[] { 490, 499, true })]
        [InlineData(new object[] { 505, 510, true })]
        [InlineData(new object[] { 490, 498, false })]
        [InlineData(new object[] { 506, 510, false })]
        public void return_expected_when_consume_called_with_given_physical_page_range(uint startPhysicalPageId, uint endPhysicalPageId, bool expectedResult)
        {
            // Arrange
            var sut = new ReadScatterRequestArray(
                _systemClock,
                _stream,
                CreateRequest(500));
            sut.AddRequest(CreateRequest(501));
            sut.AddRequest(CreateRequest(502));
            sut.AddRequest(CreateRequest(503));
            sut.AddRequest(CreateRequest(504));

            var newArray = new ReadScatterRequestArray(
                _systemClock,
                _stream,
                CreateRequest(startPhysicalPageId));
            for (var index = startPhysicalPageId + 1; index <= endPhysicalPageId; ++index)
            {
                newArray.AddRequest(CreateRequest(index));
            }

            // Act
            var result = sut.Consume(newArray);

            // Assert
            result.Should().Be(expectedResult);
        }

        // RequireFlush returns false if max age is less than threshold
        // RequireFlush returns false if max length is less than threshold
        // RequireFlush returns true if max age is greater than threshold
        // RequireFlush returns true if max length is greater than threshold
        [Theory(DisplayName = nameof(ScatterGatherRequestArray_should) + "_" + nameof(return_expected_when_requireflush_called_with_given_values))]
        [InlineData(new object[] { 5, 5, true })]
        [InlineData(new object[] { 15, 5, true })]
        [InlineData(new object[] { 5, 15, true })]
        [InlineData(new object[] { 15, 15, false })]
        public void return_expected_when_requireflush_called_with_given_values(int ageThreshold, int lengthThreshold, bool expectedResult)
        {
            // Arrange
            var clockQueue = new Queue<DateTime>();
            clockQueue.Enqueue(DateTime.UtcNow);
            clockQueue.Enqueue(DateTime.UtcNow.AddMinutes(10));

            Mock.Get(_systemClock)
                .SetupGet(sc => sc.UtcNow)
                .Returns(() => clockQueue.Dequeue());

            var sut = new ReadScatterRequestArray(
                _systemClock,
                _stream,
                CreateRequest(500));
            for (uint index = 0; index < 10; ++index)
            {
                sut.AddRequest(CreateRequest(501 + index));
            }

            // Act
            var result = sut.RequiresFlush(TimeSpan.FromMinutes(ageThreshold), lengthThreshold);

            // Assert
            result.Should().Be(expectedResult);
        }

        private static ScatterGatherRequest CreateRequest(uint physicalPageId)
        {
            return new ScatterGatherRequest(physicalPageId, Mock.Of<IVirtualBuffer>());
        }
    }
}