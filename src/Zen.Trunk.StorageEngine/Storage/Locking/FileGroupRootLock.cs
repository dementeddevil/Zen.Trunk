using System;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// Defines locking primatives which apply to file-group root pages.
    /// </summary>
    public enum FileGroupRootLockType
    {
        /// <summary>
        /// No locking required (illegal)
        /// </summary>
        None = 0,

        /// <summary>
        /// Shared read access to file-group root page
        /// </summary>
        Shared = 1,

        /// <summary>
        /// Update lock - used to serialise access to exclusive state
        /// </summary>
        Update = 2,

        /// <summary>
        /// Exclusive read/write access to file-group page
        /// </summary>
        Exclusive = 3
    }

    /// <summary>
    /// Implements a child transaction lock for locking file-group root pages.
    /// </summary>
    public class FileGroupRootLock : ChildTransactionLock<FileGroupRootLockType, DatabaseLock>
    {
        #region Private Fields
        private static readonly FileGroupNoneState NoneStateObject = new FileGroupNoneState();
        private static readonly FileGroupSharedState SharedStateObject = new FileGroupSharedState();
        private static readonly FileGroupUpdateState UpdateStateObject = new FileGroupUpdateState();
        private static readonly FileGroupExclusiveState ExclusiveStateObject = new FileGroupExclusiveState();
        #endregion

        #region File-Group Root Lock State
        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="TransactionLock{FileGroupRootLockType}.State" />
        protected abstract class FileGroupRootLockState : State
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
            public override bool IsExclusiveLock(FileGroupRootLockType lockType)
            {
                return lockType == FileGroupRootLockType.Exclusive;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="FileGroupRootLockState" />
        protected class FileGroupNoneState : FileGroupRootLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override FileGroupRootLockType Lock => FileGroupRootLockType.None;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override FileGroupRootLockType[] AllowedLockTypes =>
                new[]
                {
                    FileGroupRootLockType.Shared,
                    FileGroupRootLockType.Update,
                    FileGroupRootLockType.Exclusive,
                };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="FileGroupRootLockState" />
        protected class FileGroupSharedState : FileGroupRootLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override FileGroupRootLockType Lock => FileGroupRootLockType.Shared;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override FileGroupRootLockType[] AllowedLockTypes =>
                new[]
                {
                    FileGroupRootLockType.Shared,
                    FileGroupRootLockType.Update,
                };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="FileGroupRootLockState" />
        protected class FileGroupUpdateState : FileGroupRootLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override FileGroupRootLockType Lock => FileGroupRootLockType.Update;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override FileGroupRootLockType[] AllowedLockTypes =>
                new[]
                {
                    FileGroupRootLockType.Shared,
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
        /// <seealso cref="FileGroupRootLockState" />
        protected class FileGroupExclusiveState : FileGroupRootLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override FileGroupRootLockType Lock => FileGroupRootLockType.Exclusive;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            protected override FileGroupRootLockType[] AllowedLockTypes =>
                new FileGroupRootLockType[0];

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
        protected override FileGroupRootLockType NoneLockType => FileGroupRootLockType.None;
        #endregion

        #region Protected Methods
        /// <summary>
        /// When overridden by derived class, gets the state object from
        /// the specified state type.
        /// </summary>
        /// <param name="lockType">Type of the lock.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected override State GetStateFromType(FileGroupRootLockType lockType)
        {
            switch (lockType)
            {
                case FileGroupRootLockType.None:
                    return NoneStateObject;

                case FileGroupRootLockType.Shared:
                    return SharedStateObject;

                case FileGroupRootLockType.Update:
                    return UpdateStateObject;

                case FileGroupRootLockType.Exclusive:
                    return ExclusiveStateObject;

                default:
                    throw new InvalidOperationException();
            }
        }
        #endregion
    }
}