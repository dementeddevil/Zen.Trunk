using System;
using System.IO;
using System.Text;

namespace Zen.Trunk.IO
{
    /// <summary>
    /// <c>SwitchingBinaryReader</c> behaves just like a BinaryReader however
    /// the encoding scheme used for reading strings can be changed dynamically.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class SwitchingBinaryReader : IDisposable
    {
        #region Private Fields
        private bool _disposed;
        private Stream _stream;
        private BinaryReader _reader;
        private bool _useUnicode;
        private Encoding _currentEncoding = Encoding.ASCII;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Creates a new <see cref="T:SwitchingBinaryReader" /> object against
        /// the specified <see cref="T:Stream" /> object.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="leaveOpen">if set to <c>true</c> the stream will be left open.</param>
        public SwitchingBinaryReader(Stream stream, bool leaveOpen = false)
        {
            if (!leaveOpen || stream is NonClosingStream)
            {
                _stream = stream;
            }
            else
            {
                _stream = new NonClosingStream(stream);
            }

            _reader = new BinaryReader(_stream);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the underlying stream object.
        /// </summary>
        public Stream BaseStream => _stream;

        /// <summary>
        /// Gets/sets a boolean value controlling whether text strings
        /// are read or written using ASCII or UNICODE encoding.
        /// </summary>
        public bool UseUnicode
        {
            get => _useUnicode;
            set
            {
                if (_useUnicode == value) return;

                _useUnicode = value;
                _currentEncoding = _useUnicode ? Encoding.Unicode : Encoding.ASCII;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #region Stream Methods
        /// <summary>
        /// Flushes all buffers to the underlying storage.
        /// </summary>
        /// <exception cref="T:ObjectDisposedException">
        /// Thrown if the object has been disposed.
        /// </exception>
        public void Flush()
        {
            CheckDisposed();
            _stream.Flush();
        }

        /// <summary>
        /// Closes the <see cref="T:BufferReaderWriter"/> object.
        /// </summary>
        public void Close()
        {
            if (_reader != null)
            {
                _reader.Close();
                _reader = null;
            }

            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }

            _disposed = true;
        }
        #endregion

        #region Reader Methods
        /// <summary>
        /// Reads the boolean.
        /// </summary>
        /// <returns></returns>
        public bool ReadBoolean()
        {
            CheckDisposed();
            return ((_reader.ReadByte() & 1) != 0);
        }

        /// <summary>
        /// Reads the byte.
        /// </summary>
        /// <returns></returns>
        public byte ReadByte()
        {
            CheckDisposed();
            return _reader.ReadByte();
        }

        /// <summary>
        /// Reads the bytes.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public byte[] ReadBytes(int count)
        {
            CheckDisposed();
            return _reader.ReadBytes(count);
        }

        /// <summary>
        /// Reads the character.
        /// </summary>
        /// <returns></returns>
        public char ReadChar()
        {
            CheckDisposed();
            var byteCount = _currentEncoding.GetMaxByteCount(1);
            var bytes = _reader.ReadBytes(byteCount);
            return _currentEncoding.GetChars(bytes)[0];
        }

        /// <summary>
        /// Reads the chars.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public char[] ReadChars(int count)
        {
            CheckDisposed();
            var byteCount = _currentEncoding.GetMaxByteCount(count);
            var bytes = _reader.ReadBytes(byteCount);
            return _currentEncoding.GetChars(bytes);
        }

        /// <summary>
        /// Reads the string.
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            CheckDisposed();
            var byteCount = _reader.ReadUInt16();
            var buffer = _reader.ReadBytes(byteCount);
            return _currentEncoding.GetString(buffer, 0, byteCount);
        }

        /// <summary>
        /// Reads the string exact.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public string ReadStringExact(int count)
        {
            CheckDisposed();
            var byteCount = count * (_currentEncoding.IsSingleByte ? 1 : 2);
            var buffer = _reader.ReadBytes(byteCount);
            return _currentEncoding.GetString(buffer, 0, byteCount).TrimEnd('\0');
        }

        /// <summary>
        /// Reads the single.
        /// </summary>
        /// <returns></returns>
        public float ReadSingle()
        {
            CheckDisposed();
            return _reader.ReadSingle();
        }

        /// <summary>
        /// Reads the double.
        /// </summary>
        /// <returns></returns>
        public double ReadDouble()
        {
            CheckDisposed();
            return _reader.ReadDouble();
        }

        /// <summary>
        /// Reads the decimal.
        /// </summary>
        /// <returns></returns>
        public Decimal ReadDecimal()
        {
            CheckDisposed();
            return _reader.ReadDecimal();
        }

        /// <summary>
        /// Reads the u int16.
        /// </summary>
        /// <returns></returns>
        [CLSCompliant(false)]
        public ushort ReadUInt16()
        {
            CheckDisposed();
            return _reader.ReadUInt16();
        }

        /// <summary>
        /// Reads the u int32.
        /// </summary>
        /// <returns></returns>
        [CLSCompliant(false)]
        public uint ReadUInt32()
        {
            CheckDisposed();
            return _reader.ReadUInt32();
        }

        /// <summary>
        /// Reads the u int64.
        /// </summary>
        /// <returns></returns>
        [CLSCompliant(false)]
        public ulong ReadUInt64()
        {
            CheckDisposed();
            return _reader.ReadUInt64();
        }

        /// <summary>
        /// Reads the int16.
        /// </summary>
        /// <returns></returns>
        public short ReadInt16()
        {
            CheckDisposed();
            return _reader.ReadInt16();
        }

        /// <summary>
        /// Reads the int32.
        /// </summary>
        /// <returns></returns>
        public int ReadInt32()
        {
            CheckDisposed();
            return _reader.ReadInt32();
        }

        /// <summary>
        /// Reads the int64.
        /// </summary>
        /// <returns></returns>
        public long ReadInt64()
        {
            CheckDisposed();
            return _reader.ReadInt64();
        }
        #endregion
        #endregion

        #region Protected Methods
        /// <summary>
        /// Disposes managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">Boolean value controlling whether
        /// managed resources should be disposed of.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Close();
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
        #endregion
    }
}
