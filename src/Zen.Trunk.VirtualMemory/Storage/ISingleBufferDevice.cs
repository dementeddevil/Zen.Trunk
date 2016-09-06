using System.Threading.Tasks;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.IBufferDevice" />
    public interface ISingleBufferDevice : IBufferDevice
	{
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name
		{
			get;
		}

        /// <summary>
        /// Gets the page count.
        /// </summary>
        /// <value>
        /// The page count.
        /// </value>
        uint PageCount
		{
			get;
		}

        /// <summary>
        /// Loads the buffer asynchronous.
        /// </summary>
        /// <param name="physicalPageId">The physical page identifier.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        Task LoadBufferAsync(uint physicalPageId, IVirtualBuffer buffer);

        /// <summary>
        /// Saves the buffer asynchronous.
        /// </summary>
        /// <param name="physicalPageId">The physical page identifier.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        Task SaveBufferAsync(uint physicalPageId, IVirtualBuffer buffer);

        /// <summary>
        /// Flushes the buffers asynchronous.
        /// </summary>
        /// <param name="flushReads">if set to <c>true</c> [flush reads].</param>
        /// <param name="flushWrites">if set to <c>true</c> [flush writes].</param>
        /// <returns></returns>
        Task FlushBuffersAsync(bool flushReads, bool flushWrites);

        /// <summary>
        /// Expands the device.
        /// </summary>
        /// <param name="pageCount">The page count.</param>
        /// <returns></returns>
        uint ExpandDevice(int pageCount);
	}
}
