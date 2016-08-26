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
        protected abstract class RootLockState : State
        {
            public override bool IsExclusiveLock(RootLockType lockType)
            {
                return lockType == RootLockType.Exclusive;
            }
        }

        protected class NoneState : RootLockState
        {
            public override RootLockType Lock => RootLockType.None;

            public override RootLockType[] CompatableLocks =>
                new[]
                {
                    RootLockType.Shared,
                    RootLockType.Update,
                    RootLockType.Exclusive,
                };
        }

        protected class RootSharedState : RootLockState
        {
            public override RootLockType Lock => RootLockType.Shared;

            public override RootLockType[] CompatableLocks =>
                new[]
                {
                    RootLockType.Shared,
                    RootLockType.Update,
                };
        }

        protected class RootUpdateState : RootLockState
        {
            public override RootLockType Lock => RootLockType.Update;

            public override RootLockType[] CompatableLocks =>
                new[]
                {
                    RootLockType.Shared,
                };

            public override bool CanEnterExclusiveLock => true;
        }

        protected class RootExclusiveState : RootLockState
        {
            public override RootLockType Lock => RootLockType.Exclusive;

            public override RootLockType[] CompatableLocks =>
                new RootLockType[0];
        }
        #endregion

        #region Public Constructors
        public RootLock()
        {
        }
        #endregion

        #region Public Properties
        #endregion

        #region Protected Properties
        protected override RootLockType NoneLockType => RootLockType.None;

        #endregion

        #region Protected Methods
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
