namespace Zen.Trunk.Storage.Data
{
	using System;
	using Zen.Trunk.Storage.Locking;

	/// <summary>
	/// Represents an owned database page.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Owned objects are associated with a <see cref="System.Int32"/> value.
	/// These objects are tracked by distribution pages and the system object
	/// table.
	/// </para>
	/// <para>
	/// Typically owned pages are linked together by way of logical ID
	/// chains.
	/// </para>
	/// </remarks>
	public class ObjectPage : LogicalPage
	{
		#region Private Fields
		private readonly BufferFieldUInt32 _objectId;
		private ObjectLockType _objectLock;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectPage"/> class.
		/// </summary>
		public ObjectPage()
		{
			_objectId = new BufferFieldUInt32(base.LastHeaderField, 0);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the minimum number of bytes required for the header block.
		/// </summary>
		/// <value></value>
		public override uint MinHeaderSize => base.MinHeaderSize + 4;

	    /// <summary>
		/// Gets or sets the object id.
		/// </summary>
		/// <value>The object id.</value>
		public ObjectId ObjectId
		{
			get
			{
				return new ObjectId(_objectId.Value);
			}
			set
			{
				CheckReadOnly();
				if (_objectId.Value != value.Value)
				{
					_objectId.Value = value.Value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets or sets the object lock.
		/// </summary>
		/// <value>The object lock.</value>
		public ObjectLockType ObjectLock
		{
			get
			{
				return _objectLock;
			}
			set
			{
				if (_objectLock != value)
				{
					var oldLock = _objectLock;
					try
					{
						_objectLock = value;
						LockPage();
					}
					catch
					{
						_objectLock = oldLock;
						throw;
					}
				}
			}
		}
		#endregion

		#region Internal Properties
		internal DataLockOwnerBlock LockBlock
		{
			get
			{
				if (TrunkTransactionContext.Current == null)
				{
					throw new InvalidOperationException("No current transaction.");
				}

				// If we have no transaction locks then we should be in dispose
				var txnLocks = TrunkTransactionContext.TransactionLocks;
				if (txnLocks == null)
				{
					return null;
				}

				// Return the lock-owner block for this object instance
				return txnLocks.GetOrCreateDataLockOwnerBlock(ObjectId);
			}
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the last header field.
		/// </summary>
		/// <value>The last header field.</value>
		protected override BufferField LastHeaderField => _objectId;

	    #endregion

		#region Protected Methods
		/// <summary>
		/// Overridden. Called to apply suitable locks to this page.
		/// </summary>
		/// <param name="lm">A reference to the <see cref="IDatabaseLockManager"/>.</param>
		protected override void OnLockPage(IDatabaseLockManager lm)
		{
			// Perform base class locking first
			base.OnLockPage(lm);
			try
			{
				// Lock owner via lock owner block
				LockBlock.LockOwner(ObjectLock, LockTimeout);
			}
			catch
			{
				base.OnUnlockPage(lm);
				throw;
			}
		}

		/// <summary>
		/// Overridden. Called to remove locks applied to this page in a 
		/// prior call to <see cref="M:DatabasePage.OnLockPage"/>.
		/// </summary>
		/// <param name="lm">A reference to the <see cref="IDatabaseLockManager"/>.</param>
		protected override void OnUnlockPage(IDatabaseLockManager lm)
		{
			try
			{
				// Unlock owner via lock owner block
				var lob = LockBlock;
				if (lob != null)
				{
					LockBlock.UnlockOwner();
				}
			}
			finally
			{
				// Perform base class unlock last
				base.OnUnlockPage(lm);
			}
		}

		/// <summary>
		/// Performs operations on this instance prior to being initialised.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
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
			// If no lock is specified then try to obtain intent-exclusive lock.
			if (ObjectLock == ObjectLockType.None)
			{
				ObjectLock = ObjectLockType.IntentExclusive;
			}
			base.OnPreInit(e);
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
			// If no lock is specified then try to obtain intent-shared lock.
			if (ObjectLock == ObjectLockType.None)
			{
				ObjectLock = ObjectLockType.IntentShared;
			}
			base.OnPreLoad(e);
		}
		#endregion
	}
}
