using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>IMultipleBufferDevice</c> represents a page device mapped to multiple files.
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.IBufferDevice" />
    public interface IMultipleBufferDevice : IBufferDevice
	{
		/// <summary>
		/// Adds a new child single buffer device to this instance.
		/// </summary>
		/// <param name="name">The device name.</param>
		/// <param name="pathName">The pathname for the device storage.</param>
		/// <returns>The device id.</returns>
		/// <remarks>
		/// <para>
		/// If the device id is zero (it must be zero for the first device)
		/// then the next available device id is used and the value returned.
		/// If the createPageCount is non-zero then the add device call is
		/// treated as a request to create the underlying storage otherwise
		/// the call is treated as a request to open the underlying storage.
		/// </para>
		/// </remarks>
		Task<DeviceId> AddDeviceAsync(string name, string pathName);

        /// <summary>
        /// Adds a new child single buffer device to this instance.
        /// </summary>
        /// <param name="name">The device name.</param>
        /// <param name="pathName">The pathname for the device storage.</param>
        /// <param name="deviceId">The device unique identifier.</param>
        /// <param name="createPageCount">The create page count.</param>
        /// <returns>The device id.</returns>
        /// <remarks>
        /// <para>
        /// If the device id is zero (it must be zero for the first device)
        /// then the next available device id is used and the value returned.
        /// If the createPageCount is non-zero then the add device call is
        /// treated as a request to create the underlying storage otherwise
        /// the call is treated as a request to open the underlying storage.
        /// </para>
        /// </remarks>
        Task<DeviceId> AddDeviceAsync(string name, string pathName, DeviceId deviceId, uint createPageCount = 0);

        /// <summary>
        /// Removes a child single-buffer device from this instance.
        /// </summary>
        /// <param name="deviceId">The device unique identifier.</param>
        /// <returns></returns>
        Task RemoveDeviceAsync(DeviceId deviceId);

        /// <summary>
        /// Resizes the specified device to the soecified number of pages.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="pageCount">The page count.</param>
        void ResizeDevice(DeviceId deviceId, uint pageCount);

        /// <summary>
        /// Loads the page data from the physical page into the supplied buffer.
        /// </summary>
		/// <param name="pageId">The virtual page identifier.</param>
		/// <param name="buffer">The buffer.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// When scatter/gather I/O is enabled then the save is deferred until
        /// pending requests are flushed via <see cref="FlushBuffersAsync"/>.
        /// </remarks>
        Task LoadBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer);

        /// <summary>
        /// Saves the page data from the supplied buffer to the physical page.
        /// </summary>
		/// <param name="pageId">The virtual page identifier.</param>
		/// <param name="buffer">The buffer.</param>
		/// <returns></returns>
		Task SaveBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer);

        /// <summary>
        /// Flushes pending buffer operations.
        /// </summary>
        /// <param name="flushReads">
        /// if set to <c>true</c> then read operations are flushed.
        /// </param>
        /// <param name="flushWrites">
        /// if set to <c>true</c> then write operations are flushed.
        /// </param>
        /// <param name="deviceIds">
        /// An optional list of device identifiers to restrict flush operation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task FlushBuffersAsync(bool flushReads, bool flushWrites, params DeviceId[] deviceIds);

		/// <summary>
		/// Gets the device information for all child devices.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IBufferDeviceInfo> GetDeviceInfo();

		/// <summary>
		/// Gets the device information for a single child device.
		/// </summary>
		/// <param name="deviceId">The device unique identifier.</param>
		/// <returns></returns>
		IBufferDeviceInfo GetDeviceInfo(DeviceId deviceId);
	}
}
