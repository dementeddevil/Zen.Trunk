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
        private static readonly NoneState NoneStateObject = new NoneState();
        private static readonly RootSharedState SharedStateObject = new RootSharedState();
        private static readonly RootUpdateState UpdateStateObject = new RootUpdateState();
        private static readonly RootExclusiveState ExclusiveStateObject = new RootExclusiveState();
        #endregion

        #region Root Lock State
        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="TransactionLock{RootLockType}.State" />
        protected abstract class RootLockState : State
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
            public override bool IsExclusiveLock(RootLockType lockType)
            {
                return lockType == RootLockType.Exclusive;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="RootLockState" />
        protected class NoneState : RootLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override RootLockType Lock => RootLockType.None;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            public override RootLockType[] CompatableLocks =>
                new[]
                {
                    RootLockType.Shared,
                    RootLockType.Update,
                    RootLockType.Exclusive,
                };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="RootLockState" />
        protected class RootSharedState : RootLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override RootLockType Lock => RootLockType.Shared;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            public override RootLockType[] CompatableLocks =>
                new[]
                {
                    RootLockType.Shared,
                    RootLockType.Update,
                };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="RootLockState" />
        protected class RootUpdateState : RootLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override RootLockType Lock => RootLockType.Update;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            public override RootLockType[] CompatableLocks =>
                new[]
                {
                    RootLockType.Shared,
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
        /// <seealso cref="RootLockState" />
        protected class RootExclusiveState : RootLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override RootLockType Lock => RootLockType.Exclusive;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            public override RootLockType[] CompatableLocks =>
                new RootLockType[0];

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

        #region Public Properties
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets enumeration value for the lock representing the "none" lock.
        /// </summary>
        /// <value>
        /// The type of the none lock.
        /// </value>
        protected override RootLockType NoneLockType => RootLockType.None;

        #endregion

        #region Protected Methods
        /// <summary>
        /// When overridden by derived class, gets the state object from
        /// the specified state type.
        /// </summary>
        /// <param name="lockType">Type of the lock.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected override State GetStateFromType(RootLockType lockType)
        {
            switch (lockType)
            {
                case RootLockType.None:
                    return NoneStateObject;

                case RootLockType.Shared:
                    return SharedStateObject;

                case RootLockType.Update:
                    return UpdateStateObject;

                case RootLockType.Exclusive:
                    return ExclusiveStateObject;

                default:
                    throw new InvalidOperationException();
            }
        }
        #endregion
    }
}
