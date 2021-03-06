using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Serilog;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <b>Page</b> object represents an abstract page with header and data sections.
    /// </summary>
    /// <remarks>
    /// Pages are separated into a header section and a data section.
    /// In the base class the header only contains a single byte used to
    /// track status bits for the page.
    /// </remarks>
    public abstract class Page : IPage
    {
        #region Private Fields
        private static readonly ILogger Logger = Serilog.Log.ForContext<Page>();

        private ILifetimeScope _lifetimeScope;
        private readonly BufferFieldBitVector32 _status;
        private BitVector32.Section _pageType;
        private bool _headerDirty;
        private bool _dataDirty;
        private bool _disposed;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Page"/> class.
        /// </summary>
        protected Page()
        {
            _status = new BufferFieldBitVector32();

            CreateStatus(0);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the minimum number of bytes required for the header block.
        /// </summary>
        public virtual uint MinHeaderSize => 8;

        /// <summary>
        /// Gets the header block size.
        /// </summary>
        public virtual uint HeaderSize => MinHeaderSize;

        /// <summary>
        /// Gets the page size.
        /// </summary>
        public virtual uint PageSize => 1024;

        /// <summary>
        /// Gets the data area size.
        /// </summary>
        public uint DataSize => PageSize - HeaderSize;

        /// <summary>
        /// Gets/sets the page type.
        /// </summary>
        public PageType PageType
        {
            get => (PageType)_status.Value[_pageType];
            protected set
            {
                CheckReadOnly();
                _status.SetValue(_pageType, (int)value);
                SetHeaderDirty();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this page is attached to a new
        /// <see cref="T:BufferBase"/> object.
        /// </summary>
        public abstract bool IsNewPage
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets/sets the readonly page state.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets a value indicating whether this page is dirty.
        /// Note: This does not check the underlying _buffer.
        /// </summary>
        public bool IsDirty => _headerDirty | _dataDirty;

        /// <summary>
        /// Gets/sets a value indicating whether the data section of a page
        /// is managed by the persistence logic. Default: true.
        /// </summary>
        public bool IsManagedData { get; set; } = true;

        /// <summary>
        /// Gets/sets the virtual page ID.
        /// </summary>
        public virtual VirtualPageId VirtualPageId { get; set; }
        #endregion

        #region Internal Properties
        /// <summary>
        /// Suppresses calls to <see cref="M:SetHeaderDirty"/> and
        /// <see cref="M:SetDataDirty"/> methods.
        /// </summary>
        internal bool SuppressDirty { get; set; } = true;
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the first header field.
        /// </summary>
        /// <value>
        /// The first header field.
        /// </value>
        protected virtual BufferField FirstHeaderField => _status;

        /// <summary>
        /// Gets the last header field.
        /// </summary>
        /// <value>
        /// The last header field.
        /// </value>
        protected virtual BufferField LastHeaderField => _status;
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Saves this instance.
        /// </summary>
        /// <remarks>
        /// This method save all page changes to the underlying PageBuffer and
        /// update the page timestamp before marking the page as clean.
        /// </remarks>
        public void Save()
        {
            if (_headerDirty || _dataDirty)
            {
                // Inform derived classes of PreSave
                OnPreSave();

                // Inform derived classes of Save
                OnPostSave();

                // Mark page as clean
                _headerDirty = false;
                _dataDirty = false;
            }
        }

        /// <summary>
        /// Creates the data stream.
        /// </summary>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <returns></returns>
        public abstract Stream CreateDataStream(bool readOnly);

        /// <summary>
        /// Sets the lifetime scope.
        /// </summary>
        /// <param name="scope">The scope.</param>
        public void SetLifetimeScope(ILifetimeScope scope)
        {
            _lifetimeScope = scope.BeginLifetimeScope(
                builder =>
                {
                    builder.RegisterInstance(this).As(GetType(), typeof(Page));
                });
        }

        /// <summary>
        /// Raises the PreInit event.
        /// </summary>
        /// <remarks>
        /// This method supports internal infrastructure and is not designed to be called directly.
        /// </remarks>
        public void PreInitInternal()
        {
            OnPreInitAsync();
        }

        /// <summary>
        /// Raises the Init event.
        /// </summary>
        /// <remarks>
        /// This method supports internal infrastructure and is not designed to be called directly.
        /// </remarks>
        public void OnInitInternal()
        {
            OnInitAsync();

            // Initialised page must be read/write
            ReadOnly = false;

            // Ensure page is marked as dirty
            SuppressDirty = false;
            _headerDirty = _dataDirty = false;
        }

        /// <summary>
        /// Raises the PreLoad event.
        /// </summary>
        /// <remarks>
        /// This method supports internal infrastructure and is not designed to be called directly.
        /// </remarks>
        public void PreLoadInternal()
        {
            OnPreLoadAsync();
        }

        /// <summary>
        /// Raises the PostLoad event.
        /// </summary>
        /// <remarks>
        /// This method supports internal infrastructure and is not designed to be called directly.
        /// </remarks>
        public void PostLoadInternal()
        {
            ReadHeader();
            if (IsManagedData)
            {
                ReadData();
            }

            SuppressDirty = false;
            _headerDirty = _dataDirty = false;
            OnPostLoadAsync();
        }

        /// <summary>
        /// Sets the state of both header and data sections as dirty.
        /// </summary>
        public void SetDirty()
        {
            SetDirtyCore(true, true);
        }

        /// <summary>
        /// Sets the state of the header section as dirty.
        /// </summary>
        public void SetHeaderDirty()
        {
            SetDirtyCore(true, false);
        }

        /// <summary>
        /// Sets the state of the data section as dirty.
        /// </summary>
        public void SetDataDirty()
        {
            SetDirtyCore(false, true);
        }

        /// <summary>
        /// Checks whether the page is read-only and throws an exception if it is.
        /// </summary>
        /// <exception cref="PageReadOnlyException">Thrown if the page is read-only.</exception>
        public void CheckReadOnly()
        {
            if (ReadOnly)
            {
                throw new PageReadOnlyException(this);
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Checks whether this instance has been disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">
        /// Thrown if this object has been disposed.
        /// </exception>
        protected void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        /// <summary>
        /// Releases managed resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _lifetimeScope?.Dispose();
            }

            // Disconnect from site
            _lifetimeScope = null;
            _disposed = true;
        }

        /// <summary>
        /// Reads the page header block from the underlying buffer.
        /// </summary>
        protected void ReadHeader()
        {
            using (var stream = CreateHeaderStream(true))
            {
                using (var streamManager = new SwitchingBinaryReader(stream, true))
                {
                    ReadHeader(streamManager);
                    streamManager.Close();
                }
                _headerDirty = false;
            }
        }

        /// <summary>
        /// Writes the page header block to the underlying buffer.
        /// </summary>
        protected void WriteHeader()
        {
            using (var stream = CreateHeaderStream(false))
            {
                using (var streamManager = new SwitchingBinaryWriter(stream, true))
                {
                    WriteHeader(streamManager);
                    streamManager.Close();
                }
                _headerDirty = false;
            }
        }

        /// <summary>
        /// Creates the header stream.
        /// </summary>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <returns></returns>
        protected abstract Stream CreateHeaderStream(bool readOnly);

        /// <summary>
        /// Writes the page header block to the specified buffer writer.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected virtual void WriteHeader(SwitchingBinaryWriter streamManager)
        {
            FirstHeaderField.Write(streamManager);
        }

        /// <summary>
        /// Reads the page header block from the specified buffer reader.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected virtual void ReadHeader(SwitchingBinaryReader streamManager)
        {
            FirstHeaderField.Read(streamManager);
        }

        /// <summary>
        /// Writes the page data block to the specified buffer writer.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected virtual void WriteData(SwitchingBinaryWriter streamManager)
        {
        }

        /// <summary>
        /// Reads the page data block from the specified buffer reader.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected virtual void ReadData(SwitchingBinaryReader streamManager)
        {
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <typeparam name="T">
        /// An object that specifies the type of service object to get.
        /// </typeparam>
        /// <returns>
        /// A service object of type serviceType.-or- null if there is no service object of type serviceType.
        /// </returns>
        protected T GetService<T>()
        {
            return _lifetimeScope.Resolve<T>();
        }

        /// <summary>
        /// OnPreInit is called by the system before a page is initialised.
        /// </summary>
        protected virtual Task OnPreInitAsync()
        {
            // Sanity check
            System.Diagnostics.Debug.Assert(HeaderSize >= MinHeaderSize);
            return Task.FromResult(true);
        }

        /// <summary>
        /// OnInit is called by the system when a page is initialised.
        /// </summary>
        protected virtual Task OnInitAsync()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// OnPreLoad is called by the system before data has been loaded.
        /// </summary>
        protected virtual Task OnPreLoadAsync()
        {
            // Sanity check
            System.Diagnostics.Debug.Assert(HeaderSize >= MinHeaderSize);
            return Task.FromResult(true);
        }

        /// <summary>
        /// OnPostLoad is called by the system after data has been loaded.
        /// </summary>
        protected virtual Task OnPostLoadAsync()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Performs operations prior to saving this page.
        /// </summary>
        /// <remarks>
        /// If the header and/or data sections are dirty then they will be rewritten.
        /// </remarks>
        protected virtual void OnPreSave()
        {
            if (_headerDirty)
            {
                WriteHeader();
            }
            if (_dataDirty && IsManagedData)
            {
                WriteData();
            }
        }

        /// <summary>
        /// OnPostSave is called by the system after data has been saved.
        /// </summary>
        protected virtual void OnPostSave()
        {
        }

        /// <summary>
        /// OnDirty is called by the system when the page becomes dirty
        /// </summary>
        protected virtual void OnDirty()
        {
        }

        /// <summary>
        /// Creates the status sections.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="previousSection">The previous section.</param>
        /// <returns></returns>
        protected virtual BitVector32.Section CreateStatusSections(BitVector32 status, BitVector32.Section previousSection)
        {
            return previousSection;
        }
        #endregion

        #region Private Methods
        private void ReadData()
        {
            using (var stream = CreateDataStream(true))
            {
                using (var streamManager = new SwitchingBinaryReader(stream, true))
                {
                    ReadData(streamManager);
                    streamManager.Close();
                }
                _dataDirty = false;
            }
        }

        private void WriteData()
        {
            using (var stream = CreateDataStream(false))
            {
                using (var streamManager = new SwitchingBinaryWriter(stream, true))
                {
                    WriteData(streamManager);
                    streamManager.Close();
                }
                _dataDirty = false;
            }
        }

        private void CreateStatus(int value)
        {
            _status.Value = new BitVector32(value);
            _pageType = BitVector32.CreateSection((short)PageType.Root);
            CreateStatusSections(_status.Value, _pageType);
        }

        private void SetDirtyCore(bool headerDirty, bool dataDirty)
        {
            if (!SuppressDirty)
            {
                var alreadyDirty = _headerDirty | _dataDirty;
                var canFire = false;
                if (headerDirty && !_headerDirty)
                {
                    _headerDirty = true;
                    canFire = true;
                }
                if (dataDirty && !_dataDirty)
                {
                    _dataDirty = true;
                    canFire = true;
                }
                if (!alreadyDirty && canFire)
                {
                    OnDirty();
                }
            }
        }
        #endregion
    }
}
