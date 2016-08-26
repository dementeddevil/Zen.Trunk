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
		private static readonly NoneState NoneStateObject = new NoneState();
		private static readonly SharedState SharedStateObject = new SharedState();
		private static readonly UpdateState UpdateStateObject = new UpdateState();
		private static readonly ExclusiveState ExclusiveStateObject = new ExclusiveState();
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

		    public override DatabaseLockType[] CompatableLocks =>
                new[] 
		        {
		            DatabaseLockType.Shared,
		            DatabaseLockType.Update
		        };

		    public override bool CanAcquireLock(
				TransactionLock<DatabaseLockType> owner,
				AcquireLock request)
			{
				return true;
			}
		}

		protected class SharedState : DatabaseLockState
		{
			public override DatabaseLockType Lock => DatabaseLockType.Shared;

		    public override DatabaseLockType[] CompatableLocks =>
                new[] 
		        {
		            DatabaseLockType.Shared,
		            DatabaseLockType.Update,
		        };
		}

		protected class UpdateState : DatabaseLockState
		{
			public override DatabaseLockType Lock => DatabaseLockType.Update;

		    public override DatabaseLockType[] CompatableLocks =>
                new[] 
		        {
		            DatabaseLockType.Shared
		        };

		    public override bool CanEnterExclusiveLock => true;
		}

		protected class ExclusiveState : DatabaseLockState
		{
			public override DatabaseLockType Lock => DatabaseLockType.Exclusive;

		    public override DatabaseLockType[] CompatableLocks =>
                new DatabaseLockType[0];
		}
		#endregion

		#region Public Constructors
		public DatabaseLock()
		{
		}
		#endregion

		#region Protected Properties
		protected override DatabaseLockType NoneLockType => DatabaseLockType.None;
	    #endregion

		#region Protected Methods
		protected override State GetStateFromType(DatabaseLockType lockType)
		{
			switch (lockType)
			{
				case DatabaseLockType.None:
					return NoneStateObject;

				case DatabaseLockType.Shared:
					return SharedStateObject;

				case DatabaseLockType.Update:
					return UpdateStateObject;

				case DatabaseLockType.Exclusive:
					return ExclusiveStateObject;

				default:
					throw new InvalidOperationException();
			}
		}
		#endregion
	}
}
