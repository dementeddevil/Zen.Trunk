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
	/// Implements a lock for locking database objects and is the root for
	/// all child lock objects.
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
        /// <summary>
        /// Represents the base state object for all database lock states.
        /// </summary>
        /// <seealso cref="TransactionLock{DatabaseLockType}.State" />
        protected abstract class DatabaseLockState : State
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
            public override bool IsExclusiveLock(DatabaseLockType lockType)
			{
				return lockType == DatabaseLockType.Exclusive;
			}
		}

        /// <summary>
        /// Represents the state used when no lock is currently held.
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.DatabaseLock.DatabaseLockState" />
        protected class NoneState : DatabaseLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override DatabaseLockType Lock => DatabaseLockType.None;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override DatabaseLockType[] AllowedLockTypes =>
                new[] 
		        {
		            DatabaseLockType.Shared,
		            DatabaseLockType.Update
		        };

            /// <summary>
            /// Determines whether the specified request can acquire the lock.
            /// </summary>
            /// <param name="owner">The owner.</param>
            /// <param name="request">The request.</param>
            /// <returns>
            /// <c>true</c> if the request can acquire lock; otherwise,
            /// <c>false</c>.
            /// </returns>
            public override bool CanAcquireLock(
				TransactionLock<DatabaseLockType> owner,
				AcquireLock request)
			{
				return true;
			}
		}

        /// <summary>
        /// Represents state used to when shared access to the database is held
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.DatabaseLock.DatabaseLockState" />
        protected class SharedState : DatabaseLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override DatabaseLockType Lock => DatabaseLockType.Shared;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override DatabaseLockType[] AllowedLockTypes =>
                new[] 
		        {
		            DatabaseLockType.Shared,
		            DatabaseLockType.Update,
		        };
		}

        /// <summary>
        /// Represents the state used when update access to the database is held
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.DatabaseLock.DatabaseLockState" />
        protected class UpdateState : DatabaseLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override DatabaseLockType Lock => DatabaseLockType.Update;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override DatabaseLockType[] AllowedLockTypes =>
                new[] 
		        {
		            DatabaseLockType.Shared
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
        /// Represents the state when exclusive access to the database is held
        /// </summary>
        /// <seealso cref="Zen.Trunk.Storage.Locking.DatabaseLock.DatabaseLockState" />
        protected class ExclusiveState : DatabaseLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override DatabaseLockType Lock => DatabaseLockType.Exclusive;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override DatabaseLockType[] AllowedLockTypes =>
                new DatabaseLockType[0];

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

        #region Protected Properties
        /// <summary>
        /// Gets enumeration value for the lock representing the "none" lock.
        /// </summary>
        /// <value>
        /// The type of the none lock.
        /// </value>
        protected override DatabaseLockType NoneLockType => DatabaseLockType.None;
        #endregion

        #region Protected Methods
        /// <summary>
        /// When overridden by derived class, gets the state object from
        /// the specified state type.
        /// </summary>
        /// <param name="lockType">Type of the lock.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
