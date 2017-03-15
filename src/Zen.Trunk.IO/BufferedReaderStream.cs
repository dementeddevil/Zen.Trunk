// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BufferedReaderStream.cs" company="Zen Design Software">
//   © Zen Design Software 2015
// </copyright>
// <summary>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace Zen.Trunk.IO
{
    /// <summary>
    /// <c>BufferedReaderStream</c> extends
    /// <see cref="ForwardOnlyEventingReadStream" />
    /// to provide support for stream processing.
    /// </summary>
    public abstract class BufferedReaderStream : ForwardOnlyEventingReadStream
    {
        #region Private Fields
        private readonly MemoryStream _outputStream;
        private int _bufferCount;
        private int _bufferPos;
        private long _position;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initialises a new instance of the <see cref="BufferedReaderStream" />
        /// class.
        /// </summary>
        protected BufferedReaderStream()
            : this(new MemoryStream())
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="BufferedReaderStream" />
        /// class.
        /// </summary>
        /// <param name="outputStream">The output stream.</param>
        protected BufferedReaderStream(MemoryStream outputStream)
        {
            if (outputStream != null)
            {
                _outputStream = outputStream;
            }
            else
            {
                _outputStream = new MemoryStream();
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        /// <remarks>
        /// This method returns a hard-coded fixed value.
        /// </remarks>
        public override long Length => 1048576;

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        /// <returns>The current position within the stream.</returns>
        public override long Position => _position;

        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the output stream.
        /// </summary>
        /// <value>
        /// The output stream.
        /// </value>
        protected Stream OutputStream => _outputStream;

        #endregion

        #region Protected Methods
        /// <summary>
        /// Processes the data block.
        /// </summary>
        /// <param name="count">
        /// The number of bytes to write to the output stream.
        /// </param>
        /// <returns>
        /// The number of bytes written to the output stream.
        /// </returns>
        protected abstract int ProcessDataBlock(int count);

        /// <summary>
        /// Reads from the underlying stream.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method must be implemented by a derived class.
        /// </remarks>
        protected override int ReadInternal(byte[] buffer, int offset, int count)
        {
            int bytesToRead;
            var totalBytesRead = 0;
            if (_bufferCount > 0)
            {
                var outputBuffer = _outputStream.GetBuffer();

                if (_bufferCount < count)
                {
                    bytesToRead = _bufferCount;
                }
                else
                {
                    bytesToRead = count;
                }

                Array.Copy(outputBuffer, _bufferPos, buffer, offset, bytesToRead);
                _bufferCount = _bufferCount - bytesToRead;
                _bufferPos = _bufferPos + bytesToRead;
                _position = _position + bytesToRead;
                if (count != bytesToRead)
                {
                    offset += bytesToRead;
                    count -= bytesToRead;
                    totalBytesRead += bytesToRead;
                }
                else
                {
                    return bytesToRead;
                }
            }

            _outputStream.Seek(0, SeekOrigin.Begin);
            _outputStream.SetLength(0);

            var bytesRead = ProcessDataBlock(count);
            if (bytesRead < count)
            {
                count = bytesRead;
            }

            if (count > 0)
            {
                var outputBuffer = _outputStream.GetBuffer();
                Array.Copy(outputBuffer, 0, buffer, offset, count);
                if (_outputStream.Length > count)
                {
                    _bufferPos = count;
                    _bufferCount = (int) _outputStream.Length - count;
                }

                _position += count;
                totalBytesRead += count;
            }
            return totalBytesRead;
        }
        #endregion
    }
}