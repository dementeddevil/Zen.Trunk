using System;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>ChildTransactionLock</c> encapsulates the semantics necessary
    /// for a lock object that has a parent lock.
    /// </summary>
    /// <typeparam name="TLockTypeEnum">The type of the ock type enum.</typeparam>
    /// <typeparam name="TParentLockType">The type of the arent lock type.</typeparam>
    /// <seealso cref="Zen.Trunk.Storage.Locking.TransactionLock{LockTypeEnum}" />
    public abstract class ChildTransactionLock<TLockTypeEnum, TParentLockType> :
		TransactionLock<TLockTypeEnum>
		where TLockTypeEnum : struct, IComparable, IConvertible, IFormattable // enum
		where TParentLockType : class, IReferenceLock
	{
		#region Private Fields
		private TParentLockType _parentLock;
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the parent lock.
		/// </summary>
		/// <value>The parent.</value>
		public TParentLockType Parent
		{
			get => _parentLock;
		    set
			{
				if (_parentLock != value)
				{
				    _parentLock?.ReleaseLock();
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
