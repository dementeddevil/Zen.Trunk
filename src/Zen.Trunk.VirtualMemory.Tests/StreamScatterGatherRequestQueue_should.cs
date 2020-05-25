// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StreamScatterGatherRequestQueue_should.cs" company="Zen Design Software">
//   Copyright © Zen Design Software 2020
// </copyright>
// <summary>
//   Zen.Trunk.NoInstaller.Zen.Trunk.VirtualMemory.Tests.StreamScatterGatherRequestQueue_should.cs
//   Author:   Adrian Lewis
//   Created:  00:23 16/05/2020
//   Updated:  00:23 16/05/2020
// 
//   Summary description
//   (blah)
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Moq;
using Xunit;

namespace Zen.Trunk.VirtualMemory.Tests
{
    [Trait("Subsystem", "Virtual Memory")]
    [Trait("Class", "StreamScatterGatherRequestQueue")]
    // ReSharper disable once InconsistentNaming
    public class StreamScatterGatherRequestQueue_should
    {
        private ISystemClock _systemClock = Mock.Of<ISystemClock>();
        private AdvancedStream _stream = Mock.Of<AdvancedStream>();

        public StreamScatterGatherRequestQueue_should()
        {
            Mock.Get(_stream)
                .Setup(
                    s => s.BeginReadScatter(
                        It.IsAny<IVirtualBuffer[]>(),
                        It.IsAny<AsyncCallback>(),
                        It.IsAny<object>()))
                .Returns(
                    () =>
                    {
                        var result = Mock.Of<IAsyncResult>();
                        Mock.Get(result)
                            .SetupGet(r => r.CompletedSynchronously)
                            .Returns(true);
                        Mock.Get(result)
                            .SetupGet(r => r.IsCompleted)
                            .Returns(true);
                        Mock.Get(result)
                            .SetupGet(r => r.AsyncWaitHandle)
                            .Returns(new ManualResetEvent(true));
                        return result;
                    });
        }

        // QueueBufferRequestAsync allocates new array when queue is empty
        // QueueBufferRequestAsync allocates new array when new request added that isn't contiguous
        // QueueBufferRequestAsync allocates updates existing array when request fits existing array
        [Theory(DisplayName = nameof(StreamScatterGatherRequestQueue_should) + "_" + nameof(allocate_array_when_queuebufferrequest_called_with_empty_queue))]
        [InlineData(500, 0, 1)]
        [InlineData(500, 550, 2)]
        [InlineData(500, 501, 1)]
        public void allocate_array_when_queuebufferrequest_called_with_empty_queue(uint firstRequest, uint secondRequest, int expectedArrayCount)
        {
            // Arrange
            var createdArrays = new List<ReadScatterRequestArray>();
            var clockQueue = new Queue<DateTime>();
            clockQueue.Enqueue(DateTime.UtcNow);
            clockQueue.Enqueue(DateTime.UtcNow.AddMinutes(10));

            Mock.Get(_systemClock)
                .SetupGet(sc => sc.UtcNow)
                .Returns(() => clockQueue.Dequeue());

            var sut = new StreamScatterGatherRequestQueue<ReadScatterRequestArray>(
                _systemClock,
                new StreamScatterGatherRequestQueueSettings
                {
                    CoalesceRequestsPeriod = TimeSpan.FromMinutes(30),
                    MaximumRequestAge = TimeSpan.FromMinutes(10),
                    MaximumRequestBlockLength = 10,
                    MaximumRequestBlocks = 10
                },
                request =>
                {
                    var newArray = new ReadScatterRequestArray(_systemClock, _stream, request);
                    createdArrays.Add(newArray);
                    return newArray;
                });

            // Act
            var dummyTask1 = sut.QueueBufferRequestAsync(firstRequest, Mock.Of<IVirtualBuffer>());
            if (secondRequest > 0)
            {
                var dummyTask2 = sut.QueueBufferRequestAsync(secondRequest, Mock.Of<IVirtualBuffer>());
            }

            // Assert
            createdArrays.Count.Should().Be(expectedArrayCount);
        }

        // coalesce adjacent arrays during optimised flush
        [Theory(DisplayName = nameof(StreamScatterGatherRequestQueue_should) + "_" + nameof(allocate_array_when_queuebufferrequest_called_with_empty_queue))]
        [InlineData(500, 0, 1, 1)]
        [InlineData(500, 550, 2, 2)]
        [InlineData(505, 500, 2, 1)]
        public void coalesce_adjacent_arrays_during_optimised_flush(uint firstRequest, uint secondRequest, int expectedArrayCount, int expectedNonEmptyArrayCount)
        {
            // Arrange
            var createdArrays = new List<ReadScatterRequestArray>();
            var clockQueue = new Queue<DateTime>();
            clockQueue.Enqueue(DateTime.UtcNow);
            clockQueue.Enqueue(DateTime.UtcNow);
            clockQueue.Enqueue(DateTime.UtcNow.AddMinutes(10));
            clockQueue.Enqueue(DateTime.UtcNow.AddMinutes(10));
            clockQueue.Enqueue(DateTime.UtcNow.AddMinutes(10));
            clockQueue.Enqueue(DateTime.UtcNow.AddMinutes(10));
            clockQueue.Enqueue(DateTime.UtcNow.AddMinutes(10));
            clockQueue.Enqueue(DateTime.UtcNow.AddMinutes(10));

            Mock.Get(_systemClock)
                .SetupGet(sc => sc.UtcNow)
                .Returns(() => clockQueue.Dequeue());

            var sut = new StreamScatterGatherRequestQueue<ReadScatterRequestArray>(
                _systemClock,
                new StreamScatterGatherRequestQueueSettings
                {
                    CoalesceRequestsPeriod = TimeSpan.FromMinutes(30),
                    MaximumRequestAge = TimeSpan.FromMinutes(10),
                    MaximumRequestBlockLength = 10,
                    MaximumRequestBlocks = 10
                },
                request =>
                {
                    var newArray = new ReadScatterRequestArray(_systemClock, _stream, request);
                    createdArrays.Add(newArray);
                    return newArray;
                });

            // Act
            for (uint index = 0; index < 5; ++index)
            {
                var dummyTask1 = sut.QueueBufferRequestAsync(firstRequest + index, Mock.Of<IVirtualBuffer>());
            }
            if (secondRequest > 0)
            {
                for (uint index = 0; index < 5; ++index)
                {
                    var dummyTask2 = sut.QueueBufferRequestAsync(secondRequest + index, Mock.Of<IVirtualBuffer>());
                }
            }

            var dummyTask3 = sut.OptimisedFlushAsync();

            // Assert
            createdArrays.Count.Should().Be(expectedArrayCount);
            createdArrays.Count(a => !a.IsEmpty).Should().Be(expectedNonEmptyArrayCount);
        }
    }
}