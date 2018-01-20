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
        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="TransactionLock{ObjectLockType}.State" />
        protected abstract class ObjectLockState : State
		{
            /// <summary>
            /// Determines whether the specified lock type is equivalent to an
            /// exclusive lock.
            /// </summary>
            /// <param name="lockType">Type of the lock.</param>
            /// <returns>
            /// <c>true</c> if the lock type is an exclusive lock; otherwise,
            /// <c>false</c>.
            /// </returns>
            public override bool IsExclusiveLock(ObjectLockType lockType)
			{
				return lockType == ObjectLockType.Exclusive;
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.ObjectLock.ObjectLockState" />
        protected class NoneState : ObjectLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override ObjectLockType Lock => ObjectLockType.None;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override ObjectLockType[] AllowedLockTypes =>
                new[]
                {
                    ObjectLockType.IntentShared,
                    ObjectLockType.Shared,
                    ObjectLockType.IntentExclusive,
                    ObjectLockType.SharedIntentExclusive,
                    ObjectLockType.Exclusive,
                };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.ObjectLock.ObjectLockState" />
        protected class IntentSharedState : ObjectLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override ObjectLockType Lock => ObjectLockType.IntentShared;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override ObjectLockType[] AllowedLockTypes =>
                new[]
		        {
		            ObjectLockType.IntentShared,
		            ObjectLockType.Shared,
		            ObjectLockType.IntentExclusive,
		            ObjectLockType.SharedIntentExclusive,
		        };
		}

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.ObjectLock.ObjectLockState" />
        protected class SharedState : ObjectLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override ObjectLockType Lock => ObjectLockType.Shared;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override ObjectLockType[] AllowedLockTypes => 
                new[]
		        {
		            ObjectLockType.IntentShared,
		            ObjectLockType.Shared,
		        };
		}

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.ObjectLock.ObjectLockState" />
        protected class IntentExclusiveState : ObjectLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override ObjectLockType Lock => ObjectLockType.IntentExclusive;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override ObjectLockType[] AllowedLockTypes =>
                new[] 
		        {
		            ObjectLockType.IntentShared,
		            ObjectLockType.IntentExclusive,
		        };

            /// <summary>
            /// Gets a boolean value indicating whether an exclusive lock can
            /// be acquired from this state.
            /// </summary>
            /// <value>
            /// <c>true</c> if an exclusive lock can be acquired from this
            /// state; otherwise, <c>false</c>. The default is <c>false</c>.
            /// </value>
            public override bool CanEnterExclusiveLock => true;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.ObjectLock.ObjectLockState" />
        protected class SharedIntentExclusiveState : ObjectLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override ObjectLockType Lock => ObjectLockType.SharedIntentExclusive;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override ObjectLockType[] AllowedLockTypes =>
                new[]
		        {
		            ObjectLockType.IntentShared,
		        };
		}

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.ObjectLock.ObjectLockState" />
        protected class ExclusiveState : ObjectLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override ObjectLockType Lock => ObjectLockType.Exclusive;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override ObjectLockType[] AllowedLockTypes =>
                new ObjectLockType[0];

            /// <summary>
            /// Gets a boolean value indicating whether an exclusive lock can
            /// be acquired from this state.
            /// </summary>
            /// <value>
            /// <c>true</c> if an exclusive lock can be acquired from this
            /// state; otherwise, <c>false</c>. The default is <c>false</c>.
            /// </value>
            public override bool CanEnterExclusiveLock => true;
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

        #region Protected Properties
        /// <summary>
        /// Gets enumeration value for the lock representing the "none" lock.
        /// </summary>
        /// <value>
        /// The type of the none lock.
        /// </value>
        protected override ObjectLockType NoneLockType => ObjectLockType.None;
        #endregion

        #region Protected Methods
        /// <summary>
        /// Gets the type of the state from.
        /// </summary>
        /// <param name="objectLockType">Type of the object lock.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
