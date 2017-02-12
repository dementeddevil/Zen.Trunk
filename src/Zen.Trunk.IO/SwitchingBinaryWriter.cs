using System;
using System.IO;
using System.Text;

namespace Zen.Trunk.IO
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class SwitchingBinaryWriter : IDisposable
    {
        #region Private Fields
        private bool _disposed;
        private Stream _stream;
        private BinaryWriter _writer;
        private bool _useUnicode;
        private Encoding _currentEncoding = Encoding.ASCII;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Creates a new <see cref="T:SwitchingBinaryWriter" /> object against
        /// the specified <see cref="T:Stream" /> object.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="leaveOpen">if set to <c>true</c> [leave open].</param>
        public SwitchingBinaryWriter(Stream stream, bool leaveOpen = false)
        {
            // Wrap stream in non-closing stream
            if (!leaveOpen)
            {
                _stream = stream;
            }
            else
            {
                if (!(stream is NonClosingStream))
                {
                    stream = new NonClosingStream(stream);
                }
                else
                {
                    _stream = (NonClosingStream)stream;
                }
            }

            _writer = new BinaryWriter(stream);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the underlying stream object.
        /// </summary>
        public Stream BaseStream => _stream;

        /// <summary>
        /// Gets or sets a value indicating whether [write to underlying stream].
        /// </summary>
        /// <value>
        /// <c>true</c> if [write to underlying stream]; otherwise, <c>false</c>.
        /// </value>
        public bool WriteToUnderlyingStream { get; set; } = true;

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
            CheckDisposed();
            if (!WriteToUnderlyingStream)
            {
                _stream.Seek(1, SeekOrigin.Current);
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
            CheckDisposed();
            if (!WriteToUnderlyingStream)
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
            CheckDisposed();
            var byteCount = _currentEncoding.GetByteCount(new[] { value });
            if (!WriteToUnderlyingStream)
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
            CheckDisposed();
            var byteCount = _currentEncoding.GetByteCount(buffer, index, count);
            if (!WriteToUnderlyingStream)
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
            CheckDisposed();
            if (!WriteToUnderlyingStream)
            {
                // TODO: Assume we want to skip an existing string
                // Therefore, read existing string length
                //  then skip bytes
                throw new InvalidOperationException();
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
            CheckDisposed();
            if (value.Length > count)
            {
                value = value.Substring(0, count);
            }
            if (!WriteToUnderlyingStream)
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
            CheckDisposed();
            if (!WriteToUnderlyingStream)
            {
                _stream.Seek(4, SeekOrigin.Current);
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
            CheckDisposed();
            if (!WriteToUnderlyingStream)
            {
                _stream.Seek(8, SeekOrigin.Current);
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
            CheckDisposed();
            if (!WriteToUnderlyingStream)
            {
                _stream.Seek(16, SeekOrigin.Current);
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
            CheckDisposed();
            if (!WriteToUnderlyingStream)
            {
                _stream.Seek(2, SeekOrigin.Current);
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
            CheckDisposed();
            if (!WriteToUnderlyingStream)
            {
                _stream.Seek(4, SeekOrigin.Current);
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
            CheckDisposed();
            if (!WriteToUnderlyingStream)
            {
                _stream.Seek(8, SeekOrigin.Current);
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
            CheckDisposed();
            if (!WriteToUnderlyingStream)
            {
                _stream.Seek(2, SeekOrigin.Current);
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
            CheckDisposed();
            if (!WriteToUnderlyingStream)
            {
                _stream.Seek(4, SeekOrigin.Current);
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
            CheckDisposed();
            if (!WriteToUnderlyingStream)
            {
                _stream.Seek(8, SeekOrigin.Current);
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
