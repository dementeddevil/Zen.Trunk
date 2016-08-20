namespace Zen.Trunk.Storage
{
	using System;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.IO;
	using Zen.Trunk.Storage.IO;

	/// <summary>
	/// <b>Page</b> objects represents a 64Kb buffer block.
	/// </summary>
	/// <remarks>
	/// Pages are seperated into a header section and a data section.
	/// In the base class the header only contains a single byte used to
	/// track status bits for the page.
	/// </remarks>
	public abstract class Page : TraceableObject, IServiceProvider, IDisposable, IComponent
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

			protected override void OnRead(BufferReaderWriter streamManager)
			{
			}

			protected override void OnWrite(BufferReaderWriter streamManager)
			{
			}
		}
		#endregion

		#region Private Fields
		private static readonly object InitEvent = new object();
		private static readonly object LoadEvent = new object();
		private static readonly object SaveEvent = new object();
		private static readonly object DirtyEvent = new object();
		private static readonly object DisposedEvent = new object();
		private EventHandlerList _events;

		private ISite _site;

		private ulong _virtualId;
		private readonly BufferFieldBitVector32 _status;
		private readonly NewPageInterceptorField _newPageField;
		private BitVector32.Section pageType;
		private BitVector32.Section indexType;
		private bool _managedData = true;
		private bool _headerDirty;
		private bool _dataDirty;
		private bool _readOnly = true;
		private bool _suppressDirty = true;
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

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Page"/> class.
		/// </summary>
		public Page()
		{
			Tracer.WriteVerboseLine("{0}.ctor", GetType().Name);

			_status = new BufferFieldBitVector32();
			_newPageField = new NewPageInterceptorField(_status, this);

			CreateStatus(0);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the <see cref="T:System.ComponentModel.ISite"/> 
		/// associated with the <see cref="T:Page"/>.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The <see cref="T:System.ComponentModel.ISite"/> object associated 
		/// with the page; or null, if the page does not have a site.
		/// </returns>
		public ISite Site
		{
			get
			{
				return _site;
			}
			set
			{
				_site = value;
			}
		}

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
				return (PageType)_status.Value[pageType];
			}
			set
			{
				CheckReadOnly();
				_status.SetValue(pageType, (int)value);
				SetHeaderDirty();
			}
		}

		/// <summary>
		/// Gets or sets the type of the index.
		/// </summary>
		/// <value>The type of the index.</value>
		public IndexType IndexType
		{
			get
			{
				if (this.PageType != PageType.Index)
				{
					throw new InvalidOperationException("Not valid for non-index pages.");
				}
				return (IndexType)_status.Value[indexType];
			}
			set
			{
				CheckReadOnly();
				if (this.PageType != PageType.Index)
				{
					throw new InvalidOperationException("Not valid for non-index pages.");
				}
				_status.SetValue(indexType, (int)value);
				SetHeaderDirty();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is a root
		/// index page.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if this instance is root index; otherwise,
		/// <see langword="false"/>.
		/// </value>
		public bool IsRootIndex
		{
			get
			{
				return (IndexType & IndexType.Root) != 0;
			}
			set
			{
				if (value)
				{
					IndexType |= IndexType.Root;
				}
				else
				{
					IndexType &= ~IndexType.Root;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is an 
		/// intermediate index page.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if this instance is root index; otherwise,
		/// <see langword="false"/>.
		/// </value>
		public bool IsIntermediateIndex
		{
			get
			{
				return (IndexType & IndexType.Intermediate) != 0;
			}
			set
			{
				if (value)
				{
					IndexType |= IndexType.Intermediate;
				}
				else
				{
					IndexType &= ~IndexType.Intermediate;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is a root index page.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if this instance is root index; otherwise, <see langword="false"/>.
		/// </value>
		public bool IsLeafIndex
		{
			get
			{
				return (IndexType & IndexType.Leaf) != 0;
			}
			set
			{
				if (value)
				{
					IndexType |= IndexType.Leaf;
				}
				else
				{
					IndexType &= ~IndexType.Leaf;
				}
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
		public virtual bool ReadOnly
		{
			get
			{
				return _readOnly;
			}
			set
			{
				if (_readOnly != value)
				{
					_readOnly = value;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether this page is dirty.
		/// Note: This does not check the underlying _buffer.
		/// </summary>
		public bool IsDirty => _headerDirty | _dataDirty;

	    /// <summary>
		/// Gets a value indicating whether this is the root database page.
		/// </summary>
		public virtual bool IsRootPage => false;

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
		public virtual ulong VirtualId
		{
			get
			{
				return _virtualId;
			}
			set
			{
				_virtualId = value;
			}
		}
		#endregion

		#region Internal Properties
		/// <summary>
		/// Suppresses calls to <see cref="M:SetHeaderDirty"/> and
		/// <see cref="M:SetDataDirty"/> methods.
		/// </summary>
		internal bool SuppressDirty
		{
			get
			{
				return _suppressDirty;
			}
			set
			{
				_suppressDirty = value;
			}
		}
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
				if (_events == null)
				{
					_events = new EventHandlerList();
				}
				return _events;
			}
		}

		protected override string TracerName => GetType().Name;

	    protected virtual BufferField FirstHeaderField => _status;

	    protected virtual BufferField LastHeaderField => _newPageField;

	    #endregion

		#region Public Methods
		/// <summary>
		/// Performs application-defined tasks associated with freeing, 
		/// releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			DisposeManagedObjects();
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
		#endregion

		#region Internal Methods
		internal void PreInitInternal()
		{
			OnPreInit(EventArgs.Empty);
		}

		internal void OnInitInternal()
		{
			OnInit(EventArgs.Empty);

			// Initialised page must be read/write
			ReadOnly = false;

			// Ensure page is marked as dirty
			_suppressDirty = false;
			_headerDirty = _dataDirty = false;
			//OnDirty(EventArgs.Empty);
		}

		internal void PreLoadInternal()
		{
			OnPreLoad(EventArgs.Empty);
		}

		internal void PostLoadInternal()
		{
			ReadHeader();
			if (_managedData)
			{
				ReadData();
			}

			_suppressDirty = false;
			_headerDirty = _dataDirty = false;
			OnPostLoad(EventArgs.Empty);
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
		protected virtual void DisposeManagedObjects()
		{
			if (!_disposed)
			{
				if (_events != null)
				{
					// Notify objects that we are disappearing...
					var handler = (EventHandler)Events[DisposedEvent];
					if (handler != null)
					{
						handler(this, EventArgs.Empty);
					}

					// Dispose of the event handler list
					_events.Dispose();
					_events = null;
				}

				_disposed = true;
			}

			// Disconnect from site
			_site = null;
		}

		/// <summary>
		/// Creates the tracer.
		/// </summary>
		/// <param name="tracerName">Name of the tracer.</param>
		/// <returns></returns>
		protected override ITracer CreateTracer(string tracerName)
		{
			return TS.CreatePageBufferTracer(tracerName);
		}

		/// <summary>
		/// Reads the page header block from the underlying buffer.
		/// </summary>
		protected void ReadHeader()
		{
			using (var stream = CreateHeaderStream(true))
			{
				using (var streamManager = new BufferReaderWriter(stream))
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
				using (var streamManager = new BufferReaderWriter(stream))
				{
					WriteHeader(streamManager);
					streamManager.Close();
				}
				_headerDirty = false;
			}
		}

		/// <summary>
		/// Reads the page data block from the underlying buffer.
		/// </summary>
		protected void ReadData()
		{
			using (var stream = CreateDataStream(true))
			{
				using (var streamManager = new BufferReaderWriter(stream))
				{
					ReadData(streamManager);
					streamManager.Close();
				}
				_dataDirty = false;
			}
		}

		/// <summary>
		/// Writes the page data block to the underlying storage.
		/// </summary>
		protected void WriteData()
		{
			using (var stream = CreateDataStream(false))
			{
				using (var streamManager = new BufferReaderWriter(stream))
				{
					WriteData(streamManager);
					streamManager.Close();
				}
				_dataDirty = false;
			}
		}

		/// <summary>
		/// Creates the header stream.
		/// </summary>
		/// <param name="readOnly">if set to <c>true</c> [read only].</param>
		/// <returns></returns>
		protected abstract Stream CreateHeaderStream(bool readOnly);

		/// <summary>
		/// Creates the data stream.
		/// </summary>
		/// <param name="readOnly">if set to <c>true</c> [read only].</param>
		/// <returns></returns>
		public abstract Stream CreateDataStream(bool readOnly);

		/// <summary>
		/// Writes the page header block to the specified buffer writer.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected virtual void WriteHeader(BufferReaderWriter streamManager)
		{
			FirstHeaderField.Write(streamManager);
		}

		/// <summary>
		/// Reads the page header block from the specified buffer reader.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected virtual void ReadHeader(BufferReaderWriter streamManager)
		{
			FirstHeaderField.Read(streamManager);
		}

		/// <summary>
		/// Writes the page data block to the specified buffer writer.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected virtual void WriteData(BufferReaderWriter streamManager)
		{
		}

		/// <summary>
		/// Reads the page data block from the specified buffer reader.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected virtual void ReadData(BufferReaderWriter streamManager)
		{
		}

		/// <summary>
		/// Gets the service object of the specified type.
		/// </summary>
		/// <param name="serviceType">An object that specifies the type of service object to get.</param>
		/// <returns>
		/// A service object of type serviceType.-or- null if there is no service object of type serviceType.
		/// </returns>
		protected virtual object GetService(Type serviceType)
		{
			// Respond to MetaPage requests
			if (serviceType == typeof(Page))
			{
				return this;
			}

			// Pass to our site if we have one
			if (_site != null)
			{
				return _site.GetService(serviceType);
			}
			else
			{
				return null;
			}
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
			return (T)GetService(typeof(T));
		}

		/// <summary>
		/// Performs operations on this instance prior to being initialised.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected virtual void OnPreInit(EventArgs e)
		{
			// Sanity check
			System.Diagnostics.Debug.Assert(HeaderSize >= MinHeaderSize);
		}

		/// <summary>
		/// Raises the <see cref="E:Init"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected virtual void OnInit(EventArgs e)
		{
			var handler = (EventHandler)Events[InitEvent];
			if (handler != null)
			{
				handler(this, e);
			}
		}

		/// <summary>
		/// Performs operations on this instance prior to being loaded.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected virtual void OnPreLoad(EventArgs e)
		{
			// Sanity check
			System.Diagnostics.Debug.Assert(HeaderSize >= MinHeaderSize);
		}

		/// <summary>
		/// Raises the <see cref="E:Load"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected virtual void OnPostLoad(EventArgs e)
		{
			var handler = (EventHandler)Events[LoadEvent];
			if (handler != null)
			{
				handler(this, e);
			}
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
			var handler = (EventHandler)Events[SaveEvent];
			if (handler != null)
			{
				handler(this, e);
			}
		}

		/// <summary>
		/// Raises the <see cref="E:Dirty"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected virtual void OnDirty(EventArgs e)
		{
			var handler = (EventHandler)Events[DirtyEvent];
			if (handler != null)
			{
				handler(this, e);
			}
		}
		#endregion

		#region Private Methods
		private void CreateStatus(int value)
		{
			_status.Value = new BitVector32(value);
			pageType = BitVector32.CreateSection((short)PageType.Index);
			indexType = BitVector32.CreateSection((short)IndexType.Leaf, pageType);
		}

		private void SetDirtyCore(bool headerDirty, bool dataDirty)
		{
			if (!_suppressDirty)
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

		#region IServiceProvider Members
		object IServiceProvider.GetService(Type serviceType)
		{
			return ((Page)this).GetService(serviceType);
		}
		#endregion
	}
}
