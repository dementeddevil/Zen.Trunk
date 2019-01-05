using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Xunit;

namespace Zen.Trunk.VirtualMemory.Tests
{
    /// <summary>
    /// Summary description for Virtual Memory Unit Test suite
    /// </summary>
    [Trait("Subsystem", "Virtual Memory")]
    [Trait("Class", "Single Device")]
    public class VirtualMemoryUnitTests : IClassFixture<VirtualMemoryTestFixture>
    {
        private readonly VirtualMemoryTestFixture _fixture;

        public VirtualMemoryUnitTests(VirtualMemoryTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact(DisplayName = @"
Given a virtual buffer factory,
When 9 threads allocate and deallocate 1000 buffers simultaneously,
Then no corruption or deadlocks occur")]
        public void AllocateAndDeallocateTest()
        {
            var parallelRequests = new List<Task>();
            for (var tasks = 0; tasks < 9; ++tasks)
            {
                parallelRequests.Add(Task.Factory.StartNew(
                    () =>
                    {
                        var bufferList = new List<IVirtualBuffer>();
                        try
                        {
                            for (var index = 0; index < 1000; ++index)
                            {
                                bufferList.Add(_fixture.BufferFactory.AllocateBuffer());
                            }
                        }
                        catch (OutOfMemoryException)
                        {
                            Debug.WriteLine("VirtualBufferFactory: Memory exhausted.");
                        }
                        Thread.Sleep(1000);
                        foreach (var buffer in bufferList)
                        {
                            buffer.Dispose();
                        }
                    }));
            }
            Task.WaitAll(parallelRequests.ToArray());
        }

        [Fact(DisplayName = 
            @"Given a ScatterGatherRequestQueue configured to auto-flush after 5 seconds
              When data is written to queue
              Then data is not written immediately but only after timeout has occurred")]
        public async Task WriteBufferAsync_WritesToStreamAfterTimeout()
        {
            using (var stream = new FakeAdvancedStream())
            {
                var settings =
                    new ScatterGatherRequestQueueSettings
                    {
                        AutomaticFlushPeriod = TimeSpan.FromSeconds(5)
                    };
                using (var sut = new ScatterGatherRequestQueue(_fixture.Scope.Resolve<ISystemClock>(), stream, settings))
                {
                    var tasks =
                        new[]
                        {
                            sut.WriteBufferAsync(0, _fixture.BufferFactory.AllocateAndFill(0)),
                            sut.WriteBufferAsync(1, _fixture.BufferFactory.AllocateAndFill(1)),
                            sut.WriteBufferAsync(2, _fixture.BufferFactory.AllocateAndFill(2)),
                            sut.WriteBufferAsync(3, _fixture.BufferFactory.AllocateAndFill(3))
                        };

                    stream.BuffersWritten.Should().BeEmpty();
                    await Task.Delay(7).ConfigureAwait(true);
                    await Task.WhenAll(tasks).ConfigureAwait(true);
                    stream.BuffersWritten.Should().NotBeEmpty();
                }
            }
        }

        [Fact(DisplayName = "Given buffer, when GetBufferStream is called and released, then no exception is thrown")]
        public void VirtualBufferGetAndReleaseStream()
        {
            using (var buffer = _fixture.BufferFactory.AllocateBuffer())
            {
                using (var stream = buffer.GetBufferStream(0, 1024, true))
                {
                    stream.WriteByte(10);
                }
            }
        }
    }
}