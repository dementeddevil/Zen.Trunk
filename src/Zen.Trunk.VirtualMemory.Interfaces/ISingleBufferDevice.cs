using System.Threading.Tasks;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// <c>ISingleBufferDevice</c> represents a page device mapped to a single file.
    /// </summary>
    /// <seealso cref="IBufferDevice" />
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
        /// Resizes the device to the specified number of pages.
        /// </summary>
        /// <param name="pageCount">The page count.</param>
        void Resize(uint pageCount);
	}
}
