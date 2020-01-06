using System.Threading.Tasks;
using Autofac;
using Xunit;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.VirtualMemory;
using Zen.Trunk.VirtualMemory.Tests;

namespace Zen.Trunk.Storage
{
    [Trait("Subsystem", "Storage Engine")]
    [Trait("Class", "Page Buffer")]
    // ReSharper disable once InconsistentNaming
    public class PageBuffer_should : IClassFixture<StorageEngineTestFixture>
    {
        private readonly StorageEngineTestFixture _fixture;

        public PageBuffer_should(StorageEngineTestFixture fixture)
        {
            _fixture = fixture;
        }

        public IBufferDeviceFactory BufferDeviceFactory => _fixture.Scope.Resolve<IBufferDeviceFactory>();

        [Fact(DisplayName = "Validate page buffer switches state when init then free")]
        public async Task change_state_from_free_to_allocated_when_initialised()
        {
            using (var tracker = new TempFileTracker())
            {
                using (var device = BufferDeviceFactory.CreateSingleBufferDevice(
                    "master", tracker.Get($"{nameof(change_state_from_free_to_allocated_when_initialised)}.dat"), 8, true))
                {
                    TrunkTransactionContext.BeginTransaction(_fixture.Scope);

                    // Create buffer and call addref
                    using (var pageBuffer = new PageBuffer(device))
                    {
                        pageBuffer.AddRef();

                        Assert.True(pageBuffer.CurrentStateType == PageBuffer.PageBufferStateType.Free);

                        // Execute buffer actions
                        await pageBuffer
                            .InitAsync(new VirtualPageId(DeviceId.Zero, 0), new LogicalPageId(1))
                            .ConfigureAwait(true);
                        Assert.True(pageBuffer.CurrentStateType == PageBuffer.PageBufferStateType.Allocated);

                        await pageBuffer
                            .SetFreeAsync()
                            .ConfigureAwait(true);
                        Assert.True(pageBuffer.CurrentStateType == PageBuffer.PageBufferStateType.Free);

                        // Release buffer and verify it has disposed
                        pageBuffer.Release();

                        await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);
                    }
                }
            }
        }

        [Fact(DisplayName = "Validate page buffer switches state when init then load")]
        public async Task change_state_appropriately_when_loaded_and_freed()
        {
            using (var tracker = new TempFileTracker())
            {
                using (var device = BufferDeviceFactory.CreateSingleBufferDevice(
                    "master", tracker.Get($"{nameof(change_state_appropriately_when_loaded_and_freed)}.dat"), 8, true))
                {
                    await device.OpenAsync().ConfigureAwait(true);

                    TrunkTransactionContext.BeginTransaction(_fixture.Scope);

                    // Create buffer and call addref
                    using (var pageBuffer = new PageBuffer(device))
                    {
                        pageBuffer.AddRef();

                        Assert.True(pageBuffer.CurrentStateType == PageBuffer.PageBufferStateType.Free);

                        // Execute buffer actions
                        await pageBuffer
                            .RequestLoadAsync(new VirtualPageId(DeviceId.Zero, 0), new LogicalPageId(1))
                            .ConfigureAwait(true);
                        Assert.True(pageBuffer.CurrentStateType == PageBuffer.PageBufferStateType.PendingLoad);

                        await pageBuffer
                            .LoadAsync()
                            .ConfigureAwait(true);
                        Assert.True(pageBuffer.CurrentStateType == PageBuffer.PageBufferStateType.Allocated);

                        await pageBuffer
                            .SetFreeAsync()
                            .ConfigureAwait(true);
                        Assert.True(pageBuffer.CurrentStateType == PageBuffer.PageBufferStateType.Free);

                        // Release buffer and verify it has disposed
                        pageBuffer.Release();

                        await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);
                    }

                    await device.CloseAsync().ConfigureAwait(true);
                }
            }
        }

        [Fact(DisplayName = "Validate page buffer switches state when init, load, write and save")]
        public async Task ValidatePageBufferFreeThenLoadWriteSave()
        {
            using (var tracker = new TempFileTracker())
            {
                using (var device = BufferDeviceFactory.CreateSingleBufferDevice(
                    "master", tracker.Get($"{nameof(ValidatePageBufferFreeThenLoadWriteSave)}.dat"), 8, true))
                {
                    await device.OpenAsync().ConfigureAwait(true);

                    TrunkTransactionContext.BeginTransaction(_fixture.Scope);

                    // Create buffer and call addref
                    using (var pageBuffer = new PageBuffer(device))
                    {
                        pageBuffer.AddRef();
                        Assert.Equal(PageBuffer.PageBufferStateType.Free, pageBuffer.CurrentStateType);

                        // Execute buffer actions
                        await pageBuffer
                            .RequestLoadAsync(new VirtualPageId(DeviceId.Zero, 0), new LogicalPageId(1))
                            .ConfigureAwait(true);
                        Assert.Equal(PageBuffer.PageBufferStateType.PendingLoad, pageBuffer.CurrentStateType);

                        await pageBuffer.LoadAsync().ConfigureAwait(true);
                        Assert.Equal(PageBuffer.PageBufferStateType.Allocated, pageBuffer.CurrentStateType);

                        pageBuffer.EnlistInTransaction();

                        using (var stream = pageBuffer.GetBufferStream(0, 10, true))
                        {
                            stream.WriteByte(100);
                            stream.WriteByte(10);
                            stream.WriteByte(100);
                            stream.WriteByte(10);
                            stream.WriteByte(100);
                        }
                        Assert.True(pageBuffer.CurrentStateType != PageBuffer.PageBufferStateType.Dirty);
                        await pageBuffer.SetDirtyAsync().ConfigureAwait(true);
                        Assert.True(pageBuffer.CurrentStateType == PageBuffer.PageBufferStateType.Dirty);

                        await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);
                        Assert.Equal(PageBuffer.PageBufferStateType.AllocatedWritable, pageBuffer.CurrentStateType);

                        await pageBuffer.SaveAsync().ConfigureAwait(true);
                        Assert.Equal(PageBuffer.PageBufferStateType.Allocated, pageBuffer.CurrentStateType);

                        await pageBuffer.SetFreeAsync().ConfigureAwait(true);
                        Assert.Equal(PageBuffer.PageBufferStateType.Free, pageBuffer.CurrentStateType);

                        // Release buffer and verify it has disposed
                        pageBuffer.Release();
                    }

                    await device.CloseAsync().ConfigureAwait(true);
                }
            }
        }
    }
}
