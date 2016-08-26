using System;

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
			public override ObjectLockType Lock => ObjectLockType.None;

		    public override ObjectLockType[] CompatableLocks =>
                new[] 
		        {
		            ObjectLockType.IntentShared,
		            ObjectLockType.Shared,
		            ObjectLockType.IntentExclusive,
		            ObjectLockType.SharedIntentExclusive,
		            ObjectLockType.Exclusive,
		        };
		}

        protected class IntentSharedState : ObjectLockState
		{
			public override ObjectLockType Lock => ObjectLockType.IntentShared;

		    public override ObjectLockType[] CompatableLocks =>
                new[]
		        {
		            ObjectLockType.IntentShared,
		            ObjectLockType.Shared,
		            ObjectLockType.IntentExclusive,
		            ObjectLockType.SharedIntentExclusive,
		        };
		}

        protected class SharedState : ObjectLockState
		{
			public override ObjectLockType Lock => ObjectLockType.Shared;

		    public override ObjectLockType[] CompatableLocks => 
                new[]
		        {
		            ObjectLockType.IntentShared,
		            ObjectLockType.Shared,
		        };
		}

        protected class IntentExclusiveState : ObjectLockState
		{
			public override ObjectLockType Lock => ObjectLockType.IntentExclusive;

		    public override ObjectLockType[] CompatableLocks =>
                new[] 
		        {
		            ObjectLockType.IntentShared,
		            ObjectLockType.IntentExclusive,
		        };

		    public override bool CanEnterExclusiveLock => true;
		}

        protected class SharedIntentExclusiveState : ObjectLockState
		{
			public override ObjectLockType Lock => ObjectLockType.SharedIntentExclusive;

		    public override ObjectLockType[] CompatableLocks =>
                new[]
		        {
		            ObjectLockType.IntentShared,
		        };
		}

        protected class ExclusiveState : ObjectLockState
		{
			public override ObjectLockType Lock => ObjectLockType.Exclusive;

		    public override ObjectLockType[] CompatableLocks =>
                new ObjectLockType[0];
		}
		#endregion

		#region Private Fields
		private static readonly NoneState NoneStateObject = new NoneState();
		private static readonly IntentSharedState IntentSharedStateObject = new IntentSharedState();
		private static readonly SharedState SharedStateObject = new SharedState();
		private static readonly IntentExclusiveState IntentExclusiveStateObject = new IntentExclusiveState();
		private static readonly SharedIntentExclusiveState SharedIntentExclusiveStateObject = new SharedIntentExclusiveState();
		private static readonly ExclusiveState ExclusiveStateObject = new ExclusiveState();
		#endregion

		#region Public Constructors
		public ObjectLock()
		{
		}
		#endregion

		#region Protected Properties
		protected override ObjectLockType NoneLockType => ObjectLockType.None;
	    #endregion

		#region Protected Methods
		protected override State GetStateFromType(ObjectLockType objectLockType)
		{
			switch (objectLockType)
			{
				case ObjectLockType.None:
					return NoneStateObject;

				case ObjectLockType.IntentShared:
                    return IntentSharedStateObject;

				case ObjectLockType.Shared:
                    return SharedStateObject;

				case ObjectLockType.IntentExclusive:
                    return IntentExclusiveStateObject;

				case ObjectLockType.SharedIntentExclusive:
                    return SharedIntentExclusiveStateObject;

				case ObjectLockType.Exclusive:
                    return ExclusiveStateObject;

                default:
                    throw new InvalidOperationException();
            }
		}
		#endregion
	}
}
