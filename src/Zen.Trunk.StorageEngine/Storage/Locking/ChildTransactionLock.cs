namespace Zen.Trunk.Storage.Locking
{
	using System;

	public abstract class ChildTransactionLock<LockTypeEnum, ParentLockType> :
		TransactionLock<LockTypeEnum>
		where LockTypeEnum : struct, IComparable, IConvertible, IFormattable // enum
		where ParentLockType : class, IReferenceLock
	{
		#region Private Fields
		private ParentLockType _parentLock;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="ChildTransactionLock&lt;LockTypeEnum, ParentLockType&gt;"/>
		/// class.
		/// </summary>
		protected ChildTransactionLock()
		{
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the parent lock.
		/// </summary>
		/// <value>The parent.</value>
		public ParentLockType Parent
		{
			get
			{
				return _parentLock;
			}
			set
			{
				if (_parentLock != value)
				{
					if (_parentLock != null)
					{
						_parentLock.ReleaseLock();
					}
					_parentLock = value;
					if (_parentLock != null)
					{
						_parentLock.AddRefLock();
					}
				}
			}
		}
		#endregion

		#region Protected Methods
		protected override void OnFinalRelease()
		{
			Parent = null;
			base.OnFinalRelease();
		}
		#endregion
	}
}
