using System;

namespace Zen.Trunk.Storage.Locking
{
	/// <summary>
	/// Defines page level lock types.
	/// </summary>
	public enum DataLockType
	{
		/// <summary>
		/// No locking required.
		/// </summary>
		None = 0,

		/// <summary>
		/// Represents a shared read lock
		/// </summary>
		Shared = 1,

		/// <summary>
		/// Represents an update lock
		/// </summary>
		/// <remarks>
		/// This lock type is not enough to update the page but it is
		/// used to serialise access to the Exclusive lock.
		/// </remarks>
		Update = 2,

		/// <summary>
		/// Represents an exclusive lock
		/// </summary>
		Exclusive = 3,
	}

	/// <summary>
	/// Implements a general page lock suitable for root, table, sample,
	/// data and index leaf pages.
	/// </summary>
	public class DataLock : ChildTransactionLock<DataLockType, ObjectLock>
	{
		#region Data Locking State
		protected abstract class DataLockState : State
		{
			public override bool IsExclusiveLock(DataLockType lockType)
			{
				return lockType == DataLockType.Exclusive;
			}
		}

        protected class NoneState : DataLockState
		{
			public override DataLockType Lock => DataLockType.None;

		    public override DataLockType[] CompatableLocks =>
                new[]
		        {
		            DataLockType.Shared,
		            DataLockType.Update,
		            DataLockType.Exclusive,
		        };
		}

        protected class SharedState : DataLockState
		{
			public override DataLockType Lock => DataLockType.Shared;

		    public override DataLockType[] CompatableLocks =>
                new[]
		        {
		            DataLockType.Shared,
		            DataLockType.Update,
		        };
		}

        protected class UpdateState : DataLockState
		{
			public override DataLockType Lock => DataLockType.Update;

		    public override DataLockType[] CompatableLocks =>
                new[] 
		        {
		            DataLockType.Shared,
		        };

		    public override bool CanEnterExclusiveLock => true;
		}

        protected class ExclusiveState : DataLockState
		{
			public override DataLockType Lock => DataLockType.Exclusive;

		    public override DataLockType[] CompatableLocks =>
                new DataLockType[0];
		}
		#endregion

		#region Private Fields
		private static readonly NoneState NoneStateObject = new NoneState();
		private static readonly SharedState SharedStateObject = new SharedState();
		private static readonly UpdateState UpdateStateObject = new UpdateState();
		private static readonly ExclusiveState ExclusiveStateObject = new ExclusiveState();
		#endregion

		#region Public Constructors
		public DataLock()
		{
		}
		#endregion

		#region Protected Properties
		protected override DataLockType NoneLockType => DataLockType.None;
	    #endregion

		#region Protected Methods
		protected override State GetStateFromType(DataLockType lockType)
		{
			switch (lockType)
			{
				case DataLockType.None:
					return NoneStateObject;

				case DataLockType.Shared:
					return SharedStateObject;

				case DataLockType.Update:
					return UpdateStateObject;

				case DataLockType.Exclusive:
					return ExclusiveStateObject;

                default:
			        throw new InvalidOperationException();
			}
		}
		#endregion
	}
}
