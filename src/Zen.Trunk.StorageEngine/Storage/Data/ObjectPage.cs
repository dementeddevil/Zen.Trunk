using System;
using System.Threading.Tasks;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data
{
	/// <summary>
	/// Represents an owned database page.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Owned objects are associated with a <see cref="ObjectId"/> value.
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
		private readonly BufferFieldObjectId _objectId;
		private ObjectLockType _objectLock;
        private IDatabaseLockManager _lockManager;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPage"/> class.
        /// </summary>
        public ObjectPage()
		{
			_objectId = new BufferFieldObjectId(base.LastHeaderField);
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
				return _objectId.Value;
			}
			set
			{
				CheckReadOnly();
				if (_objectId.Value != value)
				{
					_objectId.Value = value;
					SetHeaderDirty();
				}
			}
		}

	    /// <summary>
	    /// Gets or sets the object lock.
	    /// </summary>
	    /// <value>The object lock.</value>
	    public ObjectLockType ObjectLock => _objectLock;
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

			    // Return the lock-owner block for this object instance
				var txnLocks = TrunkTransactionContext.GetTransactionLockOwnerBlock(LockManager);
				return txnLocks?.GetOrCreateDataLockOwnerBlock(ObjectId);
			}
		}
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the lock manager.
        /// </summary>
        /// <value>
        /// The lock manager.
        /// </value>
        protected IDatabaseLockManager LockManager => _lockManager ?? (_lockManager = GetService<IDatabaseLockManager>());

		/// <summary>
		/// Gets the last header field.
		/// </summary>
		/// <value>The last header field.</value>
		protected override BufferField LastHeaderField => _objectId;
        #endregion

        #region Public Method
        /// <summary>
        /// Sets the object lock asynchronous.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public async Task SetObjectLockAsync(ObjectLockType value)
        {
            if (_objectLock != value)
            {
                var oldLock = _objectLock;
                try
                {
                    _objectLock = value;
                    await LockPageAsync().ConfigureAwait(false);
                }
                catch
                {
                    _objectLock = oldLock;
                    throw;
                }
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Overridden. Called to apply suitable locks to this page.
        /// </summary>
        /// <param name="lockManager">A reference to the <see cref="IDatabaseLockManager"/>.</param>
        protected override async Task OnLockPageAsync(IDatabaseLockManager lockManager)
		{
			// Perform base class locking first
			await base.OnLockPageAsync(lockManager).ConfigureAwait(false);
			try
			{
				// Lock owner via lock owner block
				await LockBlock.LockOwnerAsync(ObjectLock, LockTimeout).ConfigureAwait(false);
			}
			catch
			{
				await base.OnUnlockPageAsync(lockManager).ConfigureAwait(false);
				throw;
			}
		}

		/// <summary>
		/// Overridden. Called to remove locks applied to this page in a 
		/// prior call to <see cref="M:DatabasePage.OnLockPage"/>.
		/// </summary>
		/// <param name="lockManager">A reference to the <see cref="IDatabaseLockManager"/>.</param>
		protected override async Task OnUnlockPageAsync(IDatabaseLockManager lockManager)
		{
			try
			{
				// Unlock owner via lock owner block
				var lob = LockBlock;
				if (lob != null)
				{
					await LockBlock.UnlockOwnerAsync().ConfigureAwait(false);
				}
			}
			finally
			{
				// Perform base class unlock last
				await base.OnUnlockPageAsync(lockManager).ConfigureAwait(false);
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
		protected override async Task OnPreInitAsync(EventArgs e)
		{
			// If no lock is specified then try to obtain intent-exclusive lock.
			if (ObjectLock == ObjectLockType.None)
			{
				await SetObjectLockAsync(ObjectLockType.IntentExclusive).ConfigureAwait(false);
			}
			await base.OnPreInitAsync(e).ConfigureAwait(false);
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
		protected override async Task OnPreLoadAsync(EventArgs e)
		{
			// If no lock is specified then try to obtain intent-shared lock.
			if (ObjectLock == ObjectLockType.None)
			{
				await SetObjectLockAsync(ObjectLockType.IntentShared).ConfigureAwait(false);
			}
			await base.OnPreLoadAsync(e).ConfigureAwait(false);
		}
		#endregion
	}
}
