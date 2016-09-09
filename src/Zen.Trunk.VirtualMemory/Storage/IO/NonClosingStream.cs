using System;
using System.IO;

namespace Zen.Trunk.Storage.IO
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IO.Stream" />
    public class NonClosingStream : Stream
	{
		#region Private Fields
		private readonly Stream _innerStream;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="NonClosingStream"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public NonClosingStream(Stream stream)
		{
			_innerStream = stream;
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead
		{
			get
			{
				CheckDisposed();
				return _innerStream.CanRead;
			}
		}

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek
		{
			get
			{
				CheckDisposed();
				return _innerStream.CanSeek;
			}
		}

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite
		{
			get
			{
				CheckDisposed();
				return _innerStream.CanWrite;
			}
		}

        /// <summary>
        /// Gets a value that determines whether the current stream can time out.
        /// </summary>
        public override bool CanTimeout
		{
			get
			{
				CheckDisposed();
				return _innerStream.CanTimeout;
			}
		}

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
		{
			CheckDisposed();
			_innerStream.Flush();
		}

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        public override long Length
		{
			get
			{
				CheckDisposed();
				return _innerStream.Length;
			}
		}

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
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
		#endregion

		#region Public Methods
		/// <summary>
		/// Overridden. Performs the seek operation on the underlying stream.
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="origin"></param>
		/// <returns></returns>
		public override long Seek(long offset, SeekOrigin origin)
		{
			CheckDisposed();
			return _innerStream.Seek(offset, origin);
		}

		/// <summary>
		/// Overridden. Sets the length of this stream object.
		/// </summary>
		/// <remarks>
		/// Since device streams cannot be resized, this method will
		/// always throw an <see cref="System.InvalidOperationException"/>.
		/// </remarks>
		/// <param name="value"></param>
		public override void SetLength(long value)
		{
			throw new InvalidOperationException("Wrapped stream objects cannot be resized.");
		}

		/// <summary>
		/// Overridden. Reads from the underlying stream.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			CheckDisposed();
			return _innerStream.Read(buffer, offset, count);
		}

		/// <summary>
		/// Overridden. Writes to the underlying stream.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			CheckDisposed();
			_innerStream.Write(buffer, offset, count);
		}

		/// <summary>
		/// Overridden. Reads a byte from the underlying stream.
		/// </summary>
		/// <returns></returns>
		public override int ReadByte()
		{
			CheckDisposed();
			return _innerStream.ReadByte();
		}

		/// <summary>
		/// Overridden. Writes a byte to the underlying stream.
		/// </summary>
		/// <param name="value"></param>
		public override void WriteByte(byte value)
		{
			CheckDisposed();
			_innerStream.WriteByte(value);
		}
		#endregion

		#region Private Methods
		private void CheckDisposed()
		{
			if (_innerStream == null)
			{
				throw new ObjectDisposedException("DeviceBuffer.DeviceStream");
			}
		}
		#endregion
	}
}
