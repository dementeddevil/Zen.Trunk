using System;
using System.IO;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// <c>AdvancedStream</c> extends the framework <see cref="Stream"/> by
    /// providing hook points for performing scatter/gather I/O operations.
    /// </summary>
    /// <remarks>
    /// Scatter/gather I/O is an NTFS feature that relies upon use of specially
    /// crafted memory blocks to achieve the highest performance file operations.
    /// </remarks>
    public abstract class AdvancedStream : Stream
    {
        /// <summary>
        /// Begins an asynchronous write that will write the associated buffer
        /// collection in a single NTFS gathered write operation.
        /// </summary>
        /// <param name="buffers">A collection of <see cref="T:VirtualBuffer"/> objects.</param>
        /// <param name="callback">The user callback.</param>
        /// <param name="state">The state object.</param>
        /// <returns>An <see cref="T:IAsyncResult"/> object.</returns>
        public abstract IAsyncResult BeginReadScatter(
            IVirtualBuffer[] buffers, AsyncCallback callback, object state);

        /// <summary>
        /// Ends an asynchronous read scatter operation.
        /// </summary>
        /// <returns>
        /// The number of bytes read from the stream, between zero (0) and
        /// the number of bytes you requested. Streams return zero (0) only 
        /// at the end of the stream, otherwise, they should block until at
        /// least one byte is available.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">
        /// asyncResult did not originate from a <see cref="M:BeginRead(System.Byte[],System.Int32,System.Int32,System.AsyncCallback,System.Object)"></see>
        /// method on the current stream.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// asyncResult is null.
        /// </exception>
        public abstract int EndReadScatter(IAsyncResult asyncResult);

        /// <summary>
        /// Begins an asynchronous write that will write the associated buffer
        /// collection in a single NTFS gathered write operation.
        /// </summary>
        /// <param name="buffers">A collection of <see cref="T:VirtualBuffer"/> objects.</param>
        /// <param name="callback">The user callback.</param>
        /// <param name="state">The state object.</param>
        /// <returns>An <see cref="T:IAsyncResult"/> object.</returns>
        public abstract IAsyncResult BeginWriteGather(
            IVirtualBuffer[] buffers, AsyncCallback callback, object state);

        /// <summary>
        /// Ends an asynchronous write gather operation.
        /// </summary>
        /// <param name="asyncResult">A reference to the outstanding asynchronous I/O request.</param>
        /// <exception cref="T:System.ArgumentNullException">asyncResult is null. </exception>
        /// <exception cref="T:System.ArgumentException">asyncResult did not originate from a <see cref="M:System.IO.Stream.BeginWrite(System.Byte[],System.Int32,System.Int32,System.AsyncCallback,System.Object)"></see> method on the current stream. </exception>
        public abstract void EndWriteGather(IAsyncResult asyncResult);
    }
}