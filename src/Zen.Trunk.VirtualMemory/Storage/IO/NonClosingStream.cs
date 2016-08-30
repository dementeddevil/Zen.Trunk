using System;
using System.IO;

namespace Zen.Trunk.Storage.IO
{
	public class NonClosingStream : Stream
	{
		#region Private Fields
		private readonly Stream _innerStream;
		#endregion

		#region Public Constructors
		public NonClosingStream(Stream stream)
		{
			_innerStream = stream;
		}
		#endregion

		#region Public Properties
		public override bool CanRead
		{
			get
			{
				CheckDisposed();
				return _innerStream.CanRead;
			}
		}

		public override bool CanSeek
		{
			get
			{
				CheckDisposed();
				return _innerStream.CanSeek;
			}
		}

		public override bool CanWrite
		{
			get
			{
				CheckDisposed();
				return _innerStream.CanWrite;
			}
		}

		public override bool CanTimeout
		{
			get
			{
				CheckDisposed();
				return _innerStream.CanTimeout;
			}
		}

		public override void Flush()
		{
			CheckDisposed();
			_innerStream.Flush();
		}

		public override long Length
		{
			get
			{
				CheckDisposed();
				return _innerStream.Length;
			}
		}

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
