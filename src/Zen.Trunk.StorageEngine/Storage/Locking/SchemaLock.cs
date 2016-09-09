namespace Zen.Trunk.Storage.Locking
{
	using System;

	/// <summary>
	/// Defines locking primatives which apply to database objects.
	/// </summary>
	/// <remarks>
	/// In relation to tables, this lock type applies to the row schema
	/// definition.
	/// In relation to samples, this lock type applies to the media
	/// format information.
	/// </remarks>
	public enum SchemaLockType
	{
		/// <summary>
		/// No locking required.
		/// </summary>
		None = 0,

		/// <summary>
		/// Schema is locked for reading
		/// </summary>
		/// <remarks>
		/// Compatability: BulkUpdate and SchemaStability
		/// </remarks>
		SchemaStability = 1,

		/// <summary>
		/// Schema is locked due to bulk update
		/// </summary>
		/// <remarks>
		/// Compatability: BulkUpdate and SchemaStability
		/// </remarks>
		BulkUpdate = 2,

		/// <summary>
		/// Schema is locked to all.
		/// </summary>
		/// <remarks>
		/// Compatability: None
		/// </remarks>
		SchemaModification = 3,
	}

	/// <summary>
	/// Implements a schema lock for locking table schema and 
	/// sample wave format blocks.
	/// </summary>
	public class SchemaLock : ChildTransactionLock<SchemaLockType, ObjectLock>
	{
		#region Private Fields
		private static readonly NoneState NoneStateObject = new NoneState();
		private static readonly SchemaStabilityState SchemaStabilityStateObject = new SchemaStabilityState();
		private static readonly BulkUpdateState BulkUpdateStateObject = new BulkUpdateState();
		private static readonly SchemaModificationState SchemaModificationStateObject = new SchemaModificationState();
        #endregion

        #region Schema Lock State
        /// <summary>
        /// Represents the base state from which all schema lock states are derived.
        /// </summary>
        /// <seealso cref="TransactionLock{SchemaLockType}.State" />
        protected abstract class SchemaLockState : State
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
            public override bool IsExclusiveLock(SchemaLockType lockType)
			{
				return lockType == SchemaLockType.SchemaModification;
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="SchemaLockState" />
        protected class NoneState : SchemaLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override SchemaLockType Lock => SchemaLockType.None;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            public override SchemaLockType[] CompatableLocks => new[]
		    {
		        SchemaLockType.SchemaStability,
		        SchemaLockType.BulkUpdate,
		        SchemaLockType.SchemaModification,
		    };
		}

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="SchemaLockState" />
        protected class SchemaStabilityState : SchemaLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override SchemaLockType Lock => SchemaLockType.SchemaStability;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            public override SchemaLockType[] CompatableLocks => new[]
			{
			    SchemaLockType.SchemaStability,
			    SchemaLockType.BulkUpdate,
			};
		}

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="SchemaLockState" />
        protected class BulkUpdateState : SchemaLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override SchemaLockType Lock => SchemaLockType.BulkUpdate;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            public override SchemaLockType[] CompatableLocks => new[]
            {
		        SchemaLockType.SchemaStability,
		        SchemaLockType.BulkUpdate,
		    };
		}
        
        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="SchemaLockState" />
        protected class SchemaModificationState : SchemaLockState
		{
            /// <summary>
            /// Gets the lock type that this state represents.
            /// </summary>
            /// <value>
            /// The lock.
            /// </value>
            public override SchemaLockType Lock => SchemaLockType.SchemaModification;

            /// <summary>
            /// Gets an array of lock types that this state is compatable with.
            /// </summary>
            /// <value>
            /// The compatable locks.
            /// </value>
            public override SchemaLockType[] CompatableLocks => new SchemaLockType[0];

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
        protected override SchemaLockType NoneLockType => SchemaLockType.None;
        #endregion

        #region Protected Methods
        /// <summary>
        /// When overridden by derived class, gets the state object from
        /// the specified state type.
        /// </summary>
        /// <param name="lockType">Type of the lock.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected override State GetStateFromType (SchemaLockType lockType)
		{
			switch (lockType)
			{
				case SchemaLockType.None:
					return NoneStateObject;

				case SchemaLockType.SchemaStability:
					return SchemaStabilityStateObject;

				case SchemaLockType.BulkUpdate:
					return BulkUpdateStateObject;

				case SchemaLockType.SchemaModification:
					return SchemaModificationStateObject;

				default:
					throw new InvalidOperationException ();
			}
		}
		#endregion
	}
}
