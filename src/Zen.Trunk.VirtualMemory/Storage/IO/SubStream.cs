namespace Zen.Trunk.Storage.IO
{
	using System;
	using System.IO;

	public class SubStream : Stream
	{
		#region Private Fields
		private readonly Stream _innerStream;
		private long _position;
		private readonly long _innerStartPosition;
		private readonly long _subStreamLength;
		private readonly bool _leaveUnderlyingStreamAtEOFOnClose;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initialises an instance of <see cref="T:SubStream" />.
		/// </summary>
		public SubStream (Stream innerStream, long length)
		{
			_innerStream = innerStream;
			_innerStartPosition = _innerStream.Position;
			_subStreamLength = length;
			_leaveUnderlyingStreamAtEOFOnClose = false;
		}

		/// <summary>
		/// Initialises an instance of <see cref="T:SubStream" />.
		/// </summary>
		public SubStream (Stream innerStream, long startOffset, long length)
		{
			_innerStream = innerStream;
			_innerStartPosition = startOffset;
			_subStreamLength = length;
			_leaveUnderlyingStreamAtEOFOnClose = false;
		}

		/// <summary>
		/// Initialises an instance of <see cref="T:SubStream" />.
		/// </summary>
		public SubStream (Stream innerStream, long startOffset, long length,
			bool leaveUnderlyingStreamAtEOFOnClose)
		{
			_innerStream = innerStream;
			_innerStartPosition = startOffset;
			_subStreamLength = length;
			_leaveUnderlyingStreamAtEOFOnClose = leaveUnderlyingStreamAtEOFOnClose;
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
					throw new ArgumentOutOfRangeException ("value");
				}
				if (!CanSeek)
				{
					__Error.SeekNotSupported ();
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
		public override void Close ()
		{
			if (_leaveUnderlyingStreamAtEOFOnClose && CanSeek)
			{
				// Advance stream to EOF
				_innerStream.Position = _innerStartPosition + _subStreamLength;
			}
			else
			{
				// Otherwise close
				_innerStream.Close ();
			}
			base.Close ();
		}

		public override void Flush ()
		{
			_innerStream.Flush ();
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			var position = Position;
			var spaceAvailable = _subStreamLength - position;
			if (spaceAvailable < count)
			{
				count = (int) spaceAvailable;
			}
			if (count == 0)
			{
				return 0;
			}
			Position = position;
			return _innerStream.Read (buffer, offset, count);
		}

		public override long Seek (long offset, SeekOrigin origin)
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

		public override void SetLength (long value)
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException ("value");
			}
			var endOffset = _innerStartPosition + value;
			if (endOffset > _innerStream.Length)
			{
				_innerStream.SetLength (endOffset);
			}
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			var position = Position;
			var spaceAvailable = _subStreamLength - position;
			if (spaceAvailable < count)
			{
				count = (int) spaceAvailable;
			}
			if (count > 0)
			{
				Position = position;
				_innerStream.Read (buffer, offset, count);
			}
		}
		#endregion

		#region Protected Methods
		#endregion

		#region Private Methods
		#endregion
	}
}
