namespace Zen.Trunk.Storage.Locking
{
	using System;

	/// <summary>
	/// Defines locking primatives which apply to root pages.
	/// </summary>
	public enum RootLockType
	{
		/// <summary>
		/// No locking required (illegal)
		/// </summary>
		None = 0,

		/// <summary>
		/// Shared read access to root page
		/// </summary>
		Shared = 1,

		/// <summary>
		/// Update lock - used to serialise access to exclusive state
		/// </summary>
		Update = 2,

		/// <summary>
		/// Exclusive read/write access to root page
		/// </summary>
		Exclusive = 3
	}

	/// <summary>
	/// Implements a schema lock for locking table schema and 
	/// sample wave format blocks.
	/// </summary>
	public class RootLock : ChildTransactionLock<RootLockType, DatabaseLock>
	{
		#region Private Fields
		private static readonly NoneState noneState;
		private static readonly RootSharedState sharedState;
		private static readonly RootUpdateState updateState;
		private static readonly RootExclusiveState exclusiveState;
		#endregion

		#region Root Lock State
		protected abstract class RootLockState : State
		{
			public override bool IsExclusiveLock(RootLockType lockType)
			{
				return lockType == RootLockType.Exclusive;
			}
		}
		protected class NoneState : RootLockState
		{
			public override RootLockType Lock
			{
				get
				{
					return RootLockType.None;
				}
			}

			public override RootLockType[] CompatableLocks
			{
				get
				{
					return new RootLockType[3] 
					{
						RootLockType.Shared,
						RootLockType.Update,
						RootLockType.Exclusive,
					};
				}
			}
		}
		protected class RootSharedState : RootLockState
		{
			public override RootLockType Lock
			{
				get
				{
					return RootLockType.Shared;
				}
			}

			public override RootLockType[] CompatableLocks
			{
				get
				{
					return new RootLockType[] 
					{
						RootLockType.Shared,
						RootLockType.Update,
					};
				}
			}
		}
		protected class RootUpdateState : RootLockState
		{
			public override RootLockType Lock
			{
				get
				{
					return RootLockType.Update;
				}
			}

			public override RootLockType[] CompatableLocks
			{
				get
				{
					return new RootLockType[] 
					{
						RootLockType.Shared,
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
		protected class RootExclusiveState : RootLockState
		{
			public override RootLockType Lock
			{
				get
				{
					return RootLockType.Exclusive;
				}
			}

			public override RootLockType[] CompatableLocks
			{
				get
				{
					return new RootLockType[0];
				}
			}
		}
		#endregion

		#region Public Constructors
		static RootLock()
		{
			noneState = new NoneState();
			sharedState = new RootSharedState();
			updateState = new RootUpdateState();
			exclusiveState = new RootExclusiveState();
		}

		public RootLock()
		{
		}
		#endregion

		#region Public Properties
		#endregion

		#region Protected Properties
		protected override RootLockType NoneLockType
		{
			get
			{
				return RootLockType.None;
			}
		}
		#endregion

		#region Protected Methods
		protected override TransactionLock<RootLockType>.State GetStateFromType(RootLockType lockType)
		{
			switch (lockType)
			{
				case RootLockType.None:
					return noneState;

				case RootLockType.Shared:
					return sharedState;

				case RootLockType.Update:
					return updateState;

				case RootLockType.Exclusive:
					return exclusiveState;

				default:
					throw new InvalidOperationException();
			}
		}
		#endregion
	}
}
