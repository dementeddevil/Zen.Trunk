namespace Zen.Trunk.Storage.Locking
{
	using System;

	/// <summary>
	/// <c>IDatabaseLockManager</c> represents an abstraction of the lock
	/// manager that is directly associated with a given database instance.
	/// </summary>
	public interface IDatabaseLockManager
	{
		#region Database Lock/Unlock
		void LockDatabase(DatabaseLockType lockType, TimeSpan timeout);
		void UnlockDatabase();
		#endregion

		#region Root Lock/Unlock
		void LockRoot(FileGroupId fileGroupId, RootLockType lockType, TimeSpan timeout);
		void UnlockRoot(FileGroupId fileGroupId);
		RootLock GetRootLock(FileGroupId fileGroupId);
		#endregion

		#region Distribution Page Locks
		void LockDistributionPage(VirtualPageId virtualPageId, ObjectLockType lockType, TimeSpan timeout);
		void UnlockDistributionPage(VirtualPageId virtualPageId);
		void LockDistributionExtent(VirtualPageId virtualPageId, uint extentIndex, ObjectLockType distLockType, DataLockType extentLockType, TimeSpan timeout);
		void UnlockDistributionExtent(VirtualPageId virtualPageId, uint extentIndex);
		void LockDistributionHeader(VirtualPageId virtualPageId, TimeSpan timeout);
		void UnlockDistributionHeader(VirtualPageId virtualPageId);
		ObjectLock GetDistributionLock(VirtualPageId virtualPageId);
		DataLock GetExtentLock(VirtualPageId virtualPageId, uint extentIndex);
		#endregion

		#region Object Lock/Unlock
		void LockObject(ObjectId objectId, ObjectLockType lockType, TimeSpan timeout);
		void UnlockObject(ObjectId objectId);
		ObjectLock GetObjectLock(ObjectId objectId);
		#endregion

		#region Object-Schema Lock/Unlock
		void LockSchema(ObjectId objectId, SchemaLockType lockType, TimeSpan timeout);
		void UnlockSchema(ObjectId objectId);
		SchemaLock GetSchemaLock(ObjectId objectId);
		#endregion

		#region Index Lock/Unlock
		void LockRootIndex(ObjectId ObjectId, TimeSpan timeout, bool writable);
		void UnlockRootIndex(ObjectId ObjectId, bool writable);
		void LockInternalIndex(ObjectId ObjectId, LogicalPageId logicalId, TimeSpan timeout, bool writable);
		void UnlockInternalIndex(ObjectId ObjectId, LogicalPageId logicalId, bool writable);
		#endregion

		#region Data Lock/Unlock
		void LockData(ObjectId objectId, LogicalPageId logicalId, DataLockType lockType, TimeSpan timeout);
		void UnlockData(ObjectId objectId, LogicalPageId logicalId);
		DataLock GetDataLock(ObjectId objectId, LogicalPageId logicalId);
		#endregion

	}
}
