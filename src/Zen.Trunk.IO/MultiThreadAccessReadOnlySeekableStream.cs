// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MultiThreadAccessReadOnlySeekableStream.cs" company="Zen Design Software">
//   © Zen Design Software 2015
// </copyright>
// <summary>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Threading;

namespace Zen.Trunk.IO
{
    /// <summary>
    /// <c>MultiThreadAccessReadOnlySeekableStream</c> is a class designed to
    /// allow multiple threads to read from a single stream.
    /// </summary>
    public class MultiThreadAccessReadOnlySeekableStream : Stream
    {
        #region Private Types
        private class ThreadStream : Stream
        {
            private readonly MultiThreadAccessReadOnlySeekableStream _owner;
            private long _position;

            public ThreadStream(MultiThreadAccessReadOnlySeekableStream owner, long initialPosition)
            {
                _owner = owner;
                _position = initialPosition;
            }

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => false;

            public override long Length => _owner.Length;

            public override long Position
            {
                get
                {
                    return _position;
                }
                set
                {
                    // TODO: Test for passing EOF?
                    _position = value;
                }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                lock (_owner._syncSourceStream)
                {
                    _owner._sourceStream.Position = _position;
                    try
                    {
                        return _owner._sourceStream.Read(buffer, offset, count);
                    }
                    finally
                    {
                        _position = _owner._sourceStream.Position;
                    }
                }
            }

            /// <summary>
            /// Seeks the specified offset.
            /// </summary>
            /// <param name="offset">The offset.</param>
            /// <param name="origin">The origin.</param>
            /// <returns></returns>
            public override long Seek(long offset, SeekOrigin origin)
            {
                long newPosition;
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        newPosition = offset;
                        break;
                    case SeekOrigin.Current:
                        newPosition = _position + offset;
                        break;
                    case SeekOrigin.End:
                        newPosition = Length + offset;
                        break;
                    default:
                        throw new ArgumentException("Invalid seek origin encountered.");
                }
                if (newPosition < 0)
                {
                    newPosition = 0;
                }
                else if (newPosition > Length)
                {
                    newPosition = Length;
                }
                _position = newPosition;
                return _position;
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initialises a new instance of the
        /// <see cref="MultiThreadAccessReadOnlySeekableStream" /> class.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <exception cref="System.ArgumentException">Source stream is not readable.</exception>
        public MultiThreadAccessReadOnlySeekableStream(Stream sourceStream)
        {
            if (!sourceStream.CanRead)
            {
                throw new ArgumentException("Source stream is not readable.");
            }

            if (sourceStream.CanSeek)
            {
                _sourceStream = sourceStream;
            }
            else
            {
                _sourceStream = sourceStream.AsReadOnlySeekableStream();
            }

            _originalPosition = _sourceStream.Position;
        }
        #endregion

        #region Private Fields
        private Stream _sourceStream;
        private readonly long _originalPosition;

        private bool _hasLength;
        private long _length;
        private readonly object _syncSourceStream = new object();
        private int _referenceCount = 1;
        private bool _closedMasterReference;
        private bool _disposed;
        #endregion

        #region Public Properties
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
                CheckDisposed();
                if (!_hasLength)
                {
                    lock (_syncSourceStream)
                    {
                        if (!_hasLength)
                        {
                            _length = _sourceStream.Length;
                            _hasLength = true;
                        }
                    }
                }
                return _length;
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
                CheckDisposed();
                return GetStreamForCurrentThread().Position;
            }
            set
            {
                CheckDisposed();
                GetStreamForCurrentThread().Position = value;
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and
        /// causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            CheckDisposed();
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
            CheckDisposed();
            return GetStreamForCurrentThread().Read(buffer, offset, count);
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
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();
            return GetStreamForCurrentThread().Seek(offset, origin);
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
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
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Protected Methods
        protected override void Dispose(bool disposing)
        {
            var decrementReferenceCount = false;
            var keyName = GetType().Name + ":ThreadThread";
            if (CallContext.LogicalGetData(keyName) != null)
            {
                CallContext.LogicalSetData(keyName, null);
                decrementReferenceCount = true;
            }
            else if (!_closedMasterReference)
            {
                _closedMasterReference = true;
                decrementReferenceCount = true;
            }

            // If we are closing the last reference then dispose
            if (decrementReferenceCount && Interlocked.Decrement(ref _referenceCount) == 0)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    if (_sourceStream != null)
                    {
                        _sourceStream.Dispose();
                        _sourceStream = null;
                    }
                }

                base.Dispose(disposing);
            }
        }
        #endregion

        #region Private Methods
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private Stream GetStreamForCurrentThread()
        {
            var keyName = GetType().Name + ":ThreadThread";
            var result = (Stream)CallContext.LogicalGetData(keyName);
            if (result == null)
            {
                result = new ThreadStream(this, _originalPosition);
                CallContext.LogicalSetData(keyName, result);
                Interlocked.Increment(ref _referenceCount);
            }
            return result;
        }
        #endregion
    }
}