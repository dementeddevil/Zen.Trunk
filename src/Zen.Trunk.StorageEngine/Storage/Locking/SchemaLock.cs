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
		protected abstract class SchemaLockState : State
		{
			public override bool IsExclusiveLock(SchemaLockType lockType)
			{
				return lockType == SchemaLockType.SchemaModification;
			}
		}
		protected class NoneState : SchemaLockState
		{
			public override SchemaLockType Lock => SchemaLockType.None;

		    public override SchemaLockType[] CompatableLocks => new[]
		    {
		        SchemaLockType.SchemaStability,
		        SchemaLockType.BulkUpdate,
		        SchemaLockType.SchemaModification,
		    };
		}
		protected class SchemaStabilityState : SchemaLockState
		{
			public override SchemaLockType Lock => SchemaLockType.SchemaStability;

		    public override SchemaLockType[] CompatableLocks => new[]
			{
			    SchemaLockType.SchemaStability,
			    SchemaLockType.BulkUpdate,
			};
		}
		protected class BulkUpdateState : SchemaLockState
		{
			public override SchemaLockType Lock => SchemaLockType.BulkUpdate;

		    public override SchemaLockType[] CompatableLocks => new[]
            {
		        SchemaLockType.SchemaStability,
		        SchemaLockType.BulkUpdate,
		    };
		}
		protected class SchemaModificationState : SchemaLockState
		{
			public override SchemaLockType Lock => SchemaLockType.SchemaModification;

		    public override SchemaLockType[] CompatableLocks => new SchemaLockType[0];

            public override bool CanEnterExclusiveLock => true;
        }
        #endregion

		#region Protected Properties
		protected override SchemaLockType NoneLockType => SchemaLockType.None;
	    #endregion

		#region Protected Methods
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
