using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Zen.Trunk.IO;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
	/// <c>VirtualBuffer</c> is a low-level unmanaged memory object
	/// used to create pages aligned on page boundaries and used
	/// exclusively to deal with interactions between the scatter/gather
	/// IO manager and the system.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The buffer size must be a multiple of the system page size.
	/// </para>
	/// <para>
	/// The buffer object exposes the buffer contents via a conventional
	/// .NET <see cref="T:Stream"/> object to allow for simple persistence
	/// schemes. The streams returned are also tracked for lifetime
	/// support with tracked references automatically removed when streams
	/// are closed - hence it is important that this is done in a timely
	/// manner.
	/// </para>
	/// </remarks>
	public sealed class VirtualBuffer : IVirtualBuffer
    {
        #region Private Objects
        private struct StreamInfo
        {
            #region Public Constructors
            public StreamInfo(int offset, int count)
            {
                Offset = offset;
                Count = count;
            }
            #endregion

            #region Public Properties
            public int Offset { get; }

            public int Count { get; }
            #endregion
        }

        private class BufferStream : Stream
        {
            #region Private Fields
            private VirtualBuffer _buffer;
            private Stream _innerStream;
            #endregion

            #region Public Constructors
            public BufferStream(VirtualBuffer buffer, int offset, int length, bool writable)
            {
                _buffer = buffer;
                unsafe
                {
                    _innerStream = new UnmanagedMemoryStream(_buffer.Buffer + offset,
                        length, length, writable ? FileAccess.ReadWrite : FileAccess.Read);
                }
            }
            #endregion

            #region Public Properties
            /// <summary>
            /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
            /// </summary>
            /// <value></value>
            /// <returns>true if the stream supports reading; otherwise, false.</returns>
            public override bool CanRead
            {
                get
                {
                    if (_innerStream != null)
                    {
                        return _innerStream.CanRead;
                    }
                    return false;
                }
            }

            /// <summary>
            /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
            /// </summary>
            /// <value></value>
            /// <returns>true if the stream supports seeking; otherwise, false.</returns>
            public override bool CanSeek
            {
                get
                {
                    if (_innerStream != null)
                    {
                        return _innerStream.CanSeek;
                    }
                    return false;
                }
            }

            /// <summary>
            /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
            /// </summary>
            /// <value></value>
            /// <returns>true if the stream supports writing; otherwise, false.</returns>
            public override bool CanWrite
            {
                get
                {
                    if (_innerStream != null)
                    {
                        return _innerStream.CanWrite;
                    }
                    return false;
                }
            }

            public override bool CanTimeout
            {
                get
                {
                    if (_innerStream != null)
                    {
                        return _innerStream.CanTimeout;
                    }
                    return false;
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
            /// Overridden. Closes the underlying stream and releases this
            /// stream from the owner device buffer.
            /// </summary>
            public override void Close()
            {
                if (_innerStream == null)
                {
                    Trace.WriteLine("Already closed");
                }

                // Close the inner stream object.
                if (_innerStream != null)
                {
                    _innerStream.Close();
                    _innerStream = null;
                }

                // Release this stream from the device buffer.
                if (_buffer != null)
                {
                    var bufferToClose = _buffer;
                    _buffer = null;
                    bufferToClose.ReleaseStream(this);
                }
            }

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
                throw new InvalidOperationException("Device streams are fixed in length.");
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
                _buffer.SetDirty();
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
                _buffer.SetDirty();
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
        #endregion

        #region Private Fields
        private static bool _gotPageSize;
        private static int _pageSize;

        private readonly SafeCommitableMemoryHandle _buffer;
        private readonly VirtualBufferCache _owner;
        private readonly int _cacheSlot;

        private bool _committed;
        private bool _disposed;
        private bool _dirty;

        private IDictionary<Stream, StreamInfo> _streams;
        #endregion

        #region Internal Constructors
        internal VirtualBuffer(SafeCommitableMemoryHandle buffer, int bufferSize, VirtualBufferCache owner, int slot)
        {
            if ((bufferSize % SystemPageSize) != 0)
            {
                throw new ArgumentException("bufferSize must be multiple of SystemPageSize.", nameof(bufferSize));
            }

            _buffer = buffer;
            BufferSize = bufferSize;
            _owner = owner;
            _cacheSlot = slot;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the size of the system page.
        /// </summary>
        /// <value>The size of the page.</value>
        public static int SystemPageSize
        {
            get
            {
                if (!_gotPageSize)
                {
                    var systemInfo = new SafeNativeMethods.SYSTEM_INFO();
                    SafeNativeMethods.GetSystemInfo(ref systemInfo);
                    _pageSize = systemInfo.dwPageSize;
                    _gotPageSize = true;
                }
                return _pageSize;
            }
        }

        /// <summary>
        /// Gets the buffer unique identifier.
        /// </summary>
        /// <value>
        /// The buffer unique identifier.
        /// </value>
        public string BufferId => $"{_owner.CacheId}:{_cacheSlot}";

        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <value>
        /// The size of the buffer.
        /// </value>
        public int BufferSize { get; }

        /// <summary>
		/// Gets a value indicating whether this buffer instance is dirty.
		/// </summary>
		/// <value>
		/// <c>true</c> if dirty; otherwise, <c>false</c>.
		/// </value>
		public bool IsDirty => _dirty;
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly",
            Justification = "Virtual buffer objects are recycled to avoid fragmentation of the underlying virtual memory block.")]
        public void Dispose()
        {
            DisposeManagedObjects();
        }

        /// <summary>
        /// Compares the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public int CompareTo(IVirtualBuffer buffer)
        {
            CheckDisposed();
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (buffer == this)
            {
                return 0;
            }
            unsafe
            {
                return MemcmpImpl(Buffer, ((VirtualBuffer)buffer).Buffer, BufferSize);
            }
        }

        /// <summary>
        /// Copies the contents of this instance to the specified instance.
        /// </summary>
        /// <param name="destination">The destination.</param>
        public void CopyTo(IVirtualBuffer destination)
        {
            CheckDisposed();
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (destination == this)
            {
                return;
            }
            if (destination.BufferSize != BufferSize)
            {
                throw new ArgumentException("Buffer not the same size.");
            }
            unsafe
            {
                MemcpyImpl(Buffer, ((VirtualBuffer)destination).Buffer, BufferSize);
            }
        }

        /// <summary>
        /// Initialises the contents of this instance from the specified array
        /// of bytes.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <exception cref="T:ArgumentNullException">
        /// Thrown if the buffer is <c>null</c>.
        /// </exception>
        /// <exception cref="T:ArgumentException">
        /// Thrown if the supplied buffer is a different size to this instance.
        /// </exception>
        public void InitFrom(byte[] buffer)
        {
            CheckDisposed();
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (buffer.Length != BufferSize)
            {
                throw new ArgumentException("Buffer not the same size.");
            }
            unsafe
            {
                fixed (byte* pBuffer = buffer)
                {
                    MemcpyImpl(pBuffer, Buffer, BufferSize);
                }
            }
        }

        /// <summary>
        /// Copies the contents of this instance to the specified byte array.
        /// </summary>
        /// <param name="buffer"></param>
        /// <exception cref="T:ArgumentNullException">
        /// Thrown if the buffer is <c>null</c>.
        /// </exception>
        /// <exception cref="T:ArgumentException">
        /// Thrown if the supplied buffer is a different size to this instance.
        /// </exception>
        public void CopyTo(byte[] buffer)
        {
            CheckDisposed();
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (buffer.Length != BufferSize)
            {
                throw new ArgumentException("Buffer not the same size.");
            }
            unsafe
            {
                fixed (byte* pBuffer = buffer)
                {
                    MemcpyImpl(Buffer, pBuffer, BufferSize);
                }
            }
        }

        /// <summary>
        /// Gets a tracked <see cref="T:Stream"/> backed by this instance.
        /// </summary>
        /// <param name="offset">Zero-based offset into buffer block to position the stream.</param>
        /// <param name="count">Number of bytes for the stream length.</param>
        /// <param name="writable">
        /// <c>true</c> if the stream is to be writable; otherwise <c>false</c>.
        /// </param>
        /// <returns></returns>
        /// <exception cref="T:InvalidOperationException">
        /// Thrown if two tracked streams attempt to write to the same region.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Virtual buffers keep track of all opened streams and will throw
        /// exception if an attempt is made to get multiple writable streams
        /// on the same region of buffer space.
        /// </para>
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "Stream disposed of by caller.")]
        public Stream GetBufferStream(int offset, int count, bool writable)
        {
            CheckDisposed();
            if (_streams == null)
            {
                _streams = new Dictionary<Stream, StreamInfo>();
            }
            if (_streams.Count > 0)
            {
                foreach (var info in _streams.Values)
                {
                    // Does this match?
                    if ((info.Offset == offset) ||
                        ((offset < info.Offset) && (offset + count > info.Offset)) ||
                        ((offset > info.Offset) && (info.Offset + info.Count > offset)))
                    {
                        throw new InvalidOperationException("Sharing violation.");
                    }
                }
            }

            // Construct memory stream on correct buffer object
            var stream = new BufferStream(this, offset, count, writable);
            _streams.Add(stream, new StreamInfo(offset, count));
            return new NonResizeableStream(stream);
        }

        /// <summary>
        /// Marks this instance as dirty.
        /// </summary>
        public void SetDirty()
        {
            _dirty = true;
        }

        /// <summary>
        /// Marks this instance as clean.
        /// </summary>
        public void ClearDirty()
        {
            _dirty = false;
        }
        #endregion

        #region Internal Properties
        internal int CacheSlot => _cacheSlot;

        internal unsafe byte* Buffer
        {
            get
            {
                /*if (!_committed)
				{
					Allocate ();
				}*/
                CheckDisposed();
                if (!_committed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
                return (byte*)_buffer.DangerousGetHandle().ToPointer();
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Disposes the managed objects.
        /// </summary>
        private void DisposeManagedObjects()
        {
            if (_streams != null)
            {
                var streams = new Stream[_streams.Keys.Count];
                _streams.Keys.CopyTo(streams, 0);
                for (var index = 0; index < streams.Length; ++index)
                {
                    streams[index].Dispose();
                }
                _streams = null;
            }

            // If we are committed, then free
            if (_committed)
            {
                if (_dirty)
                {
                    Trace.TraceWarning("Freeing dirty buffer.");
                }
                Free();
            }

            // Notify owner cache that this buffer is available
            _owner.FreeBuffer(this);
        }
        #endregion

        #region Internal Methods
        internal static unsafe int MemcmpImpl(byte* src, byte* dest, int len)
        {
            if (len >= 0x10)
            {
                do
                {
                    var result = *((int*)dest) - *((int*)src);
                    if (result != 0)
                    {
                        return result;
                    }
                    result = *((int*)(dest + 4)) - *((int*)(src + 4));
                    if (result != 0)
                    {
                        return result;
                    }
                    result = *((int*)(dest + 8)) - *((int*)(src + 8));
                    if (result != 0)
                    {
                        return result;
                    }
                    result = *((int*)(dest + 12)) - *((int*)(src + 12));
                    if (result != 0)
                    {
                        return result;
                    }
                    dest += 0x10;
                    src += 0x10;
                }
                while ((len -= 0x10) >= 0x10);
            }
            if (len > 0)
            {
                if ((len & 8) != 0)
                {
                    var result = *((int*)dest) - *((int*)src);
                    if (result != 0)
                    {
                        return result;
                    }
                    result = *((int*)(dest + 4)) - *((int*)(src + 4));
                    if (result != 0)
                    {
                        return result;
                    }
                    dest += 8;
                    src += 8;
                }
                if ((len & 4) != 0)
                {
                    var result = *((int*)dest) - *((int*)src);
                    if (result != 0)
                    {
                        return result;
                    }
                    dest += 4;
                    src += 4;
                }
                if ((len & 2) != 0)
                {
                    var result = (*((short*)dest) - *((short*)src));
                    if (result != 0)
                    {
                        return result;
                    }
                    dest += 2;
                    src += 2;
                }
                if ((len & 1) != 0)
                {
                    dest++;
                    src++;
                    if (dest[0] < src[0])
                    {
                        return -1;
                    }
                    else if (dest[0] > src[0])
                    {
                        return 1;
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Copies <paramref name="len"/> bytes from <paramref name="src"/> to
        /// <paramref name="dest"/>.
        /// </summary>
        /// <param name="src">A pointer to the source buffer.</param>
        /// <param name="dest">A pointer to the destination buffer.</param>
        /// <param name="len">Number of bytes to be copied.</param>
        /// <remarks>
        /// The two regions should not overlap.
        /// </remarks>
        internal static unsafe void MemcpyImpl(byte* src, byte* dest, int len)
        {
            if (len >= 0x10)
            {
                do
                {
                    *((int*)dest) = *((int*)src);
                    *((int*)(dest + 4)) = *((int*)(src + 4));
                    *((int*)(dest + 8)) = *((int*)(src + 8));
                    *((int*)(dest + 12)) = *((int*)(src + 12));
                    dest += 0x10;
                    src += 0x10;
                }
                while ((len -= 0x10) >= 0x10);
            }
            if (len > 0)
            {
                if ((len & 8) != 0)
                {
                    *((int*)dest) = *((int*)src);
                    *((int*)(dest + 4)) = *((int*)(src + 4));
                    dest += 8;
                    src += 8;
                }
                if ((len & 4) != 0)
                {
                    *((int*)dest) = *((int*)src);
                    dest += 4;
                    src += 4;
                }
                if ((len & 2) != 0)
                {
                    *((short*)dest) = *((short*)src);
                    dest += 2;
                    src += 2;
                }
                if ((len & 1) != 0)
                {
                    dest++;
                    src++;
                    dest[0] = src[0];
                }
            }
        }

        internal void Allocate()
        {
            if (!_committed)
            {
#if IOTRACE
				Trace.TraceInformation("Allocate {0} from {1} of size {2}",
					BufferId, new IntPtr(_buffer), _bufferSize);
#endif
                SafeNativeMethods.VirtualCommit(_buffer, SafeNativeMethods.PAGE_READWRITE);
                _committed = true;
            }
            _disposed = false;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void Free()
        {
            if (_committed)
            {
#if IOTRACE
				Trace.TraceInformation("Deallocate {0} from {1} of size {2}",
					BufferId, new IntPtr(_buffer), _bufferSize);
#endif
                // Protect page
                //SafeNativeMethods.VirtualProtect(_buffer, SafeNativeMethods.PAGE_NOACCESS);

                // Decommit memory block
                SafeNativeMethods.VirtualDecommit(_buffer);
                _committed = false;
            }
            _disposed = true;
        }

        /// <summary>
        /// Releases the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        private void ReleaseStream(Stream stream)
        {
            // Sanity check
            if (_streams == null || !_streams.ContainsKey(stream))
            {
                throw new ArgumentException("Not matching stream object.");
            }

            // Remove stream from collection
            _streams.Remove(stream);
        }
        #endregion
    }
}
