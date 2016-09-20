// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReadOnlySeekableStream.cs" company="Zen Design Software">
//   © Zen Design Software 2015
// </copyright>
// <summary>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace Zen.Streaming
{
    /// <summary>
    /// </summary>
    public class ReadOnlySeekableStream : Stream
    {
        private bool _isDisposed;
        private Stream _persist;
        private bool _readEof;
        private Stream _source;

        /// <summary>
        /// Initialises a new instance of the <see cref="ReadOnlySeekableStream" />
        /// class.
        /// </summary>
        /// <param name="source">The source.</param>
        public ReadOnlySeekableStream(Stream source)
            : this(source, 4096)
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="ReadOnlySeekableStream" />
        /// class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="buffer">The buffer.</param>
        public ReadOnlySeekableStream(Stream source, Stream buffer)
            : this(source, buffer, 4096)
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="ReadOnlySeekableStream" />
        /// class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        public ReadOnlySeekableStream(Stream source, int bufferSize)
        {
            _source = source;
            _persist =
                new BufferedStream(
                    new FileStream(
                        Path.GetTempFileName(),
                        FileMode.Open,
                        FileAccess.ReadWrite,
                        FileShare.None,
                        bufferSize,
                        FileOptions.DeleteOnClose),
                    bufferSize);
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="ReadOnlySeekableStream" />
        /// class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <exception cref="System.ArgumentNullException">
        /// source
        /// or
        /// buffer
        /// </exception>
        public ReadOnlySeekableStream(Stream source, Stream buffer, int bufferSize)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            buffer.SetLength(0);

            _source = source;
            _persist = new BufferedStream(buffer, bufferSize);
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the
        /// current stream supports reading.
        /// </summary>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead => true;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the
        /// current stream supports seeking.
        /// </summary>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek => true;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the
        /// current stream supports writing.
        /// </summary>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite => false;

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        public override long Length
        {
            get
            {
                CheckNotDisposed();
                if (!_readEof)
                {
                    var position = _persist.Position;
                    _persist.Seek(0, SeekOrigin.End);
                    Consume(9223372036854775807L);
                    _persist.Seek(position, SeekOrigin.Begin);
                }
                return _persist.Length;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the
        /// current stream.
        /// </summary>
        /// <returns>The current position within the stream.</returns>
        public override long Position
        {
            get
            {
                CheckNotDisposed();
                return _persist.Position;
            }
            set
            {
                CheckNotDisposed();
                Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and
        /// causes any buffered data to be written to the underlying device.
        /// </summary>
        /// <exception cref="System.NotSupportedException"></exception>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the
        /// current stream and advances the position within the stream by the number of
        /// bytes read.
        /// </summary>
        /// <param name="buffer">
        /// An array of bytes. When this method returns, the buffer
        /// contains the specified byte array with the values between
        /// <paramref name="offset" /> and (<paramref name="offset" /> +
        /// <paramref name="count" /> - 1) replaced by the bytes read from the current
        /// source.
        /// </param>
        /// <param name="offset">
        /// The zero-based byte offset in <paramref name="buffer" />
        /// at which to begin storing the data read from the current stream.
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to be read from the current
        /// stream.
        /// </param>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the
        /// number of bytes requested if that many bytes are not currently available,
        /// or zero (0) if the end of the stream has been reached.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckNotDisposed();

            var bytesRead = 0;

            // Determine how much can be satisfied from our cache
            var readableCount = (int)Math.Min(_persist.Length - Position, count);
            if (readableCount > 0)
            {
                // Read as much as we can from the cache
                bytesRead = _persist.Read(buffer, offset, readableCount);

                // Adjust offset and remaining byte count
                offset += bytesRead;
                count -= bytesRead;
            }

            // If we have not reached EOF and still have bytes to read...
            if (!_readEof && count > 0)
            {
                // Read from the source and cache to our buffer
                var newBytesRead = _source.Read(buffer, offset, count);
                if (newBytesRead == 0)
                {
                    // EOF encountered
                    _readEof = true;
                }
                else
                {
                    // Write the data we read to our buffer
                    _persist.Write(buffer, offset, newBytesRead);
                }

                // Update number of bytes read in total
                bytesRead += newBytesRead;
            }
            return bytesRead;
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current
        /// stream.
        /// </summary>
        /// <param name="offset">
        /// A byte offset relative to the <paramref name="origin" />
        /// parameter.
        /// </param>
        /// <param name="origin">
        /// A value of type <see cref="T:System.IO.SeekOrigin" />
        /// indicating the reference point used to obtain the new position.
        /// </param>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// offset;Seek offset out of
        /// range.
        /// </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckNotDisposed();

            switch (origin)
            {
                case SeekOrigin.Current:
                    if (offset > 0L || Math.Abs(offset) <= Position)
                    {
                        offset += Position;
                        origin = SeekOrigin.Begin;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(offset), "Seek offset out of range.");
                    }
                    break;
                case SeekOrigin.End:
                    if (offset > 0 || (Math.Abs(offset) <= Length && offset <= 0L))
                    {
                        offset += Length;
                        origin = SeekOrigin.Begin;
                    }
                    break;
            }

            // Determine how much we can seek now
            var maxOffset = Math.Min(offset, _persist.Length);
            if (0L != _persist.Length)
            {
                // Seek as much as we can and adjust
                _persist.Seek(maxOffset, SeekOrigin.Begin);
                offset -= maxOffset;
            }

            // If we haven't found EOF yet and still have more to go then
            //	we need to consume to get there...
            if (0L != offset && !_readEof)
            {
                Consume(offset);
            }

            // Return new position
            return Position;
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the
        /// current stream and advances the current position within this stream by the
        /// number of bytes written.
        /// </summary>
        /// <param name="buffer">
        /// An array of bytes. This method copies
        /// <paramref name="count" /> bytes from <paramref name="buffer" /> to the
        /// current stream.
        /// </param>
        /// <param name="offset">
        /// The zero-based byte offset in <paramref name="buffer" />
        /// at which to begin copying bytes to the current stream.
        /// </param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the
        /// <see cref="T:System.IO.Stream" /> and optionally releases the managed
        /// resources.
        /// </summary>
        /// <param name="disposing">
        /// true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _isDisposed = true;
                if (_source != null)
                {
                    _source.Dispose();
                    _source = null;
                }
                if (_persist != null)
                {
                    _persist.Dispose();
                    _persist = null;
                }
            }
            base.Dispose(disposing);
        }

        private void CheckNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private void Consume(long count)
        {
            CheckNotDisposed();
            if (!_readEof)
            {
                var buffer = new byte[4096];
                int bytesRead;
                for (bytesRead = _source.Read(buffer, 0, (int)Math.Min(buffer.Length, count));
                     bytesRead != 0 && count != 0L;
                     bytesRead = _source.Read(buffer, 0, (int)Math.Min(buffer.Length, count)))
                {
                    _persist.Write(buffer, 0, bytesRead);
                    count -= bytesRead;
                }
                _readEof = (0 == bytesRead);
            }
        }
    }
}