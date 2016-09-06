using System;
using System.IO;
using System.Text;

namespace Zen.Trunk.Storage.IO
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class BufferReaderWriter : IDisposable
    {
        #region Private Fields
        private bool _disposed;
        private NonClosingStream _stream;
        private BinaryReader _reader;
        private BinaryWriter _writer;
        private bool _isWritable = true;
        private bool _useUnicode;
        private Encoding _currentEncoding = Encoding.ASCII;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Creates a new <see cref="T:BufferReaderWriter"/> object against
        /// the specified <see cref="T:Stream"/> object.
        /// </summary>
        /// <param name="stream"></param>
        public BufferReaderWriter(Stream stream)
        {
            // Wrap stream in non-closing stream
            if (!(stream is NonClosingStream))
            {
                stream = new NonClosingStream(stream);
            }
            _stream = (NonClosingStream)stream;

            // Always create reader but check stream for writer
            _reader = new BinaryReader(stream);
            if (stream.CanWrite)
            {
                _writer = new BinaryWriter(stream);
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the underlying stream object.
        /// </summary>
        public Stream BaseStream => _stream;

        /// <summary>
        /// Gets/sets a boolean value controlling whether writes to this
        /// instance are passed to the underlying stream.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is writeable; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// When the <see cref="BufferReaderWriter"/> is not writeable all 
        /// writes are transformed into no-ops that move the file pointer by
        /// the same amount as if the write had taken place.
        /// </remarks>
        public bool IsWritable
        {
            get
            {
                return _isWritable;
            }
            set
            {
                _isWritable = value;
            }
        }

        /// <summary>
        /// Gets/sets a boolean value controlling whether text strings
        /// are read or written using ASCII or UNICODE encoding.
        /// </summary>
        public bool UseUnicode
        {
            get
            {
                return _useUnicode;
            }
            set
            {
                if (_useUnicode != value)
                {
                    _useUnicode = value;
                    if (_useUnicode)
                    {
                        _currentEncoding = Encoding.Unicode;
                    }
                    else
                    {
                        _currentEncoding = Encoding.ASCII;
                    }
                }
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
            if (_writer != null)
            {
                _writer.Close();
                _writer = null;
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
            return _currentEncoding.GetString(buffer);
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
            return _currentEncoding.GetString(buffer);
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

        #region Writer Methods
        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public void Write(bool value)
        {
            Write((byte)(value ? 1 : 0));
        }

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(byte value)
        {
            CheckWritable();
            if (!IsWritable)
            {
                ReadByte();
            }
            else
            {
                _writer.Write(value);
            }
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        public void Write(byte[] buffer, int index, int count)
        {
            CheckWritable();
            if (!IsWritable)
            {
                _stream.Seek(count, SeekOrigin.Current);
            }
            else
            {
                _writer.Write(buffer, index, count);
            }
        }

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(char value)
        {
            CheckWritable();
            var byteCount = _currentEncoding.GetByteCount(new[] { value });
            if (!IsWritable)
            {
                _stream.Seek(byteCount, SeekOrigin.Current);
            }
            else
            {
                var bytes = _currentEncoding.GetBytes(new[] { value });
                _writer.Write(bytes, 0, byteCount);
            }
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        public void Write(char[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        public void Write(char[] buffer, int index, int count)
        {
            CheckWritable();
            var byteCount = _currentEncoding.GetByteCount(buffer, index, count);
            if (!IsWritable)
            {
                _stream.Seek(byteCount, SeekOrigin.Current);
            }
            else
            {
                var bytes = _currentEncoding.GetBytes(buffer, index, count);
                _writer.Write(bytes, 0, byteCount);
            }
        }

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(string value)
        {
            CheckWritable();
            if (!IsWritable)
            {
                ReadString();
            }
            else
            {
                var byteCount = _currentEncoding.GetByteCount(value);
                var bytes = _currentEncoding.GetBytes(value);

                _writer.Write((ushort)byteCount);
                _writer.Write(bytes, 0, byteCount);
            }
        }

        /// <summary>
        /// Writes the string exact.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="count">The count.</param>
        public void WriteStringExact(string value, int count)
        {
            CheckWritable();
            if (value.Length > count)
            {
                value = value.Substring(0, count);
            }
            if (!IsWritable)
            {
                var byteCount = _currentEncoding.GetByteCount(
                    new string(' ', count));
                _writer.Seek(byteCount, SeekOrigin.Current);
            }
            else
            {
                var byteCount = _currentEncoding.GetByteCount(value);
                var bytes = _currentEncoding.GetBytes(value);
                _writer.Write(bytes, 0, byteCount);

                if (value.Length < count)
                {
                    byteCount = _currentEncoding.GetByteCount(
                        new string(' ', count - value.Length));
                    bytes = new byte[byteCount];
                    _writer.Write(bytes, 0, byteCount);
                }
            }
        }

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(float value)
        {
            CheckWritable();
            if (!IsWritable)
            {
                ReadSingle();
            }
            else
            {
                _writer.Write(value);
            }
        }

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(double value)
        {
            CheckWritable();
            if (!IsWritable)
            {
                ReadDouble();
            }
            else
            {
                _writer.Write(value);
            }
        }

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(Decimal value)
        {
            CheckWritable();
            if (!IsWritable)
            {
                ReadDecimal();
            }
            else
            {
                _writer.Write(value);
            }
        }

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        [CLSCompliant(false)]
        public void Write(ushort value)
        {
            CheckWritable();
            if (!IsWritable)
            {
                ReadUInt16();
            }
            else
            {
                _writer.Write(value);
            }
        }

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        [CLSCompliant(false)]
        public void Write(uint value)
        {
            CheckWritable();
            if (!IsWritable)
            {
                ReadUInt32();
            }
            else
            {
                _writer.Write(value);
            }
        }

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        [CLSCompliant(false)]
        public void Write(ulong value)
        {
            CheckWritable();
            if (!IsWritable)
            {
                ReadUInt64();
            }
            else
            {
                _writer.Write(value);
            }
        }

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(short value)
        {
            CheckWritable();
            if (!IsWritable)
            {
                ReadInt16();
            }
            else
            {
                _writer.Write(value);
            }
        }

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(int value)
        {
            CheckWritable();
            if (!IsWritable)
            {
                ReadInt32();
            }
            else
            {
                _writer.Write(value);
            }
        }

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(long value)
        {
            CheckWritable();
            if (!IsWritable)
            {
                ReadInt64();
            }
            else
            {
                _writer.Write(value);
            }
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
        private void CheckWritable()
        {
            CheckDisposed();
            if (_writer == null)
            {
                throw new InvalidOperationException("Not writable.");
            }
        }

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
