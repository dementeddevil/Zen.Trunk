namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.IO;
	using System.Threading;
	using System.Transactions;
	using Zen.Trunk.Storage.Locking;

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
		private TimeSpan _lockTimeout = TimeSpan.FromSeconds(10);
		private PageBuffer _buffer;
		private readonly SpinLockClass _syncTimestamp = new SpinLockClass();
		private readonly BufferFieldInt64 _timestamp;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DatabasePage"/> class.
		/// </summary>
		/// <param name="owner">The owner.</param>
		public DataPage()
		{
			// TODO Assert trust permissions here
			/*_database = owner;
			if (_database != null)
			{
				ReadOnly = _database.IsReadOnly;
			}*/
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
		/// <see cref="T:BufferBase"/> object.
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
		public TimeSpan LockTimeout
		{
			get
			{
				return _lockTimeout;
			}
			set
			{
				_lockTimeout = value;
			}
		}

		/// <summary>
		/// Gets/sets a boolean that indicates whether locks are held until
		/// the current transaction is committed.
		/// </summary>
		/// <value>
		/// A boolean value indicating whether to hold locks.
		/// If true locks are held until the end of the transaction
		/// If false locks are held until the page has been read from storage.
		/// </value>
		public bool HoldLock
		{
			get;
			set;
		}

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
		public FileGroupId FileGroupId
		{
			get;
			set;
		}
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
					if (_buffer != null)
					{
						_buffer.Release();
					}
					_buffer = value;
					if (_buffer != null)
					{
						_buffer.AddRef();
					}
				}
			}
		}

		internal TransactionLockOwnerBlock TransactionLocks
		{
			get
			{
				TransactionLockOwnerBlock result = null;
				var privTxn = TrunkTransactionContext.Current as ITrunkTransactionPrivate;
				if (privTxn != null)
				{
					result = privTxn.TransactionLocks;
				}
				return result;
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
			//UnlockPage();

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

		protected override Stream CreateHeaderStream(bool readOnly)
		{
			Tracer.WriteVerboseLine(
				"CreateHeaderStream ({0})",
				new object[]
				{
					readOnly ? "read-only" : "writeable"
				});

			// Return memory stream based on underlying buffer memory
			return _buffer.GetBufferStream(0, (int)HeaderSize, !readOnly);
		}

		public override Stream CreateDataStream(bool readOnly)
		{
			Tracer.WriteVerboseLine(
				"CreateDataStream ({0})",
				new object[]
				{
					readOnly ? "read-only" : "writeable"
				});

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
		protected override void OnPreInit(EventArgs e)
		{
			base.OnPreInit(e);

			// Enable the locking system
			IsLockingEnabled = true;

			// Attempt to acquire lock
			// NOTE: For new pages we always hold the lock until commit
			HoldLock = true;
			LockPage();
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
		/// LockPage.
		/// This mechanism ensures that all lock states have been set prior to
		/// the first call to LockPage.
		/// </remarks>
		protected override void OnPreLoad(EventArgs e)
		{
			base.OnPreLoad(e);

			// Enable the locking system
			IsLockingEnabled = true;

			if (TrunkTransactionContext.Current != null)
			{
				switch (TrunkTransactionContext.Current.IsolationLevel)
				{
					case IsolationLevel.ReadUncommitted:
						break;

					case IsolationLevel.ReadCommitted:
						LockPage();
						break;

					case IsolationLevel.RepeatableRead:
					case IsolationLevel.Serializable:
						HoldLock = true;
						LockPage();
						break;

					default:
						throw new InvalidOperationException();
				}
			}
		}

		protected override void OnPostLoad(EventArgs e)
		{
			base.OnPostLoad(e);

			if (TrunkTransactionContext.Current != null)
			{
				switch (TrunkTransactionContext.Current.IsolationLevel)
				{
					case IsolationLevel.ReadUncommitted:
						break;

					case IsolationLevel.ReadCommitted:
						if (!HoldLock)
						{
							UnlockPage();
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
			long updateTimestamp = 0;
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

		protected virtual void PreUpdateTimestamp()
		{
		}

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

		protected virtual void PostUpdateTimestamp()
		{
			SetHeaderDirty();
		}

		/// <summary>
		/// Locks the page.
		/// </summary>
		protected virtual void LockPage()
		{
			if (IsLockingEnabled)
			{
				// Get instance of lock manager
				var lm = GetService<IDatabaseLockManager>();
				if (lm != null)
				{
					OnLockPage(lm);
				}
			}
		}

		/// <summary>
		/// Called to apply suitable locks to this page.
		/// </summary>
		/// <param name="lm">A reference to the <see cref="IDatabaseLockManager"/>.</param>
		protected virtual void OnLockPage(IDatabaseLockManager lm)
		{
		}

		/// <summary>
		/// Called to remove locks applied to this page in a prior call to 
		/// <see cref="M:DatabasePage.OnLockPage"/>.
		/// </summary>
		/// <param name="lm">A reference to the <see cref="IDatabaseLockManager"/>.</param>
		protected virtual void OnUnlockPage(IDatabaseLockManager lm)
		{
		}

		/// <summary>
		/// Unlocks the page.
		/// </summary>
		protected virtual void UnlockPage()
		{
			if (IsLockingEnabled && !HoldLock)
			{
                // Get instance of lock manager
                var lm = GetService<IDatabaseLockManager>();
                if (lm != null)
				{
					OnUnlockPage(lm);
				}
			}
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

		#region Private Methods
		#endregion
	}
}
