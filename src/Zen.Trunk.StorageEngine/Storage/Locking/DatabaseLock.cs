namespace Zen.Trunk.Storage.Locking
{
	using System;

	/// <summary>
	/// Defines locking primatives which apply to the database itself
	/// </summary>
	public enum DatabaseLockType
	{
		/// <summary>
		/// No locking required (illegal)
		/// </summary>
		None = 0,

		/// <summary>
		/// Shared access to database
		/// </summary>
		Shared = 1,

		/// <summary>
		/// Update lock - used to serialise access to exclusive state
		/// </summary>
		Update = 2,

		/// <summary>
		/// Exclusive read/write access to database
		/// </summary>
		Exclusive = 3
	}

	/// <summary>
	/// Implements a schema lock for locking table schema and 
	/// sample wave format blocks.
	/// </summary>
	public class DatabaseLock : TransactionLock<DatabaseLockType>
	{
		#region Private Fields
		private static readonly NoneState noneState;
		private static readonly SharedState sharedState;
		private static readonly UpdateState updateState;
		private static readonly ExclusiveState exclusiveState;
		#endregion

		#region Database Lock State
		protected abstract class DatabaseLockState : State
		{
			public override bool IsExclusiveLock(DatabaseLockType lockType)
			{
				return lockType == DatabaseLockType.Exclusive;
			}
		}
		protected class NoneState : DatabaseLockState
		{
			public override DatabaseLockType Lock => DatabaseLockType.None;

		    public override DatabaseLockType[] CompatableLocks => new DatabaseLockType[] 
		    {
		        DatabaseLockType.Shared,
		        DatabaseLockType.Update
		    };

		    public override bool CanAcquireLock(
				TransactionLock<DatabaseLockType> owner,
				TransactionLock<DatabaseLockType>.AcquireLock request)
			{
				return true;
			}
		}
		protected class SharedState : DatabaseLockState
		{
			public override DatabaseLockType Lock => DatabaseLockType.Shared;

		    public override DatabaseLockType[] CompatableLocks => new DatabaseLockType[2] 
		    {
		        DatabaseLockType.Shared,
		        DatabaseLockType.Update,
		    };
		}
		protected class UpdateState : DatabaseLockState
		{
			public override DatabaseLockType Lock => DatabaseLockType.Update;

		    public override DatabaseLockType[] CompatableLocks => new DatabaseLockType[] 
		    {
		        DatabaseLockType.Shared
		    };

		    public override bool CanEnterExclusiveLock => true;
		}
		protected class ExclusiveState : DatabaseLockState
		{
			public override DatabaseLockType Lock => DatabaseLockType.Exclusive;

		    public override DatabaseLockType[] CompatableLocks => new DatabaseLockType[0];
		}
		#endregion

		#region Public Constructors
		static DatabaseLock()
		{
			noneState = new NoneState();
			sharedState = new SharedState();
			updateState = new UpdateState();
			exclusiveState = new ExclusiveState();
		}

		public DatabaseLock()
		{
		}
		#endregion

		#region Public Properties
		#endregion

		#region Protected Properties
		protected override DatabaseLockType NoneLockType => DatabaseLockType.None;

	    #endregion

		#region Protected Methods
		protected override TransactionLock<DatabaseLockType>.State GetStateFromType(DatabaseLockType lockType)
		{
			switch (lockType)
			{
				case DatabaseLockType.None:
					return noneState;

				case DatabaseLockType.Shared:
					return sharedState;

				case DatabaseLockType.Update:
					return updateState;

				case DatabaseLockType.Exclusive:
					return exclusiveState;

				default:
					throw new InvalidOperationException();
			}
		}
		#endregion
	}
}
