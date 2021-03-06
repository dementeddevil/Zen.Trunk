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
    public class ObjectSchemaPage : ObjectPage, IObjectSchemaPage
    {
        #region Private Fields
        private SchemaLockType _schemaLock;
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the schema lock.
        /// </summary>
        /// <value>The schema lock.</value>
        public SchemaLockType SchemaLock => _schemaLock;
        #endregion

        #region Internal Properties
        internal ISchemaLock ObjectSchemaLock =>
            TransactionLockOwnerBlock?.GetOrCreateSchemaLock(ObjectId);
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
        /// <remarks>
        /// Overrides to this method must set their desired lock prior to
        /// calling the base class.
        /// The base class method will enable the locking primitives and call
        /// LockPage.
        /// This mechanism ensures that all lock states have been set prior to
        /// the first call to LockPage.
        /// </remarks>
        protected override async Task OnPreInitAsync()
        {
            if (SchemaLock == SchemaLockType.None)
            {
                await SetSchemaLockAsync(SchemaLockType.SchemaModification).ConfigureAwait(false);
            }
            await base.OnPreInitAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Overridden. Called by the system prior to loading the page
        /// from persistent storage.
        /// </summary>
        /// <remarks>
        /// Overrides to this method must set their desired lock prior to
        /// calling the base class.
        /// The base class method will enable the locking primitives and call
        /// LockPage.
        /// This mechanism ensures that all lock states have been set prior to
        /// the first call to LockPage.
        /// </remarks>
        protected override async Task OnPreLoadAsync()
        {
            if (SchemaLock == SchemaLockType.None)
            {
                await SetSchemaLockAsync(SchemaLockType.SchemaModification).ConfigureAwait(false);
            }
            await base.OnPreLoadAsync().ConfigureAwait(false);
        }

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
                // Lock schema
                await ObjectSchemaLock.LockAsync(SchemaLock, LockTimeout).ConfigureAwait(false);
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
            // Unlock page based on schema
            try
            {
                await ObjectSchemaLock.UnlockAsync().ConfigureAwait(false);
            }
            finally
            {
                // Perform base class unlock last
                await base.OnUnlockPageAsync(lockManager).ConfigureAwait(false);
            }
        }
        #endregion
    }
}
