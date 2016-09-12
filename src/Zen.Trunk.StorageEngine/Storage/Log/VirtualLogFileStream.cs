using System;
using System.IO;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// <c>VirtualLogFileStream</c> handles writing log information to a virtual
    /// stream that exists on top of the log device and extends <see cref="Stream"/>.
    /// </summary>
    /// <seealso cref="System.IO.Stream" />
    /// <remarks>
    /// Log streams maintain two header blocks and a set of log entries.
    /// The use of two headers allows the system to recover if a power failure occurs
    /// during writing of header data.
    /// </remarks>
    public class VirtualLogFileStream : Stream
    {
        #region Private Fields
        private const int HeaderSize = 24;
        private const int TotalHeaderSize = HeaderSize * 2;

        private readonly LogPageDevice _device;
        private readonly VirtualLogFileInfo _logFileInfo;
        private readonly BufferReaderWriter _streamManager;
        private bool _writeFirstHeader = true;
        private bool _headerDirty;
        private long _position;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Creates a new <see cref="T:VirtualLogFileStream"/> object owned by
        /// the specified <see cref="T:LogPageDevice"/> and mapped against the
        /// given <see cref="T:Stream"/>. The log file has the characteristics
        /// specified in the <see cref="T:VirtualLogFileInfo"/>.
        /// </summary>
        /// <param name="device">The log device that owns this log stream.</param>
        /// <param name="backingStore">The stream backing store for this object.</param>
        /// <param name="logFileInfo"></param>
        public VirtualLogFileStream(
            LogPageDevice device, Stream backingStore, VirtualLogFileInfo logFileInfo)
        {
            _device = device;
            _logFileInfo = logFileInfo;
            _streamManager = new BufferReaderWriter(backingStore);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the log file Id.
        /// </summary>
        public LogFileId FileId => _logFileInfo.FileId;

        /// <summary>
        /// Gets/sets the previous log file Id.
        /// </summary>
        public LogFileId PrevFileId
        {
            get
            {
                return _logFileInfo.CurrentHeader.PreviousLogFileId;
            }
            set
            {
                if (_logFileInfo.CurrentHeader.PreviousLogFileId != value)
                {
                    _logFileInfo.CurrentHeader.PreviousLogFileId = value;
                    _headerDirty = true;
                }
            }
        }

        /// <summary>
        /// Gets/sets the next log file Id.
        /// </summary>
        public LogFileId NextFileId
        {
            get
            {
                return _logFileInfo.CurrentHeader.NextLogFileId;
            }
            set
            {
                if (_logFileInfo.CurrentHeader.NextLogFileId != value)
                {
                    _logFileInfo.CurrentHeader.NextLogFileId = value;
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
        public bool Recovery => _device.IsInRecovery;

        /// <summary>
        /// Gets a boolean value indicating whether this stream is read-only.
        /// </summary>
        public bool ReadOnly => _device.IsReadOnly | Recovery;

        /// <summary>
        /// Overridden. Gets a boolean value indicating whether the stream
        /// supports reading.
        /// </summary>
        public override bool CanRead => Recovery;

        /// <summary>
        /// Overridden. Gets a boolean value indicating whether the stream
        /// supports writing.
        /// </summary>
        public override bool CanWrite => !Recovery && !_device.IsReadOnly;

        /// <summary>
        /// Overridden. Gets a boolean value indicating whether the stream
        /// supports seeking.
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// Overridden. Gets a value indicating the stream length.
        /// This is equal to the length reported by the log file info plus the
        /// total header size.
        /// </summary>
        public override long Length => _logFileInfo.Length - TotalHeaderSize;

        /// <summary>
        /// Gets/sets a value which indicates the current stream position.
        /// </summary>
        /// <remarks>
        /// The position is relative to the start of the data section,
        /// skipping the two headers.
        /// </remarks>
        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                Seek(value, SeekOrigin.Begin);
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
        public void InitNew()
        {
            // Write header twice and flush
            _logFileInfo.IsAllocated = true;
            WriteHeader();
            WriteHeader();
            Flush();
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
        public void InitLoad()
        {
            // Read headers and determine correct version
            ReadHeaders();
            Position = 0;
        }

        /// <summary>
        /// Writes the specified <see cref="T:LogEntry"/> object to the
        /// log file stream.
        /// </summary>
        /// <param name="entry"><see cref="T:LogEntry"/> log entry to be 
        /// written.</param>
        public void WriteEntry(LogEntry entry)
        {
            lock (_streamManager)
            {
                // Record cursor position of last log record written to the
                //	stream and move stream position to write point.
                entry.LastLog = _logFileInfo.CurrentHeader.LastCursor;
                Seek(_logFileInfo.CurrentHeader.Cursor, SeekOrigin.Begin);

                // Serialise the log entry
                EnsurePositionValid();
                try
                {
                    entry.Write(_streamManager);
                }
                finally
                {
                    UpdatePosition();
                }

                // If write was successfull then we can update the header.
                _logFileInfo.CurrentHeader.LastCursor =
                    _logFileInfo.CurrentHeader.Cursor;
                _logFileInfo.CurrentHeader.Cursor = (uint)Position;
                WriteHeader();
            }
        }

        /// <summary>
        /// Reads the next log entry from the log file stream.
        /// </summary>
        /// <returns><see cref="T:LogEntry"/> object.</returns>
        public LogEntry ReadEntry()
        {
            LogEntry entry;
            EnsurePositionValid();
            try
            {
                entry = LogEntry.ReadEntry(_streamManager);
            }
            finally
            {
                UpdatePosition();
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
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
            {
                throw new InvalidOperationException("Current stream mode does not support seeking.");
            }

            // Determine new offset
            var newOffset = _logFileInfo.StartOffset + TotalHeaderSize;
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
            _streamManager.BaseStream.Seek(newOffset, SeekOrigin.Begin);
            UpdatePosition();
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
        public override void SetLength(long value)
        {
            throw new NotSupportedException("Setting page stream length is not supported.");
        }

        /// <summary>
        /// Overridden. Flushes all pending changes to the stream.
        /// </summary>
        public override void Flush()
        {
            if (_streamManager != null && CanWrite)
            {
                // Write header block if necessary
                if (_headerDirty)
                {
                    WriteHeader();
                }

                // Flush to underlying stream
                _streamManager.Flush();
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
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
            {
                throw new InvalidOperationException("Stream does not support reading at this time.");
            }

            int retVal;
            EnsurePositionValid();
            try
            {
                retVal = _streamManager.BaseStream.Read(buffer, offset, count);
            }
            finally
            {
                UpdatePosition();
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
        public override int ReadByte()
        {
            if (!CanRead)
            {
                throw new InvalidOperationException("Stream does not support reading at this time.");
            }

            int retVal;
            EnsurePositionValid();
            try
            {
                retVal = _streamManager.BaseStream.ReadByte();
            }
            finally
            {
                UpdatePosition();
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
        public override void Write(byte[] buffer, int offset, int count)
        {
            // Sanity checks
            if (!CanWrite)
            {
                throw new InvalidOperationException("Stream mode does not support writing.");
            }
            if (count == 0)
            {
                return;
            }

            EnsurePositionValid();
            try
            {
                _streamManager.BaseStream.Write(buffer, offset, count);
            }
            finally
            {
                UpdatePosition();
            }
        }

        /// <summary>
        /// Overridden. Writes a byte to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="T:InvalidOperationException">
        /// Stream does not support writing.
        /// </exception>
        public override void WriteByte(byte value)
        {
            if (!CanWrite)
            {
                throw new InvalidOperationException("Stream mode does not support writing.");
            }

            EnsurePositionValid();
            try
            {
                _streamManager.BaseStream.WriteByte(value);
            }
            finally
            {
                UpdatePosition();
            }
        }
        #endregion

        #region Private Methods
        private void ReadHeaders()
        {
            // Read first header from log stream
            Position = 0;
            var firstHeader = new VirtualLogFileHeader();
            firstHeader.Read(_streamManager);

            // Read second header from log stream
            Position = HeaderSize;
            var secondHeader = new VirtualLogFileHeader();
            secondHeader.Read(_streamManager);

            // Determine whether each header is valid by comparison of timestamp hash
            //  with the value in the hash field (timestamp is written first and hash last)
            var isFirstHeaderValid = firstHeader.Timestamp.GetHashCode() == firstHeader.Hash;
            var isSecondHeaderValid = secondHeader.Timestamp.GetHashCode() == secondHeader.Hash;

            // Check for cases where only one header is valid
            if (isFirstHeaderValid && !isSecondHeaderValid)
            {
                _logFileInfo.CurrentHeader = firstHeader;
                _writeFirstHeader = false;
                return;
            }
            if (isSecondHeaderValid && !isFirstHeaderValid)
            {
                _logFileInfo.CurrentHeader = secondHeader;
                _writeFirstHeader = true;
                return;
            }

            // Check for no valid headers
            if (!isFirstHeaderValid && !isSecondHeaderValid)
            {
                throw new InvalidOperationException("No valid file header.");
            }

            // Determine best header to use
            if ((firstHeader.Timestamp < secondHeader.Timestamp &&
                firstHeader.Timestamp < int.MinValue && secondHeader.Timestamp > int.MaxValue) ||
                (firstHeader.Timestamp > secondHeader.Timestamp))
            {
                _logFileInfo.CurrentHeader = firstHeader;
                _writeFirstHeader = false;
            }
            else
            {
                _logFileInfo.CurrentHeader = secondHeader;
                _writeFirstHeader = true;
            }
        }

        /// <summary>
        /// Ensures the stream is positioned correctly for reading or writing
        /// a log entry record.
        /// </summary>
        private void EnsurePositionValid()
        {
            // Set the underlying stream position
            _streamManager.BaseStream.Position = TotalHeaderSize + _position;
        }

        private void UpdatePosition()
        {
            _position = _streamManager.BaseStream.Position - TotalHeaderSize;
            if (_position < 0)
            {
                _position = 0;
            }
        }

        private void WriteHeader()
        {
            // Save the current position
            var currentPosition = _streamManager.BaseStream.Position;
            try
            {
                // Header size is 24 bytes
                _streamManager.Flush();
                _streamManager.BaseStream.Seek(_logFileInfo.StartOffset +
                    (_writeFirstHeader ? 0 : HeaderSize), SeekOrigin.Begin);

                // Update state machine
                _writeFirstHeader = !_writeFirstHeader;
                ++_logFileInfo.CurrentHeader.Timestamp;
                _logFileInfo.CurrentHeader.Hash = _logFileInfo.CurrentHeader.Timestamp.GetHashCode();

                // Write the header block (24 bytes)
                _logFileInfo.CurrentHeader.Write(_streamManager);
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
