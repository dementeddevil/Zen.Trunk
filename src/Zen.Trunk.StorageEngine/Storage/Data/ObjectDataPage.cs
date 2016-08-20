namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Transactions;
	using Zen.Trunk.Storage.Locking;

	/// <summary>
	/// <b>ObjectDataPage</b> objects have a concept of being owned by
	/// another object and to facilitate this they have an associated
	/// Object Id.
	/// </summary>
	/// <remarks>
	/// Currently the object ID can refer to a table, sample or index.
	/// TODO: Implement mechanism for enabling hold-lock support.
	/// </remarks>
	public class ObjectDataPage : ObjectPage
	{
		#region Private Fields
		private bool _mustHoldLock;
		private DataLockType _pageLock;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectDataPage"/> class.
		/// </summary>
		public ObjectDataPage()
		{
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the page lock.
		/// </summary>
		/// <value>The page lock.</value>
		public DataLockType PageLock
		{
			get
			{
				return _pageLock;
			}
			set
			{
				if (_pageLock != value)
				{
					DataLockType oldLock = _pageLock;
					try
					{
						_pageLock = value;
						LockPage();
					}
					catch
					{
						_pageLock = oldLock;
						throw;
					}
				}
			}
		}
		#endregion

		#region Public Methods
		public void SetDirtyState()
		{
			SetDirty();
		}
		#endregion

		#region Protected Methods
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
			// NOTE: We do not apply a default lock here because we wish to
			//	support reading uncommitted data....
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
			// NOTE: We do not apply a default lock here unless we have an 
			//	active transaction context...
			if (TrunkTransactionContext.Current != null)
			{
				switch(TrunkTransactionContext.Current.IsolationLevel)
				{
					case IsolationLevel.ReadCommitted:
						if (PageLock == DataLockType.None)
						{
							PageLock = DataLockType.Shared;
						}
						_mustHoldLock = false;
						break;
					case IsolationLevel.RepeatableRead:
						if (PageLock == DataLockType.None)
						{
							PageLock = DataLockType.Shared;
						}
						_mustHoldLock = true;
						break;
					case IsolationLevel.Serializable:
						// TODO: This implementation is not correct

						// Ensure we have the correct type of object lock
						if (ObjectLock == ObjectLockType.None)
						{
							// This will block any other serialzable transaction
							//	from the owner object.
							ObjectLock = ObjectLockType.SharedIntentExclusive;
						}

						// Ensure we have the correct type of page lock
						if (PageLock == DataLockType.None)
						{
							if (PageType == PageType.Index)
							{
								PageLock = DataLockType.Exclusive;
							}
							else if (PageType == Storage.PageType.Data)
							{
								PageLock = DataLockType.Shared;
							}
						}

						// Serializable transactions must hold locks until 
						//	commit/rollback.
						_mustHoldLock = true;
						break;
				}
			}
			base.OnPreLoad(e);
		}

		protected override void OnPostLoad(EventArgs e)
		{
			// Shared read locks on readcommitted are released after
			//	load unless we are requested to hold the lock
			if (!_mustHoldLock && PageLock == DataLockType.Shared &&
				TrunkTransactionContext.Current != null &&
				TrunkTransactionContext.Current.IsolationLevel == IsolationLevel.ReadCommitted)
			{
				UnlockPage();
			}
			base.OnPostLoad(e);
		}

		protected override void OnLockPage(IDatabaseLockManager lm)
		{
			// Perform base class locking first
			base.OnLockPage(lm);
			try
			{
				// Lock data via lock owner block
				DataLockOwnerBlock lob = LockBlock;
				if (lob == null)
				{
					throw new InvalidOperationException("Cannot obtain lock owner block for this transaction.");
				}
				lob.LockItem(LogicalId, PageLock, LockTimeout);
			}
			catch
			{
				base.OnUnlockPage(lm);
				throw;
			}
		}

		protected override void OnUnlockPage(IDatabaseLockManager lm)
		{
			try
			{
				// Unlock data via lock owner block
				DataLockOwnerBlock lob = LockBlock;
				if (lob != null)
				{
					lob.UnlockItem(LogicalId);
				}
			}
			finally
			{
				// Perform base class unlock last
				base.OnUnlockPage(lm);
			}
		}
		#endregion
	}
}
