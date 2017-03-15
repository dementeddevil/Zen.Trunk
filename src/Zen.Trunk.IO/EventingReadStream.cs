// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventingReadStream.cs" company="Zen Design Software">
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
    /// <c>EventingReadStream</c> is an abstract class that raises events as
    /// the stream is read.
    /// </summary>
    public abstract class EventingReadStream : Stream, IProvideReadStreamEvents
    {
        #region Private Fields
        private bool _readStarted;
        #endregion

        #region Public Events
        /// <summary>
        /// Fired before first read.
        /// </summary>
        public event EventHandler BeforeFirstReadEvent;

        /// <summary>
        /// Fired when a read event occurs.
        /// </summary>
        public event EventHandler<ReadEventArgs> ReadEvent;

        /// <summary>
        /// Fired after last read event.
        /// </summary>
        public event EventHandler AfterLastReadEvent;
        #endregion

        #region Protected Constructors
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// <c>true</c> if the stream supports writing; otherwise, <c>false</c>.
        /// </returns>
        public sealed override bool CanWrite => false;

        /// <summary>
        /// Gets or sets a value indicating whether reading from the stream has
        /// been completed.
        /// </summary>
        /// <value>
        /// <c>true</c> if stream reading has been completed; otherwise,
        /// <c>false</c>.
        /// </value>
        public bool ReadCompleted { get; private set; }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets or sets a value indicating whether to enable read events.
        /// </summary>
        /// <value>
        /// <c>true</c> if read events are enabled; otherwise, <c>false</c>.
        /// </value>
        protected bool EnableReadEvent { get; set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the
        /// position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">
        /// An array of bytes. When this method returns, the buffer contains
        /// the specified byte array with the values between <paramref name="offset" />
        /// and (<paramref name="offset" /> + <paramref name="count" /> - 1)
        /// replaced by the bytes read from the current source.
        /// </param>
        /// <param name="offset">
        /// The zero-based byte offset in <paramref name="buffer" /> at which to
        /// begin storing the data read from the current stream.
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to be read from the current stream.
        /// </param>
        /// <returns>
        /// The total number of bytes read into the buffer.
        /// This can be less than the number of bytes requested if that many
        /// bytes are not currently available, or zero (0) if the end of the
        /// stream has been reached.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">
        /// The sum of
        /// <paramref name="offset" /> and <paramref name="count" /> is larger than the
        /// buffer length.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="buffer" /> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="offset" /> or <paramref name="count" /> is negative.
        /// </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// The stream does not support
        /// reading.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// Methods were called after
        /// the stream was closed.
        /// </exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_readStarted)
            {
                _readStarted = true;
                FireBeforeFirstReadEvent();
            }

            var bytesRead = ReadInternal(buffer, offset, count);
            if (bytesRead > 0)
            {
                FireReadEvent(buffer, offset, bytesRead);
            }
            else if (!ReadCompleted)
            {
                ReadCompleted = true;
                FireAfterLastReadEvent();
            }

            return bytesRead;
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current
        /// position within this stream by the number of bytes written.
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
        /// <exception cref="System.NotImplementedException">
        /// This method is sealed and not implemented.
        /// </exception>
        public sealed override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the
        /// position within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        /// <exception cref="System.NotImplementedException">
        /// This method is sealed and not implemented.
        /// </exception>
        public sealed override void WriteByte(byte value)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Protected Methods
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
        protected abstract int ReadInternal(byte[] buffer, int offset, int count);

        /// <summary>
        /// Fires the before first read event.
        /// </summary>
        protected void FireBeforeFirstReadEvent()
        {
            var handler = BeforeFirstReadEvent;
            if (EnableReadEvent && handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Fires the read event.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="bytesRead">The bytes read.</param>
        protected void FireReadEvent(byte[] buffer, int offset, int bytesRead)
        {
            var handler = ReadEvent;
            if (EnableReadEvent && handler != null)
            {
                var eventArgs = new ReadEventArgs(Position, buffer, offset, bytesRead);
                handler(this, eventArgs);
            }
        }

        /// <summary>
        /// Fires the after last read event.
        /// </summary>
        protected void FireAfterLastReadEvent()
        {
            var handler = AfterLastReadEvent;
            if (EnableReadEvent && handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
        #endregion
    }

    /// <summary>
    /// <c>CEventingReadStream</c> is a concrete implementation of the
    /// <see cref="EventingReadStream" />.
    /// </summary>
    public class CEventingReadStream : EventingReadStream
    {
        #region Private Fields
        private Stream _data;
        private readonly bool _allowStat;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initialises a new instance of the <see cref="CEventingReadStream" /> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="allowStat">
        /// if set to <c>true</c> the Length property will delegate to the
        /// inner stream, otherwise; Length will return a hardcoded value.
        /// </param>
        public CEventingReadStream(Stream data, bool allowStat = true)
        {
            _data = data;
            _allowStat = allowStat;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead
        {
            get
            {
                CheckNotDisposed();
                return _data.CanRead;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek
        {
            get
            {
                CheckNotDisposed();
                return _data.CanSeek;
            }
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        public override long Length
        {
            get
            {
                CheckNotDisposed();
                if (_allowStat && _data.CanSeek)
                {
                    return _data.Length;
                }
                return 1048576L;
            }
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        /// <returns>The current position within the stream.</returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public override long Position
        {
            get
            {
                CheckNotDisposed();
                return _data.Position;
            }
            set
            {
                if (value <= _data.Position)
                {
                    _data.Position = value;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to
        /// be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            CheckNotDisposed();
            _data.Flush();
        }

        /// <summary>
        /// Sets the position within the current stream.
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
            CheckNotDisposed();
            return _data.Seek(offset, origin);
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            CheckNotDisposed();
            _data.SetLength(value);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream" /> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (_data != null)
            {
                _data.Dispose();
                _data = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Reads from the underlying stream.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        protected override int ReadInternal(byte[] buffer, int offset, int count)
        {
            CheckNotDisposed();
            return _data.Read(buffer, offset, count);
        }
        #endregion

        #region Private Methods
        private void CheckNotDisposed()
        {
            if (_data == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
        #endregion
    }
}