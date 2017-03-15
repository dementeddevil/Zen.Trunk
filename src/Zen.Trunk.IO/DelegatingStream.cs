// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DelegatingStream.cs" company="Zen Design Software">
//   © Zen Design Software 2015
// </copyright>
// <summary>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Zen.Trunk.IO
{
    /// <summary>
    /// <c>DelegatingStream</c> is a wrapper stream object.
    /// </summary>
    public class DelegatingStream : Stream
    {
        private Stream _innerStream;
        private SynchronizationContext _syncContext;

        /// <summary>
        /// Initialises a new instance of the <see cref="DelegatingStream" /> class.
        /// </summary>
        /// <param name="innerStream">The inner stream.</param>
        /// <param name="useSyncContext">
        /// if set to <c>true</c> then the <see cref="E:Disposed" /> event will
        /// be raised on the same thread that called this constructor.
        /// </param>
        public DelegatingStream(Stream innerStream, bool useSyncContext = false)
        {
            _innerStream = innerStream;
            if (useSyncContext)
            {
                _syncContext = new SynchronizationContext();
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the
        /// current stream supports reading.
        /// </summary>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead => _innerStream != null && _innerStream.CanRead;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the
        /// current stream supports seeking.
        /// </summary>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek => _innerStream != null && _innerStream.CanSeek;

        /// <summary>
        /// Gets a value that determines whether the current stream can time out.
        /// </summary>
        /// <returns>A value that determines whether the current stream can time out.</returns>
        public override bool CanTimeout => _innerStream != null && _innerStream.CanTimeout;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the
        /// current stream supports writing.
        /// </summary>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite => _innerStream != null && _innerStream.CanWrite;

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        public override long Length
        {
            get
            {
                CheckDisposed();
                return _innerStream.Length;
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
                return _innerStream.Position;
            }
            set
            {
                CheckDisposed();
                _innerStream.Position = value;
            }
        }

        /// <summary>
        /// Gets or sets a value, in miliseconds, that determines how long the stream
        /// will attempt to read before timing out.
        /// </summary>
        /// <returns>
        /// A value, in miliseconds, that determines how long the stream will
        /// attempt to read before timing out.
        /// </returns>
        public override int ReadTimeout
        {
            get
            {
                CheckDisposed();
                return _innerStream.ReadTimeout;
            }
            set
            {
                CheckDisposed();
                _innerStream.ReadTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets a value, in miliseconds, that determines how long the stream
        /// will attempt to write before timing out.
        /// </summary>
        /// <returns>
        /// A value, in miliseconds, that determines how long the stream will
        /// attempt to write before timing out.
        /// </returns>
        public override int WriteTimeout
        {
            get
            {
                CheckDisposed();
                return _innerStream.WriteTimeout;
            }
            set
            {
                CheckDisposed();
                _innerStream.WriteTimeout = value;
            }
        }

        /// <summary>
        /// Occurs when the stream is disposed.
        /// </summary>
        public event EventHandler Disposed;

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and
        /// causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            CheckDisposed();
            _innerStream.Flush();
        }

        /// <summary>
        /// Asynchronously clears all buffers for this stream, causes any buffered data
        /// to be written to the underlying device, and monitors cancellation requests.
        /// </summary>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests.
        /// The default value is
        /// <see cref="P:System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous flush operation.
        /// </returns>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _innerStream.FlushAsync(cancellationToken);
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
            return _innerStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by
        /// one byte, or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>
        /// The unsigned byte cast to an Int32, or -1 if at the end of the stream.
        /// </returns>
        public override int ReadByte()
        {
            CheckDisposed();
            return _innerStream.ReadByte();
        }

        /// <summary>
        /// Asynchronously reads a sequence of bytes from the current stream, advances
        /// the position within the stream by the number of bytes read, and monitors
        /// cancellation requests.
        /// </summary>
        /// <param name="buffer">The buffer to write the data into.</param>
        /// <param name="offset">
        /// The byte offset in <paramref name="buffer" /> at which to
        /// begin writing data from the stream.
        /// </param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests.
        /// The default value is
        /// <see cref="P:System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation.
        /// The value of the Task contains the total number of bytes read into the
        /// buffer.
        /// The result value can be less than the number of bytes requested if the
        /// number
        /// of bytes currently available is less than the requested number,
        /// or it can be 0 (zero) if the end of the stream has been reached.
        /// </returns>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
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
            return _innerStream.Seek(offset, origin);
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            CheckDisposed();
            _innerStream.SetLength(value);
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
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            _innerStream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the
        /// position within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        public override void WriteByte(byte value)
        {
            CheckDisposed();
            _innerStream.WriteByte(value);
        }

        /// <summary>
        /// Asynchronously writes a sequence of bytes to the current stream, advances
        /// the current position within this stream by the number of bytes written, and
        /// monitors cancellation requests.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">
        /// The zero-based byte offset in <paramref name="buffer" />
        /// from which to begin copying bytes to the stream.
        /// </param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests.
        /// The default value is
        /// <see cref="P:System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous write operation.
        /// </returns>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to
        /// another stream, using a specified buffer size and cancellation token.
        /// </summary>
        /// <param name="destination">
        /// The stream to which the contents of the current
        /// stream will be copied.
        /// </param>
        /// <param name="bufferSize">
        /// The size, in bytes, of the buffer. This value must be
        /// greater than zero. The default size is 4096.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests.
        /// The default value is
        /// <see cref="P:System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous copy operation.
        /// </returns>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
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
            if (disposing)
            {
                // Discard inner stream if still valid
                if (_innerStream != null)
                {
                    try
                    {
                        _innerStream.Dispose();
                    }
                        // ReSharper disable once EmptyGeneralCatchClause
                    catch
                    {
                    }
                    finally
                    {
                        _innerStream = null;
                    }
                }

                // Raise event
                if (_syncContext != null)
                {
                    _syncContext.Send(state => OnDisposed(EventArgs.Empty), null);
                    _syncContext = null;
                }
                else
                {
                    OnDisposed(EventArgs.Empty);
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Raises the <see cref="E:Disposed" /> event.
        /// </summary>
        /// <param name="e">
        /// The <see cref="EventArgs" /> instance containing the event
        /// data.
        /// </param>
        protected virtual void OnDisposed(EventArgs e)
        {
            Disposed?.Invoke(this, e);
        }

        /// <summary>
        /// Invalidates the inner stream.
        /// </summary>
        protected void InvalidateInnerStream()
        {
            _innerStream = null;
        }

        private void CheckDisposed()
        {
            if (_innerStream == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}