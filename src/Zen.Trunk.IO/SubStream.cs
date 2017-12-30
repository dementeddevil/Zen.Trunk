using System;
using System.IO;

namespace Zen.Trunk.IO
{
    /// <summary>
    /// <c>SubStream</c> is a stream that represents a sub-section of an
    /// owner stream.
    /// </summary>
    /// <seealso cref="System.IO.Stream" />
    public class SubStream : Stream
    {
        #region Private Fields
        private readonly Stream _innerStream;
        private long _position;
        private readonly long _innerStartPosition;
        private readonly long _subStreamLength;
        private readonly bool _leaveUnderlyingStreamAtEofOnClose;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initialises an instance of <see cref="T:SubStream" />.
        /// </summary>
        /// <paramref name="innerStream">Inner stream</paramref>
        /// <paramref name="length">Length of the sub-stream</paramref>
        /// <remarks>
        /// The sub-stream will be positioned at the current underlying stream position
        /// and will have a length as specified.1
        /// </remarks>
        public SubStream(Stream innerStream, long length)
        {
            _innerStream = innerStream;
            _innerStartPosition = _innerStream.Position;
            _subStreamLength = length;
            _leaveUnderlyingStreamAtEofOnClose = false;
        }

        /// <summary>
        /// Initialises an instance of <see cref="T:SubStream" />.
        /// </summary>
        /// <paramref name="innerStream">Inner stream</paramref>
        /// <paramref name="startOffset">Position in underlying stream to start sub-stream</paramref>
        /// <paramref name="length">Length of the sub-stream</paramref>
        /// <paramref name="leaveUnderlyingStreamAtEofOnClose">
        /// <c>true</c> then underlying stream is left at the logical position
        /// matching the end of the sub-stream when the sub-stream is closed.
        /// <c>false</c> then underlying stream is closed when the sub-stream is
        /// closed.
        /// </paramref>
        /// <remarks>
        /// The sub-stream will be positioned at the current underlying stream position
        /// and will have a length as specified.1
        /// </remarks>
        public SubStream(Stream innerStream, long startOffset, long length, bool leaveUnderlyingStreamAtEofOnClose = false)
        {
            _innerStream = innerStream;
            _innerStartPosition = startOffset;
            _subStreamLength = length;
            _leaveUnderlyingStreamAtEofOnClose = leaveUnderlyingStreamAtEofOnClose;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <value></value>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead => _innerStream.CanRead;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <value></value>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek => _innerStream.CanSeek;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <value></value>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite => _innerStream.CanWrite;

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <value></value>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        /// <exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Length => _subStreamLength;

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <value></value>
        /// <returns>The current position within the stream.</returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Position
        {
            get
            {
                var innerPosition = _innerStream.Position;
                if (innerPosition >= _innerStartPosition &&
                    innerPosition <= (_innerStartPosition + _subStreamLength))
                {
                    _position = innerPosition - _innerStartPosition;
                }
                return _position;
            }
            set
            {
                if (value < 0 || _position >= _subStreamLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                if (!CanSeek)
                {
                    throw new NotSupportedException("Seeking not supported.");
                }
                if (Position != value)
                {
                    _innerStream.Position = _innerStartPosition + value;
                }
            }
        }

        /// <summary>
        /// Gets a value that determines whether the current stream can time out.
        /// </summary>
        /// <value></value>
        /// <returns>A value that determines whether the current stream can time out.</returns>
        public override bool CanTimeout => _innerStream.CanTimeout;
        #endregion

        #region Public Methods
        /// <summary>
        /// Closes the current stream and releases any resources (such as 
        /// sockets and file handles) associated with the current stream.
        /// </summary>
        public override void Close()
        {
            if (_leaveUnderlyingStreamAtEofOnClose && CanSeek)
            {
                // Advance stream to EOF
                _innerStream.Position = _innerStartPosition + _subStreamLength;
            }
            else
            {
                // Otherwise close
                _innerStream.Close();
            }
            base.Close();
        }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            _innerStream.Flush();
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var position = Position;
            var spaceAvailable = _subStreamLength - position;
            if (spaceAvailable < count)
            {
                count = (int)spaceAvailable;
            }
            if (count == 0)
            {
                return 0;
            }
            Position = position;
            return _innerStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to obtain the new position.</param>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = 0;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = _innerStartPosition + offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = Position + offset;
                    break;
                case SeekOrigin.End:
                    newPosition = _innerStartPosition + _subStreamLength - offset;
                    break;
            }
            Position = newPosition;
            return Position;
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public override void SetLength(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            var endOffset = _innerStartPosition + value;
            if (endOffset > _innerStream.Length)
            {
                _innerStream.SetLength(endOffset);
            }
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count" /> bytes from <paramref name="buffer" /> to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            var position = Position;
            var spaceAvailable = _subStreamLength - position;
            if (spaceAvailable < count)
            {
                count = (int)spaceAvailable;
            }
            if (count > 0)
            {
                Position = position;
                _innerStream.Read(buffer, offset, count);
            }
        }
        #endregion
    }
}
