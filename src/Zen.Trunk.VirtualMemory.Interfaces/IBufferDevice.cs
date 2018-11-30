using System;
using System.Threading.Tasks;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    [CLSCompliant(false)]
	public interface IBufferDevice : IDisposable
	{
        /// <summary>
        /// Gets the buffer factory.
        /// </summary>
        /// <value>
        /// The buffer factory.
        /// </value>
        IVirtualBufferFactory BufferFactory { get; }

        /// <summary>
        /// Opens the asynchronous.
        /// </summary>
        /// <returns></returns>
        Task OpenAsync();

        /// <summary>
        /// Closes the asynchronous.
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();

	    /// <summary>
	    /// Loads the page data from the physical page into the supplied buffer.
	    /// </summary>
	    /// <param name="pageId">The virtual page identifier.</param>
	    /// <param name="buffer">The buffer.</param>
	    /// <returns>
	    /// A <see cref="Task"/> representing the asynchronous operation.
	    /// </returns>
	    /// <remarks>
	    /// When scatter/gather I/O is enabled then the request is queued until
	    /// the device is flushed.
	    /// </remarks>
	    Task LoadBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer);

        /// <summary>
        /// Saves the page data from the supplied buffer to the physical page.
        /// </summary>
        /// <param name="pageId">The virtual page identifier.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// When scatter/gather I/O is enabled then the request is queued until
        /// the device is flushed.
        /// </remarks>
	    Task SaveBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer);
	}
}
