namespace Zen.Trunk.Storage.Locking
{
	using System;

	/// <summary>
	/// <c>DatabaseLockManager</c> represents an abstraction of the lock manager
	/// that is directly associated with a given database instance.
	/// </summary>
	public class DatabaseLockManager : IDatabaseLockManager
	{
		#region Private Fields
		private readonly IGlobalLockManager _globalLockManager;
		private readonly DatabaseId _dbId;
		#endregion

		#region Public Constructors
		public DatabaseLockManager(IGlobalLockManager globalLockManager, DatabaseId dbId)
		{
			if (globalLockManager == null)
			{
				throw new ArgumentNullException(nameof(globalLockManager));
			}

		    if (dbId == DatabaseId.Zero)
		    {
		        throw new ArgumentNullException(nameof(dbId));
		    }

			_globalLockManager = globalLockManager;
			_dbId = dbId;
		}
		#endregion

		#region Public Methods
		#region Database Lock/Unlock
		public void LockDatabase(DatabaseLockType lockType, TimeSpan timeout)
		{
			_globalLockManager.LockDatabase(_dbId, lockType, timeout);
		}

		public void UnlockDatabase()
		{
			_globalLockManager.UnlockDatabase(_dbId);
		}
		#endregion

		#region Root Lock/Unlock
		public void LockRoot(FileGroupId fileGroupId, RootLockType lockType, TimeSpan timeout)
		{
			_globalLockManager.LockRoot(_dbId, fileGroupId, lockType, timeout);
		}

		public void UnlockRoot(FileGroupId fileGroupId)
		{
			_globalLockManager.UnlockRoot(_dbId, fileGroupId);
		}

		public RootLock GetRootLock(FileGroupId fileGroupId)
		{
			return _globalLockManager.GetRootLock(_dbId, fileGroupId);
		}
		#endregion

		#region Distribution Page Locks
		public void LockDistributionPage(VirtualPageId virtualPageId, ObjectLockType lockType, TimeSpan timeout)
		{
			_globalLockManager.LockDistributionPage(_dbId, virtualPageId, lockType, timeout);
		}

		public void UnlockDistributionPage(VirtualPageId virtualPageId)
		{
			_globalLockManager.UnlockDistributionPage(_dbId, virtualPageId);
		}

		public void LockDistributionExtent(VirtualPageId virtualPageId, uint extentIndex,
			ObjectLockType distLockType, DataLockType extentLockType, TimeSpan timeout)
		{
			_globalLockManager.LockDistributionExtent(_dbId, virtualPageId, extentIndex, distLockType, extentLockType, timeout);
		}

		public void UnlockDistributionExtent(VirtualPageId virtualPageId, uint extentIndex)
		{
			_globalLockManager.UnlockDistributionExtent(_dbId, virtualPageId, extentIndex);
		}

		public void LockDistributionHeader(VirtualPageId virtualPageId, TimeSpan timeout)
		{
			_globalLockManager.LockDistributionHeader(_dbId, virtualPageId, timeout);
		}

		public void UnlockDistributionHeader(VirtualPageId virtualPageId)
		{
			_globalLockManager.UnlockDistributionHeader(_dbId, virtualPageId);
		}

		public ObjectLock GetDistributionLock(VirtualPageId virtualPageId)
		{
			return _globalLockManager.GetDistributionLock(_dbId, virtualPageId);
		}

		public DataLock GetExtentLock(VirtualPageId virtualPageId, uint extentIndex)
		{
			return _globalLockManager.GetExtentLock(_dbId, virtualPageId, extentIndex);
		}
		#endregion

		#region Object Lock/Unlock
		public void LockObject(ObjectId objectId, ObjectLockType lockType, TimeSpan timeout)
		{
			_globalLockManager.LockObject(_dbId, objectId, lockType, timeout);
		}

		public void UnlockObject(ObjectId objectId)
		{
			_globalLockManager.UnlockObject(_dbId, objectId);
		}

		public ObjectLock GetObjectLock(ObjectId objectId)
		{
			return _globalLockManager.GetObjectLock(_dbId, objectId);
		}
		#endregion

		#region Object-Schema Lock/Unlock
		public void LockSchema(ObjectId objectId, SchemaLockType lockType, TimeSpan timeout)
		{
			_globalLockManager.LockSchema(_dbId, objectId, lockType, timeout);
		}

		public void UnlockSchema(ObjectId objectId)
		{
			_globalLockManager.UnlockSchema(_dbId, objectId);
		}

		public SchemaLock GetSchemaLock(ObjectId objectId)
		{
			return _globalLockManager.GetSchemaLock(_dbId, objectId);
		}
		#endregion

		#region Index Lock/Unlock
		public void LockRootIndex(ObjectId objectId, bool writable, TimeSpan timeout)
		{
			_globalLockManager.LockRootIndex(_dbId, objectId, writable, timeout);
		}

		public void UnlockRootIndex(ObjectId objectId, bool writable)
		{
			_globalLockManager.UnlockRootIndex(_dbId, objectId, writable);
		}

		public void LockInternalIndex(ObjectId objectId, LogicalPageId logicalId, bool writable, TimeSpan timeout)
		{
			_globalLockManager.LockInternalIndex(_dbId, objectId, logicalId, writable, timeout);
		}

		public void UnlockInternalIndex(ObjectId objectId, LogicalPageId logicalId, bool writable)
		{
			_globalLockManager.UnlockInternalIndex(_dbId, objectId, logicalId, writable);
		}
		#endregion

		#region Data Lock/Unlock
		public void LockData(ObjectId objectId, LogicalPageId logicalId, DataLockType lockType, TimeSpan timeout)
		{
			_globalLockManager.LockData(_dbId, objectId, logicalId, lockType, timeout);
		}

		public void UnlockData(ObjectId objectId, LogicalPageId logicalId)
		{
			_globalLockManager.UnlockData(_dbId, objectId, logicalId);
		}

		public DataLock GetDataLock(ObjectId objectId, LogicalPageId logicalId)
		{
			return _globalLockManager.GetDataLock(_dbId, objectId, logicalId);
		}
		#endregion
		#endregion
	}
}
