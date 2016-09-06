using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Data
{
	using System;
	using Locking;

	/// <summary>
	/// <c>SchemaPage</c> extends <see cref="T:ObjectPage"/> to add support
	/// for obtaining schema-locks.
	/// </summary>
	/// <remarks>
	/// Schema locking support is required for tables and other media objects.
	/// </remarks>
	public class ObjectSchemaPage : ObjectPage
	{
		#region Private Fields
		private SchemaLockType _schemaLock;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectSchemaPage"/> class.
		/// </summary>
		public ObjectSchemaPage()
		{
		}
		#endregion

		#region Public Properties
	    /// <summary>
	    /// Gets or sets the schema lock.
	    /// </summary>
	    /// <value>The schema lock.</value>
	    public SchemaLockType SchemaLock => _schemaLock;
		#endregion

		#region Internal Properties
		internal SchemaLock TrackedLock
		{
			get
			{
				if (TrunkTransactionContext.Current == null)
				{
					throw new InvalidOperationException("No current transaction.");
				}

				// If we have no transaction locks then we should be in dispose
				var txnLocks = TrunkTransactionContext.TransactionLocks;

			    // Return the lock-owner block for this object instance
				return txnLocks?.GetOrCreateSchemaLock(ObjectId);
			}
		}
        #endregion

	    #region Public Methods
        /// <summary>
        /// Attempts to set the specified schema lock.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public async Task SetSchemaLockAsync(SchemaLockType value)
		{
			if (_schemaLock != value)
			{
				var oldLock = _schemaLock;
				try
				{
					_schemaLock = value;
					await LockPageAsync().ConfigureAwait(false);
				}
				catch
				{
					_schemaLock = oldLock;
					throw;
				}
			}
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
        protected override async Task OnPreInitAsync(EventArgs e)
		{
			if (SchemaLock == SchemaLockType.None)
			{
				await SetSchemaLockAsync(SchemaLockType.SchemaModification).ConfigureAwait(false);
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
			if (SchemaLock == SchemaLockType.None)
			{
				await SetSchemaLockAsync(SchemaLockType.SchemaModification).ConfigureAwait(false);
			}
			await base.OnPreLoadAsync(e).ConfigureAwait(false);
		}

		/// <summary>
		/// Overridden. Called to apply suitable locks to this page.
		/// </summary>
		/// <param name="lm">A reference to the <see cref="IDatabaseLockManager"/>.</param>
		protected override async Task OnLockPageAsync(IDatabaseLockManager lm)
		{
			// Perform base class locking first
			await base.OnLockPageAsync(lm).ConfigureAwait(false);
			try
			{
				// Lock schema
				await TrackedLock.LockAsync(SchemaLock, LockTimeout).ConfigureAwait(false);
				//lm.LockSchema(ObjectId, SchemaLock, LockTimeout);
			}
			catch
			{
				await base.OnUnlockPageAsync(lm).ConfigureAwait(false);
				throw;
			}
		}

		/// <summary>
		/// Overridden. Called to remove locks applied to this page in a
		/// prior call to <see cref="M:DatabasePage.OnLockPage"/>.
		/// </summary>
		/// <param name="lm">A reference to the <see cref="IDatabaseLockManager"/>.</param>
		protected override async Task OnUnlockPageAsync(IDatabaseLockManager lm)
		{
			// Unlock page based on schema
			try
			{
				await TrackedLock.UnlockAsync().ConfigureAwait(false);
				//lm.UnlockSchema(ObjectId);
			}
			finally
			{
				// Perform base class unlock last
				await base.OnUnlockPageAsync(lm).ConfigureAwait(false);
			}
		}
		#endregion
	}
}
