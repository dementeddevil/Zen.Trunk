using System.Threading.Tasks;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>ISingleBufferDevice</c> represents a page device mapped to a single file.
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
        string Name { get; }

        /// <summary>
        /// Gets the pathname of the underlying file.
        /// </summary>
        /// <value>
        /// The pathname.
        /// </value>
        string Pathname { get; }

        /// <summary>
        /// Gets the page count.
        /// </summary>
        /// <value>
        /// The page count.
        /// </value>
        uint PageCount { get; }

        /// <summary>
        /// Loads the page data from the physical page into the supplied buffer.
        /// </summary>
        /// <param name="physicalPageId">The physical page identifier.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// When scatter/gather I/O is enabled then the load is deferred until
        /// pending requests are flushed via <see cref="FlushBuffersAsync"/>.
        /// </remarks>
        Task LoadBufferAsync(uint physicalPageId, IVirtualBuffer buffer);

        /// <summary>
        /// Saves the page data from the supplied buffer to the physical page.
        /// </summary>
        /// <param name="physicalPageId">The physical page identifier.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// When scatter/gather I/O is enabled then the save is deferred until
        /// pending requests are flushed via <see cref="FlushBuffersAsync"/>.
        /// </remarks>
        Task SaveBufferAsync(uint physicalPageId, IVirtualBuffer buffer);

        /// <summary>
        /// Flushes pending buffer operations.
        /// </summary>
        /// <param name="flushReads">
        /// if set to <c>true</c> then read operations are flushed.
        /// </param>
        /// <param name="flushWrites">
        /// if set to <c>true</c> then write operations are flushed.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task FlushBuffersAsync(bool flushReads, bool flushWrites);

        /// <summary>
        /// Expands the device.
        /// </summary>
        /// <param name="pageCount">The page count.</param>
        /// <returns></returns>
        uint ExpandDevice(int pageCount);
	}
}
