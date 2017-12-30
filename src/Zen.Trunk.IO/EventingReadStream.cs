using System;
using System.IO;

namespace Zen.Trunk.IO
{
    /// <summary>
    /// <c>EventingReadStream</c> is a concrete implementation of the
    /// <see cref="BaseEventingReadStream" />.
    /// </summary>
    public class EventingReadStream : BaseEventingReadStream
    {
        #region Private Fields
        private Stream _data;
        private readonly bool _allowStat;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initialises a new instance of the <see cref="EventingReadStream" /> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="allowStat">
        /// if set to <c>true</c> the Length property will delegate to the
        /// inner stream, otherwise; Length will return a hardcoded value.
        /// </param>
        public EventingReadStream(Stream data, bool allowStat = true)
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