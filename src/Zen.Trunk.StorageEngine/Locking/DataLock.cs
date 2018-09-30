using System;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
	/// Implements a general page lock suitable for root, table, sample,
	/// data and index leaf pages.
	/// </summary>
	public class DataLock : ChildTransactionLock<DataLockType, ObjectLockType>, IDataLock
	{
        #region Data Locking State
        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="TransactionLock{DataLockType}.State" />
        protected abstract class DataLockState : State
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
            public override bool IsExclusiveLock(DataLockType lockType)
			{
				return lockType == DataLockType.Exclusive;
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.DataLock.DataLockState" />
        protected class NoneState : DataLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override DataLockType Lock => DataLockType.None;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override DataLockType[] AllowedLockTypes =>
                new[]
		        {
		            DataLockType.Shared,
		            DataLockType.Update,
		            DataLockType.Exclusive,
		        };
		}

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.DataLock.DataLockState" />
        protected class SharedState : DataLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override DataLockType Lock => DataLockType.Shared;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override DataLockType[] AllowedLockTypes =>
                new[]
		        {
		            DataLockType.Shared,
		            DataLockType.Update,
		        };
		}

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.DataLock.DataLockState" />
        protected class UpdateState : DataLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override DataLockType Lock => DataLockType.Update;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override DataLockType[] AllowedLockTypes =>
                new[] 
		        {
		            DataLockType.Shared,
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
        /// <seealso cref="Zen.Trunk.Storage.Locking.DataLock.DataLockState" />
        protected class ExclusiveState : DataLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override DataLockType Lock => DataLockType.Exclusive;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override DataLockType[] AllowedLockTypes =>
                new DataLockType[0];

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
		private static readonly SharedState SharedStateObject = new SharedState();
		private static readonly UpdateState UpdateStateObject = new UpdateState();
		private static readonly ExclusiveState ExclusiveStateObject = new ExclusiveState();
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets enumeration value for the lock representing the "none" lock.
        /// </summary>
        /// <value>
        /// The type of the none lock.
        /// </value>
        protected override DataLockType NoneLockType => DataLockType.None;
        #endregion

        #region Protected Methods
        /// <summary>
        /// When overridden by derived class, gets the state object from
        /// the specified state type.
        /// </summary>
        /// <param name="lockType">Type of the lock.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
