using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Zen.Trunk.IO;
using Zen.Trunk.Logging;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
	/// <summary>
	/// <b>Page</b> objects represents a 64Kb buffer block.
	/// </summary>
	/// <remarks>
	/// Pages are seperated into a header section and a data section.
	/// In the base class the header only contains a single byte used to
	/// track status bits for the page.
	/// </remarks>
	public abstract class Page : IDisposable
	{
		#region Internal Objects
		private class NewPageInterceptorField : BufferField
		{
			private readonly Page _owner;

			public NewPageInterceptorField(BufferField prev, Page owner)
				: base(prev)
			{
				_owner = owner;
			}

			public override ushort MaxElements => 0;

		    public override int DataSize => 0;

		    public override int FieldLength => 0;

		    protected override bool CanContinue(bool isReading)
			{
				if (isReading && _owner.PageType == PageType.New)
				{
					_owner.IsNewPage = true;
					return false;
				}
				return true;
			}

			protected override void OnRead(SwitchingBinaryReader reader)
			{
			}

			protected override void OnWrite(SwitchingBinaryWriter writer)
			{
			}
		}
		#endregion

		#region Private Fields
	    private static readonly ILog Logger = LogProvider.For<Page>();

		private static readonly object InitEvent = new object();
		private static readonly object LoadEvent = new object();
		private static readonly object SaveEvent = new object();
		private static readonly object DirtyEvent = new object();
		private static readonly object DisposedEvent = new object();
		private EventHandlerList _events;

	    private ILifetimeScope _lifetimeScope;

	    private readonly BufferFieldBitVector32 _status;
		private readonly NewPageInterceptorField _newPageField;
		private BitVector32.Section _pageType;
		private bool _managedData = true;
		private bool _headerDirty;
		private bool _dataDirty;
	    private bool _disposed;
		#endregion

		#region Public Events
		/// <summary>
		/// Occurs when [init].
		/// </summary>
		public event EventHandler InitNew
		{
			add
			{
				Events.AddHandler(InitEvent, value);
			}
			remove
			{
				Events.RemoveHandler(InitEvent, value);
			}
		}

		/// <summary>
		/// Occurs when [load].
		/// </summary>
		public event EventHandler LoadExisting
		{
			add
			{
				Events.AddHandler(LoadEvent, value);
			}
			remove
			{
				Events.RemoveHandler(LoadEvent, value);
			}
		}

		/// <summary>
		/// Occurs when [save].
		/// </summary>
		public event EventHandler SaveCompleted
		{
			add
			{
				Events.AddHandler(SaveEvent, value);
			}
			remove
			{
				Events.RemoveHandler(SaveEvent, value);
			}
		}

		/// <summary>
		/// Occurs when the page becomes dirty.
		/// </summary>
		public event EventHandler PageDirty
		{
			add
			{
				Events.AddHandler(DirtyEvent, value);
			}
			remove
			{
				Events.RemoveHandler(DirtyEvent, value);
			}
		}

		/// <summary>
		/// Represents the method that handles the <see cref="E:System.ComponentModel.IComponent.Disposed"/> event of a component.
		/// </summary>
		public event EventHandler Disposed
		{
			add
			{
				Events.AddHandler(DisposedEvent, value);
			}
			remove
			{
				Events.RemoveHandler(DisposedEvent, value);
			}
		}
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Page"/> class.
		/// </summary>
		protected Page()
		{
		    if (Logger.IsDebugEnabled())
		    {
		        Logger.Debug($"{GetType().Name} ctor");
		    }

			_status = new BufferFieldBitVector32();
			_newPageField = new NewPageInterceptorField(_status, this);

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
		public virtual PageType PageType
		{
			get
			{
				return (PageType)_status.Value[_pageType];
			}
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
		public bool IsManagedData
		{
			get
			{
				return _managedData;
			}
			set
			{
				_managedData = value;
			}
		}

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
		/// Gets the event handler list for this object.
		/// </summary>
		protected EventHandlerList Events
		{
			get
			{
			    CheckDisposed();
			    return _events ?? (_events = new EventHandlerList());
			}
		}

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
        protected virtual BufferField LastHeaderField => _newPageField;
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
				OnPreSave(EventArgs.Empty);

				// Inform derived classes of Save
				OnPostSave(EventArgs.Empty);

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
        #endregion

        #region Internal Methods
        internal void PreInitInternal()
		{
			OnPreInitAsync(EventArgs.Empty);
		}

		internal void OnInitInternal()
		{
			OnInitAsync(EventArgs.Empty);

			// Initialised page must be read/write
			ReadOnly = false;

			// Ensure page is marked as dirty
			SuppressDirty = false;
			_headerDirty = _dataDirty = false;
			//OnDirty(EventArgs.Empty);
		}

		internal void PreLoadInternal()
		{
			OnPreLoadAsync(EventArgs.Empty);
		}

		internal void PostLoadInternal()
		{
			ReadHeader();
			if (_managedData)
			{
				ReadData();
			}

			SuppressDirty = false;
			_headerDirty = _dataDirty = false;
			OnPostLoadAsync(EventArgs.Empty);
		}

		internal void SetDirty()
		{
			SetDirtyCore(true, true);
		}

		internal void SetHeaderDirty()
		{
			SetDirtyCore(true, false);
		}

		internal void SetDataDirty()
		{
			SetDirtyCore(false, true);
		}

		internal void CheckReadOnly()
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
				if (_events != null)
				{
                    // Notify objects that we are disappearing...
                    ((EventHandler)Events[DisposedEvent])?.Invoke(this, EventArgs.Empty);

                    // Dispose of the event handler list
                    _events.Dispose();
				}

			    _lifetimeScope?.Dispose();
			}

			// Disconnect from site
		    _lifetimeScope = null;
			_events = null;
			_disposed = true;
		}

		/// <summary>
		/// Reads the page header block from the underlying buffer.
		/// </summary>
		protected void ReadHeader()
		{
			using (var stream = CreateHeaderStream(true))
			{
				using (var streamManager = new SwitchingBinaryReader(stream))
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
				using (var streamManager = new SwitchingBinaryWriter(stream))
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
		/// Performs operations on this instance prior to being initialised.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected virtual Task OnPreInitAsync(EventArgs e)
		{
			// Sanity check
			System.Diagnostics.Debug.Assert(HeaderSize >= MinHeaderSize);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Raises the <see cref="E:Init"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual Task OnInitAsync(EventArgs e)
		{
            ((EventHandler)Events[InitEvent])?.Invoke(this, e);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Performs operations on this instance prior to being loaded.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual Task OnPreLoadAsync(EventArgs e)
		{
			// Sanity check
			System.Diagnostics.Debug.Assert(HeaderSize >= MinHeaderSize);
		    return Task.FromResult(true);
		}

		/// <summary>
		/// Raises the <see cref="E:Load"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected virtual Task OnPostLoadAsync(EventArgs e)
		{
            ((EventHandler)Events[LoadEvent])?.Invoke(this, e);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Performs operations prior to saving this page.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// If the header and/or data sections are dirty then they will be rewritten.
        /// </remarks>
        protected virtual void OnPreSave(EventArgs e)
		{
			if (_headerDirty)
			{
				WriteHeader();
			}
			if (_dataDirty && _managedData)
			{
				WriteData();
			}
		}

		/// <summary>
		/// Raises the <see cref="E:Save"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected virtual void OnPostSave(EventArgs e)
		{
            ((EventHandler)Events[SaveEvent])?.Invoke(this, e);
		}

		/// <summary>
		/// Raises the <see cref="E:Dirty"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected virtual void OnDirty(EventArgs e)
		{
            ((EventHandler)Events[DirtyEvent])?.Invoke(this, e);
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
                using (var streamManager = new SwitchingBinaryReader(stream))
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
                using (var streamManager = new SwitchingBinaryWriter(stream))
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
			_pageType = BitVector32.CreateSection((short)PageType.Index);
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
					OnDirty(EventArgs.Empty);
				}
			}
		}
		#endregion
	}
}
