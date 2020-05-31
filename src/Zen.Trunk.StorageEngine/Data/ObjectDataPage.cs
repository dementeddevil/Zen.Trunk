using System;
using System.Transactions;
using System.Threading.Tasks;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// <b>ObjectDataPage</b> objects have a concept of being owned by
    /// another object and to facilitate this they have an associated
    /// Object Id.
    /// </summary>
    /// <remarks>
    /// Currently the object ID can refer to a table, sample or index.
    /// TODO: Implement mechanism for enabling hold-lock support.
    /// </remarks>
    public class ObjectDataPage : ObjectPage, IObjectDataPage
    {
        #region Private Fields
        private bool _mustHoldLock;
        private DataLockType _pageLock;
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the page lock.
        /// </summary>
        /// <value>The page lock.</value>
        public DataLockType PageLock => _pageLock;
        #endregion

        #region Public Methods
        /// <summary>
        /// Attempts to sets the page lock asynchronous.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public async Task SetPageLockAsync(DataLockType value)
        {
            if (_pageLock != value)
            {
                var oldLock = _pageLock;
                try
                {
                    _pageLock = value;
                    await LockPageAsync().ConfigureAwait(false);
                }
                catch
                {
                    _pageLock = oldLock;
                    throw;
                }
            }
        }

        /// <summary>
        /// Sets the state of the dirty.
        /// </summary>
        public void SetDirtyState()
        {
            SetDirty();
        }
        #endregion

        #region Protected Methods
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
            // NOTE: We do not apply a default lock here unless we have an 
            //	active transaction context...
            if (TrunkTransactionContext.Current != null)
            {
                switch (TrunkTransactionContext.Current.IsolationLevel)
                {
                    case IsolationLevel.ReadCommitted:
                        if (PageLock == DataLockType.None)
                        {
                            await SetPageLockAsync(DataLockType.Shared).ConfigureAwait(false);
                        }
                        _mustHoldLock = false;
                        break;
                    case IsolationLevel.RepeatableRead:
                        if (PageLock == DataLockType.None)
                        {
                            await SetPageLockAsync(DataLockType.Shared).ConfigureAwait(false);
                        }
                        _mustHoldLock = true;
                        break;
                    case IsolationLevel.Serializable:
                        // Ensure we have the correct type of object lock
                        if (ObjectLock == ObjectLockType.None)
                        {
                            // This will block any other serializable transaction
                            //	from the owner object.
                            await SetObjectLockAsync(ObjectLockType.SharedIntentExclusive).ConfigureAwait(false);
                        }

                        // Ensure we have the correct type of page lock
                        if (PageLock == DataLockType.None)
                        {
                            if (PageType == PageType.Index)
                            {
                                await SetPageLockAsync(DataLockType.Exclusive).ConfigureAwait(false);
                            }
                            else if (PageType == PageType.Data)
                            {
                                await SetPageLockAsync(DataLockType.Shared).ConfigureAwait(false);
                            }
                        }

                        // Serializable transactions must hold locks until 
                        //	commit/rollback.
                        _mustHoldLock = true;
                        break;
                }
            }
            await base.OnPreLoadAsync(e).ConfigureAwait(false);
        }

        /// <summary>
        /// Raises the <see cref="E:PostLoadAsync" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        protected override async Task OnPostLoadAsync(EventArgs e)
        {
            // Shared read locks on readcommitted are released after
            //	load unless we are requested to hold the lock
            if (!_mustHoldLock && PageLock == DataLockType.Shared &&
                TrunkTransactionContext.Current != null &&
                TrunkTransactionContext.Current.IsolationLevel == IsolationLevel.ReadCommitted)
            {
                await UnlockPageAsync().ConfigureAwait(false);
            }
            await base.OnPostLoadAsync(e).ConfigureAwait(false);
        }

        /// <summary>
        /// Overridden. Called to apply suitable locks to this page.
        /// </summary>
        /// <param name="lockManager">A reference to the <see cref="IDatabaseLockManager" />.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Cannot obtain lock owner block for this transaction.</exception>
        protected override async Task OnLockPageAsync(IDatabaseLockManager lockManager)
        {
            // Perform base class locking first
            await base.OnLockPageAsync(lockManager).ConfigureAwait(false);
            try
            {
                // Lock data via lock owner block
                var lob = DataLockOwnerBlock;
                if (lob == null)
                {
                    throw new InvalidOperationException("Cannot obtain lock owner block for this transaction.");
                }
                await lob.LockItemAsync(LogicalPageId, PageLock, LockTimeout).ConfigureAwait(false);
            }
            catch
            {
                await base.OnUnlockPageAsync(lockManager).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Overridden. Called to remove locks applied to this page in a
        /// prior call to <see cref="M:DatabasePage.OnLockPage" />.
        /// </summary>
        /// <param name="lockManager">A reference to the <see cref="IDatabaseLockManager" />.</param>
        /// <returns></returns>
        protected override async Task OnUnlockPageAsync(IDatabaseLockManager lockManager)
        {
            try
            {
                // Unlock data via lock owner block
                var lob = DataLockOwnerBlock;
                if (lob != null)
                {
                    await lob.UnlockItemAsync(LogicalPageId).ConfigureAwait(false);
                }
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
