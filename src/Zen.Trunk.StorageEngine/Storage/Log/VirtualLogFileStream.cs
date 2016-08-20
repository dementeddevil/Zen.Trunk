namespace Zen.Trunk.Storage.Log
{
	using System;
	using System.IO;
	using Zen.Trunk.Storage.IO;

	internal class VirtualLogFileStream : Stream
	{
		#region Private Fields
		private const int HeaderSize = 24;
		private const int TotalHeaderSize = HeaderSize * 2;

		private LogPageDevice _device;

		private VirtualLogFileInfo _logFileInfo;
		private bool _writeFirstHeader;
		private bool _headerDirty;
		private long _position;

		private BufferReaderWriter _streamManager;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Creates a new <see cref="T:VirtualLogFileStream"/> object owned by
		/// the specified <see cref="T:LogPageDevice"/> and mapped against the
		/// given <see cref="T:Stream"/>. The log file has the characteristics
		/// specified in the <see cref="T:VirtualLogFileInfo"/>.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="backingStore"></param>
		/// <param name="info"></param>
		public VirtualLogFileStream (
			LogPageDevice device, Stream backingStore, VirtualLogFileInfo info)
		{
			_device = device;
			_logFileInfo = info;
			_writeFirstHeader = true;
			_streamManager = new BufferReaderWriter (backingStore);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the log file Id.
		/// </summary>
		public uint FileId
		{
			get
			{
				return _logFileInfo.FileId;
			}
		}

		/// <summary>
		/// Gets/sets the previous log file Id.
		/// </summary>
		public uint PrevFileId
		{
			get
			{
				return _logFileInfo.CurrentHeader.PrevFileId;
			}
			set
			{
				if (_logFileInfo.CurrentHeader.PrevFileId != value)
				{
					_logFileInfo.CurrentHeader.PrevFileId = value;
					_headerDirty = true;
				}
			}
		}

		/// <summary>
		/// Gets/sets the next log file Id.
		/// </summary>
		public uint NextFileId
		{
			get
			{
				return _logFileInfo.CurrentHeader.NextFileId;
			}
			set
			{
				if (_logFileInfo.CurrentHeader.NextFileId != value)
				{
					_logFileInfo.CurrentHeader.NextFileId = value;
					_headerDirty = true;
				}
			}
		}

		/// <summary>
		/// Gets/sets a boolean value indicating whether this stream is full.
		/// </summary>
		public bool IsFull
		{
			get
			{
				return _logFileInfo.IsFull;
			}
			set
			{
				_logFileInfo.IsFull = value;
			}
		}

		/// <summary>
		/// Gets a boolean value indicating whether the owner 
		/// <see cref="T:LogPageDevice"/> is in recovery mode.
		/// </summary>
		/// <remarks>
		/// If the device is in recovery the stream cannot be written to
		/// only read from. Seeking is also allowed when in this mode.
		/// </remarks>
		public bool Recovery
		{
			get
			{
				return _device.IsInRecovery;
			}
		}

		/// <summary>
		/// Gets a boolean value indicating whether this stream is read-only.
		/// </summary>
		public bool ReadOnly
		{
			get
			{
				return _device.IsReadOnly | Recovery;
			}
		}

		/// <summary>
		/// Overridden. Gets a boolean value indicating whether the stream
		/// supports reading.
		/// </summary>
		public override bool CanRead
		{
			get
			{
				return Recovery;
			}
		}

		/// <summary>
		/// Overridden. Gets a boolean value indicating whether the stream
		/// supports writing.
		/// </summary>
		public override bool CanWrite
		{
			get
			{
				if (Recovery)
					return false;
				return !_device.IsReadOnly;
			}
		}

		/// <summary>
		/// Overridden. Gets a boolean value indicating whether the stream
		/// supports seeking.
		/// </summary>
		public override bool CanSeek
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Overridden. Gets a value indicating the stream length.
		/// This is equal to the number of pages multiplied by
		/// the log page data size.
		/// </summary>
		public override long Length
		{
			get
			{
				return _logFileInfo.Length - TotalHeaderSize;
			}
		}

		/// <summary>
		/// Gets/sets a value which indicates the current stream position.
		/// </summary>
		public override long Position
		{
			get
			{
				return _position;
			}
			set
			{
				Seek (value, SeekOrigin.Begin);
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Initialises a new file stream.
		/// </summary>
		/// <remarks>
		/// Marks this file stream as allocated and writes both headers to
		/// the underlying device before positioning the stream ready for
		/// writing.
		/// </remarks>
		public void InitNew ()
		{
			// Write header twice and flush
			_logFileInfo.IsAllocated = true;
			WriteHeader ();
			WriteHeader ();
			Flush ();
			Position = 0;
		}

		/// <summary>
		/// Initiates loading from the stream.
		/// </summary>
		/// <remarks>
		/// This method will read both headers from the start of the file and
		/// determine which of the headers is the most current before 
		/// positioning the stream at a point for reading log records.
		/// </remarks>
		public void InitLoad ()
		{
			// Read headers and determine correct version
			ReadHeaders ();
			Position = 0;
		}

		/// <summary>
		/// Writes the specified <see cref="T:LogEntry"/> object to the
		/// log file stream.
		/// </summary>
		/// <param name="entry"><see cref="T:LogEntry"/> log entry to be 
		/// written.</param>
		public void WriteEntry (LogEntry entry)
		{
			lock (_streamManager)
			{
				// Record cursor position of last log record written to the
				//	stream and move stream position to write point.
				entry.LastLog = _logFileInfo.CurrentHeader.LastCursor;
				Seek (_logFileInfo.CurrentHeader.Cursor, SeekOrigin.Begin);

				// Serialise the log entry
				EnsurePositionValid ();
				try
				{
					entry.Write (_streamManager);
				}
				finally
				{
					UpdatePosition ();
				}

				// If write was successfull then we can update the header.
				_logFileInfo.CurrentHeader.LastCursor =
					_logFileInfo.CurrentHeader.Cursor;
				_logFileInfo.CurrentHeader.Cursor = (uint) Position;
				WriteHeader ();
			}
		}

		/// <summary>
		/// Reads the next log entry from the log file stream.
		/// </summary>
		/// <returns><see cref="T:LogEntry"/> object.</returns>
		public LogEntry ReadEntry ()
		{
			LogEntry entry = null;
			EnsurePositionValid ();
			try
			{
				entry = LogEntry.ReadEntry (_streamManager);
			}
			finally
			{
				UpdatePosition ();
			}
			return entry;
		}

		/// <summary>
		/// Overridden. Changes the current position of the stream by
		/// determining the new page and offset.
		/// </summary>
		/// <remarks>
		/// Throws a <see cref="System.InvalidOperationException"/> if
		/// seeking is not allowed.
		/// </remarks>
		/// <param name="offset"></param>
		/// <param name="origin"></param>
		/// <returns></returns>
		public override long Seek (long offset, SeekOrigin origin)
		{
			if (!CanSeek)
			{
				throw new InvalidOperationException ("Current stream mode does not support seeking.");
			}

			// Determine new offset
			long newOffset = _logFileInfo.StartOffset + TotalHeaderSize;
			switch (origin)
			{
				case SeekOrigin.Begin:
					newOffset += offset;
					break;
				case SeekOrigin.Current:
					newOffset = newOffset + Position + offset;
					break;
				case SeekOrigin.End:
					newOffset = newOffset + Length + offset;
					break;
			}

			// Ensure we don't go before start point
			if (newOffset < (_logFileInfo.StartOffset + TotalHeaderSize))
			{
				newOffset = _logFileInfo.StartOffset + TotalHeaderSize;
			}
			if (newOffset > (_logFileInfo.StartOffset + Length))
			{
				newOffset = _logFileInfo.StartOffset + Length;
			}
			_streamManager.BaseStream.Seek (newOffset, SeekOrigin.Begin);
			UpdatePosition ();
			return Position;
		}

		/// <summary>
		/// Overridden. Sets the stream length.
		/// </summary>
		/// <remarks>
		/// This method always throws <see cref="System.NotSupportedException"/> as
		/// changing the stream length is not allowed.
		/// </remarks>
		/// <param name="value"></param>
		public override void SetLength (long value)
		{
			throw new NotSupportedException ("Setting page stream length is not supported.");
		}

		/// <summary>
		/// Overridden. Flushes all pending changes to the stream.
		/// </summary>
		public override void Flush ()
		{
			if (_streamManager != null && CanWrite)
			{
				if (_headerDirty)
				{
					WriteHeader ();
				}
				else
				{
					_streamManager.Flush ();
				}
			}
		}

		/// <summary>
		/// Overridden. Reads block of bytes from the stream.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		/// <exception cref="T:InvalidOperationException">
		/// Stream does not support reading.
		/// </exception>
		public override int Read (byte[] buffer, int offset, int count)
		{
			if (!CanRead)
			{
				throw new InvalidOperationException ("Stream does not support reading at this time.");
			}

			int retVal = 0;
			EnsurePositionValid ();
			try
			{
				retVal = _streamManager.BaseStream.Read (buffer, offset, count);
			}
			finally
			{
				UpdatePosition ();
			}
			return retVal;
		}

		/// <summary>
		/// Overridden. Reads a single byte from the stream.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="T:InvalidOperationException">
		/// Stream does not support reading.
		/// </exception>
		public override int ReadByte ()
		{
			if (!CanRead)
			{
				throw new InvalidOperationException ("Stream does not support reading at this time.");
			}

			int retVal = 0;
			EnsurePositionValid ();
			try
			{
				retVal = _streamManager.BaseStream.ReadByte ();
			}
			finally
			{
				UpdatePosition ();
			}
			return retVal;
		}

		/// <summary>
		/// Overridden. Writes data to a virtual log file spread across
		/// multiple log pages on a single device.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		/// <exception cref="T:InvalidOperationException">
		/// Stream does not support writing.
		/// </exception>
		public override void Write (byte[] buffer, int offset, int count)
		{
			// Sanity checks
			if (!CanWrite)
			{
				throw new InvalidOperationException ("Stream mode does not support writing.");
			}
			if (count == 0)
			{
				return;
			}

			EnsurePositionValid ();
			try
			{
				_streamManager.BaseStream.Write (buffer, offset, count);
			}
			finally
			{
				UpdatePosition ();
			}
		}

		/// <summary>
		/// Overridden. Writes a byte to the stream.
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="T:InvalidOperationException">
		/// Stream does not support writing.
		/// </exception>
		public override void WriteByte (byte value)
		{
			if (!CanWrite)
			{
				throw new InvalidOperationException ("Stream mode does not support writing.");
			}

			EnsurePositionValid ();
			try
			{
				_streamManager.BaseStream.WriteByte (value);
			}
			finally
			{
				UpdatePosition ();
			}
		}
		#endregion

		#region Private Methods
		private void ReadHeaders ()
		{
			VirtualLogFileHeader info1 = new VirtualLogFileHeader ();
			VirtualLogFileHeader info2 = new VirtualLogFileHeader ();
			bool valid1, valid2;

			Position = 0;
			info1.Read (_streamManager);

			Position = HeaderSize;
			info2.Read (_streamManager);

			valid1 = (info1.Timestamp.GetHashCode () == info1.Hash);
			valid2 = (info2.Timestamp.GetHashCode () == info2.Hash);

			// Check for cases where only one header is valid
			if (valid1 && !valid2)
			{
				_logFileInfo.CurrentHeader = info1;
				_writeFirstHeader = false;
				return;
			}
			else if (valid2 && !valid1)
			{
				_logFileInfo.CurrentHeader = info2;
				_writeFirstHeader = true;
				return;
			}

			// Check for no valid headers
			else if (!valid1 && !valid2)
			{
				throw new InvalidOperationException ("No valid file header.");
			}

			// Determine best header to use
			if ((info1.Timestamp < info2.Timestamp &&
				info1.Timestamp < int.MinValue && info2.Timestamp > int.MaxValue) ||
				(info1.Timestamp > info2.Timestamp))
			{
				_logFileInfo.CurrentHeader = info1;
				_writeFirstHeader = false;
			}
			else
			{
				_logFileInfo.CurrentHeader = info2;
				_writeFirstHeader = true;
			}
		}

		/// <summary>
		/// Ensures the stream is positioned correctly for reading or writing
		/// a log entry record.
		/// </summary>
		private void EnsurePositionValid ()
		{
			// Set the underlying stream position
			_streamManager.BaseStream.Position = TotalHeaderSize + _position;
		}

		private void UpdatePosition ()
		{
			_position = _streamManager.BaseStream.Position - TotalHeaderSize;
			if (_position < 0)
			{
				_position = 0;
			}
		}

		private void WriteHeader ()
		{
			// Save the current position
			long currentPosition = _streamManager.BaseStream.Position;
			try
			{
				// Header size is 24 bytes
				_streamManager.Flush ();
				_streamManager.BaseStream.Seek (_logFileInfo.StartOffset +
					(_writeFirstHeader ? 0 : HeaderSize), SeekOrigin.Begin);

				// Update state machine
				_writeFirstHeader = !_writeFirstHeader;
				++_logFileInfo.CurrentHeader.Timestamp;
				_logFileInfo.CurrentHeader.Hash = _logFileInfo.CurrentHeader.Timestamp.GetHashCode ();

				// Write the header block (24 bytes)
				_logFileInfo.CurrentHeader.Write (_streamManager);
				//_streamManager.Flush ();
				_headerDirty = false;
			}
			finally
			{
				_streamManager.BaseStream.Position = currentPosition;
			}
		}
		#endregion
	}
}
