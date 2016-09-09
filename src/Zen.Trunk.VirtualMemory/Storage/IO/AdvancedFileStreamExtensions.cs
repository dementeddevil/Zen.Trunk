using System.Threading.Tasks;

namespace Zen.Trunk.Storage.IO
{
    /// <summary>
    /// 
    /// </summary>
    public static class AdvancedFileStreamExtensions
	{
        /// <summary>
        /// Writes the specified buffers to the associated stream using scatter/gather I/O.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="buffers">The buffers.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// The operation is not completed until the underlying stream is flushed.
        /// </remarks>
        public static Task WriteGatherAsync(
			this AdvancedFileStream stream, IVirtualBuffer[] buffers)
		{
			return Task.Factory.FromAsync(
				stream.BeginWriteGather, stream.EndWriteGather, buffers, null);
		}

        /// <summary>
        /// Reads the specified buffers from the associated stream using scatter/gather I/O.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="buffers">The buffers.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// The operation is not completed until the underlying stream is flushed.
        /// </remarks>
        public static Task<int> ReadScatterAsync(
			this AdvancedFileStream stream, IVirtualBuffer[] buffers)
		{
			return Task<int>.Factory.FromAsync(
				stream.BeginReadScatter, stream.EndReadScatter, buffers, null);
		}
	}
}
