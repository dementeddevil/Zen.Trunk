namespace Zen.Trunk.Storage
{
	using System.Collections.Generic;
	using System.Threading.Tasks;

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

		uint ExpandDevice(DeviceId deviceId, int pageCount);

		/// <summary>
		/// Asynchronously loads a buffer from the device and page associated
		/// with the specified pageId.
		/// </summary>
		/// <param name="pageId"></param>
		/// <param name="buffer"></param>
		/// <returns></returns>
		Task LoadBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer);

		/// <summary>
		/// Asynchronously saves a buffer to the device and page associated
		/// with the specified pageId.
		/// </summary>
		/// <param name="pageId">The page unique identifier.</param>
		/// <param name="buffer">The buffer.</param>
		/// <returns></returns>
		Task SaveBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer);

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
