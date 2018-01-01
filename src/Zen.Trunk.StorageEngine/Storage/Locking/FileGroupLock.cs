using System;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// Defines locking primatives which apply to file-group root pages.
    /// </summary>
    public enum FileGroupLockType
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
    /// Implements a schema lock for locking table schema and 
    /// sample wave format blocks.
    /// </summary>
    public class FileGroupLock : ChildTransactionLock<FileGroupLockType, DatabaseLock>
    {
        #region Private Fields
        private static readonly FileGroupNoneState NoneStateObject = new FileGroupNoneState();
        private static readonly FileGroupSharedState SharedStateObject = new FileGroupSharedState();
        private static readonly FileGroupUpdateState UpdateStateObject = new FileGroupUpdateState();
        private static readonly FileGroupExclusiveState ExclusiveStateObject = new FileGroupExclusiveState();
        #endregion

        #region File-Group Lock State
        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="TransactionLock{FileGroupLockType}.State" />
        protected abstract class FileGroupLockState : State
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
            public override bool IsExclusiveLock(FileGroupLockType lockType)
            {
                return lockType == FileGroupLockType.Exclusive;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="FileGroupLockState" />
        protected class FileGroupNoneState : FileGroupLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override FileGroupLockType Lock => FileGroupLockType.None;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            public override FileGroupLockType[] CompatableLocks =>
                new[]
                {
                    FileGroupLockType.Shared,
                    FileGroupLockType.Update,
                    FileGroupLockType.Exclusive,
                };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="FileGroupLockState" />
        protected class FileGroupSharedState : FileGroupLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override FileGroupLockType Lock => FileGroupLockType.Shared;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            public override FileGroupLockType[] CompatableLocks =>
                new[]
                {
                    FileGroupLockType.Shared,
                    FileGroupLockType.Update,
                };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="FileGroupLockState" />
        protected class FileGroupUpdateState : FileGroupLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override FileGroupLockType Lock => FileGroupLockType.Update;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            public override FileGroupLockType[] CompatableLocks =>
                new[]
                {
                    FileGroupLockType.Shared,
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
        /// <seealso cref="FileGroupLockState" />
        protected class FileGroupExclusiveState : FileGroupLockState
        {
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override FileGroupLockType Lock => FileGroupLockType.Exclusive;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            public override FileGroupLockType[] CompatableLocks =>
                new FileGroupLockType[0];

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
        protected override FileGroupLockType NoneLockType => FileGroupLockType.None;
        #endregion

        #region Protected Methods
        /// <summary>
        /// When overridden by derived class, gets the state object from
        /// the specified state type.
        /// </summary>
        /// <param name="lockType">Type of the lock.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected override State GetStateFromType(FileGroupLockType lockType)
        {
            switch (lockType)
            {
                case FileGroupLockType.None:
                    return NoneStateObject;

                case FileGroupLockType.Shared:
                    return SharedStateObject;

                case FileGroupLockType.Update:
                    return UpdateStateObject;

                case FileGroupLockType.Exclusive:
                    return ExclusiveStateObject;

                default:
                    throw new InvalidOperationException();
            }
        }
        #endregion
    }
}