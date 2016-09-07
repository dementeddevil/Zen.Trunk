using System;
using System.IO;
using System.Threading.Tasks;
using System.Transactions;
using Zen.Trunk.CoordinationDataStructures;
using Zen.Trunk.Logging;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data
{
	/// <summary>
	/// <b>DataPage</b> object extends <see cref="Page"/> and is the
	/// common base class for all data related page functionality.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class will add the underlying buffer to the transaction if page is
	/// modified and subsequently "saved". However in this case, save, will only
	/// write the modified page fields to the underlying page buffer and NOT to
	/// persistent storage. Writing of the underlying buffer to the persistent 
	/// store is controlled by the transaction logic.
	/// </para>
	/// </remarks>
	public class DataPage : Page
	{
		#region Private Fields
	    private static readonly ILog Logger = LogProvider.For<DataPage>();

	    private PageBuffer _buffer;
		private readonly SpinLockClass _syncTimestamp = new SpinLockClass();
		private readonly BufferFieldInt64 _timestamp;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DataPage"/> class.
		/// </summary>
		public DataPage()
		{
			_timestamp = new BufferFieldInt64(base.LastHeaderField, 0);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Overridden. Gets/sets the virtual page ID.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// The setting of this property is only supported prior to initialising
		/// the underlying buffer object.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the setter is called after the page has been initialised.
		/// </exception>
		public override VirtualPageId VirtualId
		{
			get
			{
				if (_buffer != null)
				{
					return _buffer.PageId;
				}
				return base.VirtualId;
			}
			set
			{
				if (_buffer != null)
				{
					throw new InvalidOperationException("Cannot change virtual ID once buffer has been set.");
				}
				base.VirtualId = value;
			}
		}

		/// <summary>
		/// Overridden. Gets a value indicating whether this page is attached to a new
		/// <see cref="PageBuffer"/> object.
		/// </summary>
		/// <value></value>
		public override bool IsNewPage
		{
			get
			{
				return _buffer.IsNew;
			}
			internal set
			{
				_buffer.IsNew = value;
			}
		}

		/// <summary>
		/// Gets/sets the lock timeout duration.
		/// </summary>
		/// <value>Lock time-span value</value>
		public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets/sets a boolean that indicates whether locks are held until
        /// the current transaction is committed.
        /// </summary>
        /// <value>
        /// A boolean value indicating whether to hold locks.
        /// If <c>true</c> locks are held until the end of the transaction
        /// If <c>false</c> locks are held until the page has been read from storage.
        /// </value>
        public bool HoldLock { get; set; }

		/// <summary>
		/// Overridden. Gets the header size - 192 bytes
		/// </summary>
		public override uint HeaderSize => 192;

	    /// <summary>
		/// Overridden. Gets the page size - 8192 bytes
		/// </summary>
		public override uint PageSize => 8192;

	    /// <summary>
		/// Gets the minimum number of bytes required for the header block.
		/// </summary>
		public override uint MinHeaderSize => base.MinHeaderSize + 8;

	    /// <summary>
		/// Returns the current page timestamp.
		/// </summary>
		public long Timestamp => _timestamp.Value;

	    /// <summary>
		/// Gets/sets the page file-group ID.
		/// </summary>
		public FileGroupId FileGroupId { get; set; }
		#endregion

		#region Internal Properties
		/// <summary>
		/// Gets/sets the data buffer.
		/// </summary>
		/// <value>A <see cref="T:BufferBase"/> object.</value>
		internal PageBuffer DataBuffer
		{
			get
			{
				return _buffer;
			}
			set
			{
				if (_buffer != value)
				{
				    _buffer?.Release();

				    _buffer = value;

				    _buffer?.AddRef();
				}
			}
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets or sets a value indicating whether this page has locking enabled.
		/// </summary>
		/// <value>
		/// <c>true</c> if locking is enabled; otherwise, <c>false</c>.
		/// </value>
		protected bool IsLockingEnabled
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the last header field.
		/// </summary>
		/// <value>The last header field.</value>
		protected override BufferField LastHeaderField => _timestamp;
	    #endregion

		#region Protected Methods
		/// <summary>
		/// Releases managed resources
		/// </summary>
		protected override void DisposeManagedObjects()
		{
			if (IsDirty)
			{
				Save();
			}

			// Release our buffer (if it is dirty then it will be managed
			//	by the transaction logic)
			if (_buffer != null)
			{
				_buffer.Release();
				_buffer = null;
			}

			// Do base class work
			base.DisposeManagedObjects();
		}

        /// <summary>
        /// Creates the header stream.
        /// </summary>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <returns></returns>
        protected override Stream CreateHeaderStream(bool readOnly)
		{
		    if (Logger.IsDebugEnabled())
		    {
		        var readOnlyState = readOnly ? "read-only" : "writeable";
		        Logger.Debug($"CreateHeaderStream as {readOnlyState}");
		    }

			// Return memory stream based on underlying buffer memory
			return _buffer.GetBufferStream(0, (int)HeaderSize, !readOnly);
		}

        /// <summary>
        /// Creates the data stream.
        /// </summary>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <returns></returns>
        public override Stream CreateDataStream(bool readOnly)
		{
            if (Logger.IsDebugEnabled())
            {
                var readOnlyState = readOnly ? "read-only" : "writeable";
                Logger.Debug($"CreateDataStream as {readOnlyState}");
            }

			// Return memory stream based on underlying buffer memory
			return _buffer.GetBufferStream(
				(int)HeaderSize,
				(int)(PageSize - HeaderSize),
				!readOnly);
		}

		/// <summary>
		/// Performs operations on this instance prior to being initialised.
		/// </summary>
		/// <param name="e">
		/// The <see cref="System.EventArgs"/> instance containing the event data.
		/// </param>
		/// <remarks>
		/// Overrides to this method must set their desired lock prior to 
		/// calling the base class.
		/// The base class method will enable the locking primitives and call
		/// LockPage.
		/// This mechanism ensures that all lock states have been set prior to
		/// the first call to LockPage.
		/// </remarks>
		protected override async Task OnPreInitAsync(EventArgs e)
		{
			await base.OnPreInitAsync(e).ConfigureAwait(false);

			// Enable the locking system
			IsLockingEnabled = true;

			// Attempt to acquire lock
			// NOTE: For new pages we always hold the lock until commit
			HoldLock = true;
			await LockPageAsync().ConfigureAwait(false);
		}

		/// <summary>
		/// Overridden. Called by the system prior to loading the page
		/// from persistent storage.
		/// </summary>
		/// <param name="e"></param>
		/// <remarks>
		/// Overrides to this method must set their desired lock prior to 
		/// calling the base class.
		/// The base class method will enable the locking primitives and call
		/// <see cref="LockPageAsync"/> as necessary.
		/// This mechanism ensures that all lock states have been set prior to
		/// the first call to LockPage.
		/// When the current isolation level is uncommitted read then <see cref="LockPageAsync"/>
		/// will not be called.
		/// When the current isolation level is repeatable read or serializable
		/// then the <see cref="HoldLock"/> will be set to <c>true</c> prior to
		/// calling <see cref="LockPageAsync"/>.
		/// </remarks>
		protected override async Task OnPreLoadAsync(EventArgs e)
		{
			await base.OnPreLoadAsync(e).ConfigureAwait(false);

			// Enable the locking system
			IsLockingEnabled = true;

			if (TrunkTransactionContext.Current != null)
			{
				switch (TrunkTransactionContext.Current.IsolationLevel)
				{
					case IsolationLevel.ReadUncommitted:
						break;

					case IsolationLevel.ReadCommitted:
						await LockPageAsync().ConfigureAwait(false);
						break;

					case IsolationLevel.RepeatableRead:
					case IsolationLevel.Serializable:
						HoldLock = true;
						await LockPageAsync().ConfigureAwait(false);
						break;

					default:
						throw new InvalidOperationException();
				}
			}
		}

        /// <summary>
        /// Raises the <see cref="E:Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected override async Task OnPostLoadAsync(EventArgs e)
		{
			await base.OnPostLoadAsync(e).ConfigureAwait(false);

			if (TrunkTransactionContext.Current != null)
			{
				switch (TrunkTransactionContext.Current.IsolationLevel)
				{
					case IsolationLevel.ReadUncommitted:
						break;

					case IsolationLevel.ReadCommitted:
						if (!HoldLock)
						{
							await UnlockPageAsync().ConfigureAwait(false);
						}
						break;

					case IsolationLevel.RepeatableRead:
					case IsolationLevel.Serializable:
						// These locks are held until the end of the transaction
						break;

					default:
						throw new InvalidOperationException();
				}
			}
		}

		/// <summary>
		/// Raises the <see cref="E:PreSave"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected override void OnPreSave(EventArgs e)
		{
			// Every call to PreSaveInternal must update the timestamp
			long updateTimestamp;
			PreUpdateTimestamp();
			try
			{
				updateTimestamp = UpdateTimestamp();
			}
			finally
			{
				PostUpdateTimestamp();
			}

			// Now we can do base class save work
			base.OnPreSave(e);

			// Mark buffer as dirty and setup timestamp
			DataBuffer.SetDirtyAsync();
			DataBuffer.Timestamp = updateTimestamp;
		}

        /// <summary>
        /// Called by the Storage Engine prior to updating the page timestamp.
        /// </summary>
        protected virtual void PreUpdateTimestamp()
		{
		}

        /// <summary>
        /// Called by the Storage Engine to update the page timestamp.
        /// </summary>
        /// <returns>
        /// A <see cref="long"/> value representing the new timestamp value.
        /// </returns>
        protected virtual long UpdateTimestamp()
		{
			long newTimestamp = 0;
			_syncTimestamp.Execute(
				() =>
				{
					newTimestamp = ++_timestamp.Value;
				});
			return newTimestamp;
		}

        /// <summary>
        /// Called by the Storage Engine after the timestamp has been updated.
        /// </summary>
        /// <remarks>
        /// By default this method marks the header as dirty.
        /// </remarks>
        protected virtual void PostUpdateTimestamp()
		{
			SetHeaderDirty();
		}

		/// <summary>
		/// Locks the page.
		/// </summary>
		/// <remarks>
		/// If locking for this page has been enabled, then this method will
		/// call <see cref="OnLockPageAsync(IDatabaseLockManager)"/> passing
		/// an instance of the database lock manager to perform the actual
		/// locking operation.
		/// </remarks>
		protected async Task LockPageAsync()
		{
			if (IsLockingEnabled)
			{
				// Get instance of lock manager
				var lm = GetService<IDatabaseLockManager>();
				if (lm != null)
				{
					await OnLockPageAsync(lm).ConfigureAwait(false);
				}
			}
		}

        /// <summary>
        /// Unlocks the page.
        /// </summary>
		/// <remarks>
		/// If locking for this page has been enabled and locks are not being
		/// force held, then this method will call 
		/// <see cref="OnUnlockPageAsync(IDatabaseLockManager)"/> passing
		/// an instance of the database lock manager to perform the actual
		/// unlocking operation.
		/// </remarks>
        protected async Task UnlockPageAsync()
		{
			if (IsLockingEnabled && !HoldLock)
			{
                // Get instance of lock manager
                var lm = GetService<IDatabaseLockManager>();
                if (lm != null)
				{
					await OnUnlockPageAsync(lm).ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// Called to apply suitable locks to this page.
		/// </summary>
		/// <param name="lockManager">
		/// A reference to the <see cref="IDatabaseLockManager"/>.
		/// </param>
		/// <remarks>
		/// By default this method does nothing. 
		/// Override to perform actual locking operations.
		/// </remarks>
		protected virtual Task OnLockPageAsync(IDatabaseLockManager lockManager)
		{
		    return Task.FromResult(true);
		}

        /// <summary>
        /// Called to remove locks applied to this page in a prior call to 
        /// <see cref="M:DatabasePage.OnLockPage"/>.
        /// </summary>
        /// <param name="lockManager">
        /// A reference to the <see cref="IDatabaseLockManager"/>.
        /// </param>
        /// <remarks>
        /// By default this method does nothing. 
        /// Override to perform actual locking operations.
        /// </remarks>
        protected virtual Task OnUnlockPageAsync(IDatabaseLockManager lockManager)
		{
            return Task.FromResult(true);
        }

		/// <summary>
		/// Overridden. Raises the <see cref="E:Dirty"/> event.
		/// </summary>
		/// <param name="e"></param>
		/// <remarks>
		/// The override will enlist this page in the current transaction.
		/// </remarks>
		/// <exception cref="T:InvalidOperationException">
		/// Thrown if there is no current ambient transaction.
		/// </exception>
		protected override void OnDirty(EventArgs e)
		{
			DataBuffer.EnlistInTransaction();
			base.OnDirty(e);
		}
		#endregion
	}
}
