namespace Zen.Trunk.Storage.Locking
{
	/// <summary>
	/// Defines object level lock types.
	/// </summary>
	public enum ObjectLockType
	{
		/// <summary>
		/// No locking required.
		/// </summary>
		None = 0,

		/// <summary>
		/// Transaction intents reading one or more pages owned by this object.
		/// </summary>
		IntentShared = 1,

		/// <summary>
		/// Transaction intends on reading all pages owned by this object.
		/// </summary>
		Shared = 2,

		/// <summary>
		/// Transaction intends to modify one or more (but not all) pages
		/// owned by this object.
		/// </summary>
		IntentExclusive = 3,

		/// <summary>
		/// Transaction has shared and intends to modify one or more pages
		/// owned by this object.
		/// </summary>
		SharedIntentExclusive = 4,

		/// <summary>
		/// Transaction has exclusive use of this object and all pages owned
		/// by it.
		/// </summary>
		Exclusive = 5,
	}

	/// <summary>
	/// Implements a general page lock suitable for root, table, sample,
	/// data and index leaf pages.
	/// </summary>
	public class ObjectLock : ChildTransactionLock<ObjectLockType, DatabaseLock>
	{
		#region Object Locking State
		protected abstract class ObjectLockState : State
		{
			public override bool IsExclusiveLock(ObjectLockType lockType)
			{
				return lockType == ObjectLockType.Exclusive;
			}
		}
		protected class NoneState : ObjectLockState
		{
			public override ObjectLockType Lock
			{
				get
				{
					return ObjectLockType.None;
				}
			}

			public override ObjectLockType[] CompatableLocks
			{
				get
				{
					return new ObjectLockType[] 
					{
						ObjectLockType.IntentShared,
						ObjectLockType.Shared,
						ObjectLockType.IntentExclusive,
						ObjectLockType.SharedIntentExclusive,
						ObjectLockType.Exclusive,
					};
				}
			}
		}
		protected class IntentSharedState : ObjectLockState
		{
			public override ObjectLockType Lock
			{
				get
				{
					return ObjectLockType.IntentShared;
				}
			}

			public override ObjectLockType[] CompatableLocks
			{
				get
				{
					return new ObjectLockType[]
					{
						ObjectLockType.IntentShared,
						ObjectLockType.Shared,
						ObjectLockType.IntentExclusive,
						ObjectLockType.SharedIntentExclusive,
					};
				}
			}
		}
		protected class SharedState : ObjectLockState
		{
			public override ObjectLockType Lock
			{
				get
				{
					return ObjectLockType.Shared;
				}
			}

			public override ObjectLockType[] CompatableLocks
			{
				get
				{
					return new ObjectLockType[] 
					{
						ObjectLockType.IntentShared,
						ObjectLockType.Shared,
					};
				}
			}
		}
		protected class IntentExclusiveState : ObjectLockState
		{
			public override ObjectLockType Lock
			{
				get
				{
					return ObjectLockType.IntentExclusive;
				}
			}

			public override ObjectLockType[] CompatableLocks
			{
				get
				{
					return new ObjectLockType[] 
					{
						ObjectLockType.IntentShared,
						ObjectLockType.IntentExclusive,
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
		protected class SharedIntentExclusiveState : ObjectLockState
		{
			public override ObjectLockType Lock
			{
				get
				{
					return ObjectLockType.SharedIntentExclusive;
				}
			}

			public override ObjectLockType[] CompatableLocks
			{
				get
				{
					return new ObjectLockType[]
					{
						ObjectLockType.IntentShared,
					};
				}
			}
		}
		protected class ExclusiveState : ObjectLockState
		{
			public override ObjectLockType Lock
			{
				get
				{
					return ObjectLockType.Exclusive;
				}
			}

			public override ObjectLockType[] CompatableLocks
			{
				get
				{
					return new ObjectLockType[0];
				}
			}
		}
		#endregion

		#region Private Fields
		private static readonly NoneState noneState;
		private static readonly IntentSharedState intentSharedState;
		private static readonly SharedState sharedState;
		private static readonly IntentExclusiveState intentExclusiveState;
		private static readonly SharedIntentExclusiveState sharedIntentExclusiveState;
		private static readonly ExclusiveState exclusiveState;
		#endregion

		#region Public Constructors
		static ObjectLock()
		{
			noneState = new NoneState();
			intentSharedState = new IntentSharedState();
			sharedState = new SharedState();
			intentExclusiveState = new IntentExclusiveState();
			sharedIntentExclusiveState = new SharedIntentExclusiveState();
			exclusiveState = new ExclusiveState();
		}

		public ObjectLock()
		{
		}
		#endregion

		#region Protected Properties
		protected override ObjectLockType NoneLockType
		{
			get
			{
				return ObjectLockType.None;
			}
		}
		#endregion

		#region Protected Methods
		protected override State GetStateFromType(ObjectLockType objectLockType)
		{
			State state = null;
			switch (objectLockType)
			{
				case ObjectLockType.None:
					state = noneState;
					break;
				case ObjectLockType.IntentShared:
					state = intentSharedState;
					break;
				case ObjectLockType.Shared:
					state = sharedState;
					break;
				case ObjectLockType.IntentExclusive:
					state = intentExclusiveState;
					break;
				case ObjectLockType.SharedIntentExclusive:
					state = sharedIntentExclusiveState;
					break;
				case ObjectLockType.Exclusive:
					state = exclusiveState;
					break;
			}
			return state;
		}
		#endregion
	}
}
