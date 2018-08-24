using System;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>ChildTransactionLock</c> encapsulates the semantics necessary
    /// for a lock object that has a parent lock.
    /// </summary>
    /// <typeparam name="TLockTypeEnum">The lock type enum.</typeparam>
    /// <typeparam name="TParentLockTypeEnum">The parent lock type enum.</typeparam>
    /// <seealso cref="Zen.Trunk.Storage.Locking.TransactionLock{LockTypeEnum}" />
    public abstract class ChildTransactionLock<TLockTypeEnum, TParentLockTypeEnum> :
        TransactionLock<TLockTypeEnum>,
        IChildTransactionLock<TLockTypeEnum, TParentLockTypeEnum>
        where TLockTypeEnum : struct, IComparable, IConvertible, IFormattable // enum
		where TParentLockTypeEnum : struct, IComparable, IConvertible, IFormattable //enum
    {
		#region Private Fields
		private ITransactionLock<TParentLockTypeEnum> _parentLock;
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the parent lock.
		/// </summary>
		/// <value>The parent.</value>
		public ITransactionLock<TParentLockTypeEnum> Parent
		{
			get => _parentLock;
		    set
			{
				if (_parentLock != value)
				{
				    ((IReferenceLock) _parentLock)?.ReleaseRefLock();
				    _parentLock = value;
				    _parentLock?.AddRefLock();
				}
			}
		}
        #endregion

        #region Protected Methods
        /// <summary>
        /// Called when last reference to the lock is released.
        /// </summary>
        protected override void OnFinalRelease()
		{
			Parent = null;
			base.OnFinalRelease();
		}
		#endregion
	}
}
