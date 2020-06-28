using System;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// <c>VirtualBufferFactorySettings</c> defines configuration used to setup a <see cref="VirtualBufferFactory"/>.
    /// </summary>
    public class VirtualBufferFactorySettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualBufferFactorySettings"/> class.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="reservationPageCount">The reservation page count.</param>
        /// <param name="pagesPerCacheBlock">The pages per cache block.</param>
        public VirtualBufferFactorySettings(
            int bufferSize,
            int reservationPageCount,
            int pagesPerCacheBlock)
        {
            // Buffer size must be multiple of system page size
            if ((bufferSize % VirtualBuffer.SystemPageSize) != 0)
            {
                throw new ArgumentException(
                    $"Buffer size must be multiple of {VirtualBuffer.SystemPageSize}.",
                    nameof(bufferSize));
            }

            BufferSize = bufferSize;
            ReservationPageCount = reservationPageCount;
            PagesPerCacheBlock = pagesPerCacheBlock;
        }

        /// <summary>
        /// Gets or sets the size of buffer returned by the buffer factory.
        /// </summary>
        /// <value>
        /// The size of the buffer in bytes.
        /// </value>
        /// <remarks>
        /// The buffer size should be a multiple of <see cref="VirtualBuffer.SystemPageSize"/> value.
        /// </remarks>
        public int BufferSize { get; }

        /// <summary>
        /// Gets or sets the reservation page count.
        /// </summary>
        /// <value>
        /// The reservation page count.
        /// </value>
        public int ReservationPageCount { get; }

        /// <summary>
        /// Gets or sets the pages per cache block.
        /// </summary>
        /// <value>
        /// The pages per cache block.
        /// </value>
        public int PagesPerCacheBlock { get; }
    }
}
