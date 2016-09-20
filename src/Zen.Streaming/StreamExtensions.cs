// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StreamExtensions.cs" company="Zen Design Software">
//   © Zen Design Software 2015
// </copyright>
// <summary>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Zen.Streaming
{
    using System;
    using System.IO;

    /// <summary>
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Returns a <see cref="Stream" /> capable of seek and readonly operations.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="useVirtualStreamAsBackingStore">
        /// if set to <c>true</c> then a <see cref="VirtualStream" /> stream is
        /// used as the backing store to minimise memory usage for large streams.
        /// If data needs to be buffered to support seeking then the data will first be
        /// stored in-memory and, if necessary, promoted to disk storage if the
        /// supplied threshold is exceeded.
        /// if set to <c>false</c> then a temporary disk-file is used as
        /// the backing store.
        /// </param>
        /// <param name="overflowToDiskThresholdBytes">
        /// The overflow automatic disk
        /// threshold bytes.
        /// </param>
        /// <returns>
        /// A <see cref="Stream" /> that is read-only and seekable.
        /// If the original stream already has seek capability then it is returned
        /// unwrapped.
        /// </returns>
        /// <remarks>
        /// This method is useful in situations where a forward-only stream must
        /// be manipulated by client code.
        /// </remarks>
        public static Stream AsReadOnlySeekableStream(
            this Stream stream,
            int bufferSize = 4096,
            bool useVirtualStreamAsBackingStore = true,
            int overflowToDiskThresholdBytes = 16384)
        {
            if (stream.CanSeek)
            {
                return stream;
            }
            if (!useVirtualStreamAsBackingStore)
            {
                return new ReadOnlySeekableStream(stream, bufferSize);
            }
            Stream backingStore = new VirtualStream(bufferSize, overflowToDiskThresholdBytes);
            return new ReadOnlySeekableStream(stream, backingStore, bufferSize);
        }

        /// <summary>
        /// Returns a wrapped stream that will honour calls to the Length
        /// property and return the specified value.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="fixedStreamLength">Length of the fixed stream.</param>
        /// <returns></returns>
        public static Stream AsFixedLengthStream(this Stream stream, long fixedStreamLength)
        {
            return new FixedLengthStream(stream, fixedStreamLength);
        }

        /*/// <summary>
        /// Returns a wrapped stream capable of readonly seek operations that
        /// is sharable across multiple threads.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public static Stream AsMTAReadOnlySeekableStream(this Stream stream)
        {
            return new MultiThreadAccessReadOnlySeekableStream(stream);
        }*/

        /// <summary>
        /// Returns a wrapped stream that will execute the specified action
        /// when the stream is disposed.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="action">The disposal action.</param>
        /// <returns></returns>
        public static Stream AsStreamWithDisposeHandler(this Stream stream, Action action)
        {
            var result = new DelegatingStream(stream);
            EventHandler disposeHandler = null;
            disposeHandler = (sender, args) =>
                {
                    // Disconnect event handler first
                    ((DelegatingStream)sender).Disposed -= disposeHandler;

                    // Execute the action last
                    action();
                };
            result.Disposed += disposeHandler;
            return result;
        }
    }
}