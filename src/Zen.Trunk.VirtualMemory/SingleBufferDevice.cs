using System;
using System.IO;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="BufferDevice" />
    /// <seealso cref="ISingleBufferDevice" />
    [CLSCompliant(false)]
    public class SingleBufferDevice : BufferDevice, ISingleBufferDevice
    {
        #region Private Fields
        private readonly ISystemClock _systemClock;
        private readonly IVirtualBufferFactory _bufferFactory;
        private FileStream _fileStream;
        private AdvancedFileStream _scatterGatherStream;
        private ScatterGatherRequestQueue _requestQueue;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleBufferDevice" /> class.
        /// </summary>
        /// <param name="systemClock">System reference clock.</param>
        /// <param name="bufferFactory">The buffer factory.</param>
        /// <param name="name">The device name.</param>
        /// <param name="pathname">The location of the physical file.</param>
        /// <param name="createPageCount">The create page count.</param>
        /// <param name="enableScatterGatherIo">
        /// if set to <c>true</c> then scatter-gather I/O will be enabled;
        /// otherwise <c>false</c> and conventional I/O will be used.
        /// </param>
        /// <remarks>
        /// If the <paramref name="createPageCount" /> > 0 then the underlying
        /// file will be created during the open call and initialised to a
        /// length equal to the buffer size reported by the buffer factory
        /// multiplied by the createPageCount value.
        /// </remarks>
        public SingleBufferDevice(
            ISystemClock systemClock,
            IVirtualBufferFactory bufferFactory,
            string name,
            string pathname,
            uint createPageCount,
            bool enableScatterGatherIo)
        {
            _systemClock = systemClock;
            _bufferFactory = bufferFactory;
            Name = name;
            Pathname = pathname;
            RequiresCreate = createPageCount > 0;
            PageCount = createPageCount;
            IsScatterGatherIoEnabled = enableScatterGatherIo;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the buffer factory.
        /// </summary>
        /// <value>
        /// The buffer factory.
        /// </value>
        public override IVirtualBufferFactory BufferFactory => _bufferFactory;

        /// <summary>
        /// Gets the page count.
        /// </summary>
        /// <value>
        /// The page count.
        /// </value>
        public uint PageCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get;
        }

        /// <summary>
        /// Gets the pathname of the underlying file.
        /// </summary>
        /// <value>
        /// The pathname.
        /// </value>
        public string Pathname
        {
            get;
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets or sets a value indicating this instance has scatter/gather I/O enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has scatter/gather I/O enabled; otherwise, <c>false</c>.
        /// </value>
        protected bool IsScatterGatherIoEnabled { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance requires create.
        /// </summary>
        /// <value><c>true</c> if [requires create]; otherwise, <c>false</c>.</value>
        protected bool RequiresCreate { get; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Loads the page data from the physical page into the supplied buffer.
        /// </summary>
        /// <param name="pageId">The virtual page identifier.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// When scatter/gather I/O is enabled then the load is deferred until
        /// pending requests are flushed via <see cref="FlushBuffersAsync"/>.
        /// </remarks>
        public override async Task LoadBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer)
        {
            if (IsScatterGatherIoEnabled)
            {
                await _requestQueue
                    .ReadBufferAsync(pageId.PhysicalPageId, buffer)
                    .ConfigureAwait(false);
            }
            else
            {
                Task<int> task;
                var rawBuffer = new byte[_bufferFactory.BufferSize];
                lock (_fileStream)
                {
                    _fileStream.Seek(pageId.PhysicalPageId * _bufferFactory.BufferSize, SeekOrigin.Begin);
                    task = _fileStream.ReadAsync(rawBuffer, 0, _bufferFactory.BufferSize);
                }
                await task.ConfigureAwait(false);
                buffer.InitFrom(rawBuffer);
            }
        }

        /// <summary>
        /// Saves the page data from the supplied buffer to the physical page.
        /// </summary>
        /// <param name="pageId">The virtual page identifier.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// When scatter/gather I/O is enabled then the save is deferred until
        /// pending requests are flushed via <see cref="FlushBuffersAsync"/>.
        /// </remarks>
        public override async Task SaveBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer)
        {
            if (IsScatterGatherIoEnabled)
            {
                await _requestQueue
                    .WriteBufferAsync(pageId.PhysicalPageId, buffer)
                    .ConfigureAwait(false);
            }
            else
            {
                Task task;
                var rawBuffer = new byte[_bufferFactory.BufferSize];
                buffer.CopyTo(rawBuffer);
                lock (_fileStream)
                {
                    _fileStream.Seek(pageId.PhysicalPageId * _bufferFactory.BufferSize, SeekOrigin.Begin);
                    task = _fileStream.WriteAsync(rawBuffer, 0, _bufferFactory.BufferSize);
                }
                await task.ConfigureAwait(false);
                buffer.InitFrom(rawBuffer);
            }
        }

        /// <summary>
        /// Flushes pending buffer operations.
        /// </summary>
        /// <param name="flushReads">
        /// if set to <c>true</c> then read operations are flushed.
        /// </param>
        /// <param name="flushWrites">
        /// if set to <c>true</c> then write operations are flushed.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task FlushBuffersAsync(bool flushReads, bool flushWrites)
        {
            if (IsScatterGatherIoEnabled)
            {
                await _requestQueue
                    .Flush(flushReads, flushWrites)
                    .ConfigureAwait(false);
            }
            else
            {
                _fileStream.Flush();
            }
        }

        /// <summary>
        /// Resizes the device to the specified number of pages.
        /// </summary>
        /// <param name="pageCount">The page count.</param>
        public void Resize(uint pageCount)
        {
            var fileLengthInBytes = _bufferFactory.BufferSize * pageCount;

            if (_fileStream != null)
            {
                _fileStream.SetLength(fileLengthInBytes);
            }
            else
            {
                _scatterGatherStream?.SetLength(fileLengthInBytes);
            }

            PageCount = pageCount;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (_requestQueue != null)
            {
                _requestQueue.Dispose();
                _requestQueue = null;
            }

            if (_scatterGatherStream != null)
            {
                _scatterGatherStream.Dispose();
                _scatterGatherStream = null;
            }

            if (_fileStream != null)
            {
                _fileStream.Dispose();
                _fileStream = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Called when opening the device.
        /// </summary>
        /// <returns></returns>
        protected override Task OnOpenAsync()
        {
            if (IsScatterGatherIoEnabled)
            {
                // Create the stream object
                _scatterGatherStream = new AdvancedFileStream(
                    Pathname,
                    RequiresCreate ? FileMode.CreateNew : FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    _bufferFactory.BufferSize,
                    FileOptions.Asynchronous |
                    FileOptions.RandomAccess |
                    FileOptions.WriteThrough,
                    true);
                _requestQueue = new ScatterGatherRequestQueue(
                    _systemClock,
                    _scatterGatherStream,
                    new ScatterGatherRequestQueueSettings());

                if (RequiresCreate)
                {
                    _scatterGatherStream.SetLength(_bufferFactory.BufferSize * PageCount);
                }
                else
                {
                    PageCount = (uint)(_scatterGatherStream.Length / _bufferFactory.BufferSize);
                }
            }
            else
            {
                _fileStream = new FileStream(
                    Pathname,
                    RequiresCreate ? FileMode.CreateNew : FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    _bufferFactory.BufferSize,
                    FileOptions.Asynchronous |
                    FileOptions.RandomAccess |
                    FileOptions.WriteThrough);

                if (RequiresCreate)
                {
                    _fileStream.SetLength(_bufferFactory.BufferSize * PageCount);
                }
                else
                {
                    PageCount = (uint)(_fileStream.Length / _bufferFactory.BufferSize);
                }
            }
            return CompletedTask.Default;
        }

        /// <summary>
        /// Raises the Close event.
        /// </summary>
        /// <returns></returns>
        protected override async Task OnCloseAsync()
        {
            if (IsScatterGatherIoEnabled)
            {
                await _requestQueue.Flush().ConfigureAwait(false);
                _scatterGatherStream.Flush();
                _scatterGatherStream.Close();
                _scatterGatherStream = null;
            }
            else
            {
                await _fileStream.FlushAsync().ConfigureAwait(false);
                _fileStream.Close();
                _fileStream = null;
            }
        }
        #endregion
    }
}
