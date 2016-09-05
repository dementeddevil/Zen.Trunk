using System;
using System.IO;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.IO
{
    [CLSCompliant(false)]
    public class SingleBufferDevice : BufferDevice, ISingleBufferDevice
    {
        #region Private Fields
        private readonly IVirtualBufferFactory _bufferFactory;
        private FileStream _fileStream;
        private AdvancedFileStream _scatterGatherStream;
        private ScatterGatherReaderWriter _scatterGatherHelper;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleBufferDevice" /> class.
        /// </summary>
        /// <param name="bufferFactory">The buffer factory.</param>
        /// <param name="name">The device name.</param>
        /// <param name="pathName">The location of the physical file.</param>
        /// <param name="createPageCount">The create page count.</param>
        /// <param name="enableScatterGatherIo">if set to <c>true</c> then scatter-gather I/O will be enabled.</param>
        /// <remarks>
        /// If the createPageCount > 0 then the underlying file will be created during the open call
        /// and initialised to a length equal to the buffer size reported by the buffer factory
        /// multiplied by the createPageCount value.
        /// </remarks>
        public SingleBufferDevice(
            IVirtualBufferFactory bufferFactory,
            string name,
            string pathName,
            uint createPageCount,
            bool enableScatterGatherIo)
        {
            _bufferFactory = bufferFactory;
            Name = name;
            PathName = pathName;
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
        /// Gets or sets the name of the path.
        /// </summary>
        /// <value>The name of the path.</value>
        public string PathName
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
        protected bool IsScatterGatherIoEnabled
        {
            get;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance requires create.
        /// </summary>
        /// <value><c>true</c> if [requires create]; otherwise, <c>false</c>.</value>
        protected bool RequiresCreate
        {
            get;
        }
        #endregion

        #region Public Methods
        public async Task LoadBufferAsync(uint physicalPageId, IVirtualBuffer buffer)
        {
            if (IsScatterGatherIoEnabled)
            {
                await _scatterGatherHelper
                    .ReadBufferAsync(physicalPageId, buffer)
                    .ConfigureAwait(false);
            }
            else
            {
                Task<int> task;
                var rawBuffer = new byte[_bufferFactory.BufferSize];
                lock (_fileStream)
                {
                    _fileStream.Seek(physicalPageId * _bufferFactory.BufferSize, SeekOrigin.Begin);
                    task = _fileStream.ReadAsync(rawBuffer, 0, _bufferFactory.BufferSize);
                }
                await task.ConfigureAwait(false);
                buffer.InitFrom(rawBuffer);
            }
        }

        public async Task SaveBufferAsync(uint physicalPageId, IVirtualBuffer buffer)
        {
            if (IsScatterGatherIoEnabled)
            {
                await _scatterGatherHelper
                    .WriteBufferAsync(physicalPageId, buffer)
                    .ConfigureAwait(false);
            }
            else
            {
                Task task;
                var rawBuffer = new byte[_bufferFactory.BufferSize];
                buffer.CopyTo(rawBuffer);
                lock (_fileStream)
                {
                    _fileStream.Seek(physicalPageId * _bufferFactory.BufferSize, SeekOrigin.Begin);
                    task = _fileStream.WriteAsync(rawBuffer, 0, _bufferFactory.BufferSize);
                }
                await task.ConfigureAwait(false);
                buffer.InitFrom(rawBuffer);
            }
        }

        public async Task FlushBuffersAsync(bool flushReads, bool flushWrites)
        {
            if (IsScatterGatherIoEnabled)
            {
                await _scatterGatherHelper
                    .Flush(flushReads, flushWrites)
                    .ConfigureAwait(false);
            }
            else
            {
                _fileStream.Flush();
            }
        }

        public uint ExpandDevice(int pageCount)
        {
            var oldPageCapacity = PageCount;
            var newPageCapacity = (uint)(oldPageCapacity + pageCount);
            var fileLengthInBytes = _bufferFactory.BufferSize * newPageCapacity;

            if (_fileStream != null)
            {
                _fileStream.SetLength(fileLengthInBytes);
            }
            else if (_scatterGatherStream != null)
            {
                _scatterGatherStream.SetLength(fileLengthInBytes);
            }

            PageCount = newPageCapacity;
            return newPageCapacity;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (_scatterGatherHelper != null)
            {
                _scatterGatherHelper.Dispose();
                _scatterGatherHelper = null;
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

        protected override Task OnOpen()
        {
            if (IsScatterGatherIoEnabled)
            {
                // Create the stream object
                _scatterGatherStream = new AdvancedFileStream(
                    PathName,
                    RequiresCreate ? FileMode.CreateNew : FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    _bufferFactory.BufferSize,
                    FileOptions.Asynchronous |
                    FileOptions.RandomAccess |
                    FileOptions.WriteThrough,
                    true);
                _scatterGatherHelper = new ScatterGatherReaderWriter(_scatterGatherStream);

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
                    PathName,
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

        protected override async Task OnClose()
        {
            if (IsScatterGatherIoEnabled)
            {
                await _scatterGatherHelper.Flush().ConfigureAwait(false);
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
