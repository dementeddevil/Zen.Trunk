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
		void LockRoot(byte fileGroupId, RootLockType lockType, TimeSpan timeout);
		void UnlockRoot(byte fileGroupId);
		RootLock GetRootLock(byte fileGroupId);
		#endregion

		#region Distribution Page Locks
		void LockDistributionPage(ulong virtualPageId, ObjectLockType lockType, TimeSpan timeout);
		void UnlockDistributionPage(ulong virtualPageId);
		void LockDistributionExtent(ulong virtualPageId, uint extentIndex, ObjectLockType distLockType, DataLockType extentLockType, TimeSpan timeout);
		void UnlockDistributionExtent(ulong virtualPageId, uint extentIndex);
		void LockDistributionHeader(ulong virtualPageId, TimeSpan timeout);
		void UnlockDistributionHeader(ulong virtualPageId);
		ObjectLock GetDistributionLock(ulong virtualPageId);
		DataLock GetExtentLock(ulong virtualPageId, uint extentIndex);
		#endregion

		#region Object Lock/Unlock
		void LockObject(uint objectId, ObjectLockType lockType, TimeSpan timeout);
		void UnlockObject(uint objectId);
		ObjectLock GetObjectLock(uint objectId);
		#endregion

		#region Object-Schema Lock/Unlock
		void LockSchema(uint objectId, SchemaLockType lockType, TimeSpan timeout);
		void UnlockSchema(uint objectId);
		SchemaLock GetSchemaLock(uint objectId);
		#endregion

		#region Index Lock/Unlock
		void LockRootIndex(uint indexId, TimeSpan timeout, bool writable);
		void UnlockRootIndex(uint indexId, bool writable);
		void LockInternalIndex(uint indexId, ulong logicalId, TimeSpan timeout, bool writable);
		void UnlockInternalIndex(uint indexId, ulong logicalId, bool writable);
		#endregion

		#region Data Lock/Unlock
		void LockData(uint objectId, ulong logicalId, DataLockType lockType, TimeSpan timeout);
		void UnlockData(uint objectId, ulong logicalId);
		DataLock GetDataLock(uint objectId, ulong logicalId);
		#endregion

	}
}
