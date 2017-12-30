// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VirtualStream.cs" company="Zen Design Software">
//   © Zen Design Software 2015
// </copyright>
// <summary>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace Zen.Trunk.IO
{
    /// <summary>
    /// </summary>
    public class VirtualStream : Stream
    {
        #region Private Fields
        private readonly int _bufferSize = 4096;
        private Stream _fileBackingStore;
        private bool _isDisposed;
        private Stream _memoryBackingStore;
        private readonly VirtualStreamMemoryFlag _memoryFlag = VirtualStreamMemoryFlag.AutoOverflowToDisk;
        private readonly int _thresholdSize = 16384;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initialises a new instance of the <see cref="VirtualStream" /> class.
        /// </summary>
        public VirtualStream()
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="VirtualStream" /> class.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer.</param>
        public VirtualStream(int bufferSize)
        {
            _bufferSize = bufferSize;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="VirtualStream" /> class.
        /// </summary>
        /// <param name="innerStream">The inner stream.</param>
        public VirtualStream(Stream innerStream)
        {
            _memoryBackingStore = innerStream;
            _memoryFlag = VirtualStreamMemoryFlag.OnlyInMemory;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="VirtualStream" /> class.
        /// </summary>
        /// <param name="memoryFlag">The memory flag.</param>
        public VirtualStream(VirtualStreamMemoryFlag memoryFlag)
        {
            _memoryFlag = memoryFlag;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="VirtualStream" /> class.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="thresholdSize">Size of the threshold.</param>
        public VirtualStream(int bufferSize, int thresholdSize)
        {
            _bufferSize = bufferSize;
            _thresholdSize = thresholdSize;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="VirtualStream" /> class.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="memoryFlag">The memory flag.</param>
        public VirtualStream(int bufferSize, VirtualStreamMemoryFlag memoryFlag)
        {
            _bufferSize = bufferSize;
            _memoryFlag = memoryFlag;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the
        /// current stream supports reading.
        /// </summary>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead => UnderlyingStream.CanRead;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the
        /// current stream supports seeking.
        /// </summary>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek => UnderlyingStream.CanSeek;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the
        /// current stream supports writing.
        /// </summary>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite => UnderlyingStream.CanWrite;

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        public override long Length => UnderlyingStream.Length;

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the
        /// current stream.
        /// </summary>
        /// <returns>The current position within the stream.</returns>
        public override long Position
        {
            get => UnderlyingStream.Position;
            set => UnderlyingStream.Position = value;
        }

        /// <summary>
        /// Gets the underlying stream.
        /// </summary>
        /// <value>
        /// The underlying stream.
        /// </value>
        public Stream UnderlyingStream
        {
            get
            {
                CheckNotDisposed();
                EnsureStream();
                return _memoryBackingStore ?? _fileBackingStore;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and
        /// causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            UnderlyingStream.Flush();
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the
        /// current stream and advances the position within the stream by the number of
        /// bytes read.
        /// </summary>
        /// <param name="buffer">
        /// An array of bytes. When this method returns, the buffer
        /// contains the specified byte array with the values between
        /// <paramref name="offset" /> and (<paramref name="offset" /> +
        /// <paramref name="count" /> - 1) replaced by the bytes read from the current
        /// source.
        /// </param>
        /// <param name="offset">
        /// The zero-based byte offset in <paramref name="buffer" />
        /// at which to begin storing the data read from the current stream.
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to be read from the current
        /// stream.
        /// </param>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the
        /// number of bytes requested if that many bytes are not currently available,
        /// or zero (0) if the end of the stream has been reached.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return UnderlyingStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the
        /// current stream and advances the current position within this stream by the
        /// number of bytes written.
        /// </summary>
        /// <param name="buffer">
        /// An array of bytes. This method copies
        /// <paramref name="count" /> bytes from <paramref name="buffer" /> to the
        /// current stream.
        /// </param>
        /// <param name="offset">
        /// The zero-based byte offset in <paramref name="buffer" />
        /// at which to begin copying bytes to the current stream.
        /// </param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            UnderlyingStream.Write(buffer, offset, count);
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current
        /// stream.
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
            // TODO: Check whether result of seek will be seek to uninitialised
            //	part of the file - if so we may optimise by upgrading the
            //	stream prior to performing the seek...
            return UnderlyingStream.Seek(offset, origin);
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            CheckNotDisposed();

            if (_memoryFlag == VirtualStreamMemoryFlag.AutoOverflowToDisk && _memoryBackingStore != null
                && value > _thresholdSize)
            {
                UpgradeStreamIfNeeded(true);
            }

            UnderlyingStream.SetLength(value);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Releases the unmanaged resources used by the
        /// <see cref="T:System.IO.Stream" /> and optionally releases the managed
        /// resources.
        /// </summary>
        /// <param name="disposing">
        /// true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _isDisposed = true;
                if (_memoryBackingStore != null)
                {
                    _memoryBackingStore.Flush();
                    _memoryBackingStore.Dispose();
                    _memoryBackingStore = null;
                }

                if (_fileBackingStore != null)
                {
                    _fileBackingStore.Flush();
                    _fileBackingStore.Dispose();
                    _fileBackingStore = null;
                }
            }

            _memoryBackingStore = null;
            _fileBackingStore = null;

            base.Dispose(disposing);
        }
        #endregion

        #region Private Methods
        private void CheckNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private void EnsureStream()
        {
            if (_memoryBackingStore != null || _fileBackingStore != null)
            {
                UpgradeStreamIfNeeded();
            }
            else
            {
                if (_memoryFlag == VirtualStreamMemoryFlag.OnlyInMemory ||
                    _memoryFlag == VirtualStreamMemoryFlag.AutoOverflowToDisk)
                {
                    _memoryBackingStore = new MemoryStream();
                }
                else
                {
                    _fileBackingStore =
                        new FileStream(
                            Path.GetTempFileName(),
                            FileMode.Open,
                            FileAccess.ReadWrite,
                            FileShare.None,
                            _bufferSize,
                            FileOptions.DeleteOnClose);
                }
            }
        }

        private void UpgradeStreamIfNeeded(bool force = false)
        {
            if (_memoryFlag == VirtualStreamMemoryFlag.AutoOverflowToDisk &&
                _memoryBackingStore != null &&
                _memoryBackingStore.Length > _thresholdSize)
            {
                // Create file-based storage
                _fileBackingStore =
                    new FileStream(
                        Path.GetTempFileName(),
                        FileMode.Open,
                        FileAccess.ReadWrite,
                        FileShare.None,
                        _bufferSize,
                        FileOptions.DeleteOnClose);

                // We need to be in same position in file-based stream as we
                //	are currently positioned in the memory-based stream.
                var cachedPosition = _memoryBackingStore.Position;

                // Copy the memory stream to file-based stream (may throw)
                _memoryBackingStore.Seek(0, SeekOrigin.Begin);
                _memoryBackingStore.CopyTo(_fileBackingStore, _bufferSize);
                _fileBackingStore.Flush();

                // Discard memory backing store and invalidate object
                _memoryBackingStore.Dispose();
                _memoryBackingStore = null;

                // Restore position in file-based stream
                _fileBackingStore.Position = cachedPosition;
            }
        } 
        #endregion
    }

    /// <summary>
    /// </summary>
    public enum VirtualStreamMemoryFlag
    {
        /// <summary>
        /// Use in-memory store and promote to disk if memory threshold is exceeded.
        /// </summary>
        AutoOverflowToDisk = 0,

        /// <summary>
        /// Use in-memory store only.
        /// </summary>
        OnlyInMemory = 1,

        /// <summary>
        /// Use on-disk store only.
        /// </summary>
        OnlyOnDisk = 2
    }
}