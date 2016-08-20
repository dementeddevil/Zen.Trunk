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
			public override DataLockType Lock
			{
				get
				{
					return DataLockType.None;
				}
			}

			public override DataLockType[] CompatableLocks
			{
				get
				{
					return new DataLockType[]
					{
						DataLockType.Shared,
						DataLockType.Update,
						DataLockType.Exclusive,
					};
				}
			}
		}
		protected class SharedState : DataLockState
		{
			public override DataLockType Lock
			{
				get
				{
					return DataLockType.Shared;
				}
			}

			public override DataLockType[] CompatableLocks
			{
				get
				{
					return new DataLockType[]
					{
						DataLockType.Shared,
						DataLockType.Update,
					};
				}
			}
		}
		protected class UpdateState : DataLockState
		{
			public override DataLockType Lock
			{
				get
				{
					return DataLockType.Update;
				}
			}

			public override DataLockType[] CompatableLocks
			{
				get
				{
					return new DataLockType[] 
					{
						DataLockType.Shared,
					};
				}
			}

			public override bool CanEnterExclusiveLock
			{
				get
				{
					return true;
				}
			}
		}
		protected class ExclusiveState : DataLockState
		{
			public override DataLockType Lock
			{
				get
				{
					return DataLockType.Exclusive;
				}
			}

			public override DataLockType[] CompatableLocks
			{
				get
				{
					return new DataLockType[0];
				}
			}
		}
		#endregion

		#region Private Fields
		private static readonly NoneState noneState;
		private static readonly SharedState sharedState;
		private static readonly UpdateState updateState;
		private static readonly ExclusiveState exclusiveState;
		#endregion

		#region Public Constructors
		static DataLock()
		{
			noneState = new NoneState();
			sharedState = new SharedState();
			updateState = new UpdateState();
			exclusiveState = new ExclusiveState();
		}

		public DataLock()
		{
		}
		#endregion

		#region Protected Properties
		protected override DataLockType NoneLockType
		{
			get
			{
				return DataLockType.None;
			}
		}
		#endregion

		#region Protected Methods
		protected override State GetStateFromType(DataLockType lockType)
		{
			State state = null;
			switch (lockType)
			{
				case DataLockType.None:
					state = noneState;
					break;
				case DataLockType.Shared:
					state = sharedState;
					break;
				case DataLockType.Update:
					state = updateState;
					break;
				case DataLockType.Exclusive:
					state = exclusiveState;
					break;
			}
			return state;
		}
		#endregion
	}
}
