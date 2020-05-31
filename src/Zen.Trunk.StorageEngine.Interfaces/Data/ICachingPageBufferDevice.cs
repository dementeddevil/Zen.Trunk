using System;
using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// <c>ICachingPageBufferDevice</c> represents the contract implemented by
    /// <see cref="CachingPageBufferDevice"/> that manages the loading, saving
    /// and caching of <see cref="PageBuffer"/> objects.
    /// </summary>
    public interface ICachingPageBufferDevice : IDisposable
    {
        /// <summary>
        /// Closes this instance.
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();

        /// <summary>
        /// Adds a file to the underlying multiple buffer device.
        /// </summary>
        /// <param name="name">The name of the device.</param>
        /// <param name="pathName">The pathname of the associated file.</param>
        /// <param name="deviceId">
        /// The device id for the new device or <see cref="DeviceId.Zero"/> 
        /// if the device identifier should be automatically determined.
        /// </param>
        /// <param name="createPageCount">
        /// The number of pages to allocate when creating the file;
        /// if the file exists then set this to zero.
        /// </param>
        /// <returns>
        /// A <see cref="DeviceId"/> representing the new device.
        /// </returns>
        Task<DeviceId> AddDeviceAsync(string name, string pathName, DeviceId deviceId, uint createPageCount = 0);

        /// <summary>
        /// Removes the file associated with the given device identifier.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RemoveDeviceAsync(DeviceId deviceId);

        /// <summary>
        /// Returns an initialised <see cref="PageBuffer"/> associated with the
        /// specified <see cref="VirtualPageId"/>.
        /// </summary>
        /// <param name="pageId">The virtual page identifier.</param>
        /// <returns>
        /// An instance of <see cref="PageBuffer"/>.
        /// </returns>
        Task<IPageBuffer> InitPageAsync(VirtualPageId pageId);

        /// <summary>
        /// Returns a loaded <see cref="PageBuffer"/> associated with the
        /// specified <see cref="VirtualPageId"/>.
        /// </summary>
        /// <param name="pageId">The virtual page identifier.</param>
        /// <returns>
        /// An instance of <see cref="PageBuffer"/>.
        /// </returns>
        /// <remarks>
        /// This method will not return the page buffer until one of the
        /// following has occurred;
        /// 1. the instance has it's pending reads flushed
        /// 2. the queue of pending operations exceeds a certain threshold
        /// 3. a read timeout occurs
        /// </remarks>
        Task<IPageBuffer> LoadPageAsync(VirtualPageId pageId);

        /// <summary>
        /// Flushes pending operations.
        /// </summary>
        /// <param name="flushParams"></param>
        /// <returns></returns>
        /// <remarks>
        /// If reads are flushed then all pending calls to <see cref="LoadPageAsync"/>
        /// will be completed.
        /// </remarks>
        Task FlushPagesAsync(FlushCachingDeviceParameters flushParams);
    }
}