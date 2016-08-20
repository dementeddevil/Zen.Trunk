namespace Zen.Trunk.Storage.Locking
{
	using System;

	/// <summary>
	/// Lock manager
	/// </summary>
	public class LockManager
	{
		#region Private Fields
		private GlobalLockManager _globalLockManager;
		private ushort _dbId;
		#endregion

		#region Public Constructors
		internal LockManager(GlobalLockManager globalLockManager, ushort dbId)
		{
			if (globalLockManager == null)
			{
				throw new ArgumentNullException("globalLockManager");
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
		public void LockRoot(byte fileGroupId, RootLockType lockType, TimeSpan timeout)
		{
			_globalLockManager.LockRoot(_dbId, fileGroupId, lockType, timeout);
		}

		public void UnlockRoot(byte fileGroupId)
		{
			_globalLockManager.UnlockRoot(_dbId, fileGroupId);
		}
		#endregion

		#region Distribution Page Locks
		public void LockDistributionPage(ulong virtualPageId, ObjectLockType lockType, TimeSpan timeout)
		{
			_globalLockManager.LockDistributionPage(_dbId, virtualPageId, lockType, timeout);
		}

		public void UnlockDistributionPage(ulong virtualPageId)
		{
			_globalLockManager.UnlockDistributionPage(_dbId, virtualPageId);
		}

		public void LockDistributionExtent(ulong virtualPageId, uint extentIndex,
			ObjectLockType distLockType, DataLockType extentLockType, TimeSpan timeout)
		{
			_globalLockManager.LockDistributionExtent(_dbId, virtualPageId, extentIndex, distLockType, extentLockType, timeout);
		}

		public void UnlockDistributionExtent(ulong virtualPageId, uint extentIndex)
		{
			_globalLockManager.UnlockDistributionExtent(_dbId, virtualPageId, extentIndex);
		}

		public void LockDistributionHeader(ulong virtualPageId, TimeSpan timeout)
		{
			_globalLockManager.LockDistributionHeader(_dbId, virtualPageId, timeout);
		}

		public void UnlockDistributionHeader(ulong virtualPageId)
		{
			_globalLockManager.UnlockDistributionHeader(_dbId, virtualPageId);
		}
		#endregion

		#region Object Lock/Unlock
		public void LockObject(uint objectId, ObjectLockType lockType, TimeSpan timeout)
		{
			_globalLockManager.LockObject(_dbId, objectId, lockType, timeout);
		}

		public void UnlockObject(uint objectId)
		{
			_globalLockManager.UnlockObject(_dbId, objectId);
		}
		#endregion

		#region Object-Schema Lock/Unlock
		public void LockSchema(uint objectId, SchemaLockType lockType, TimeSpan timeout)
		{
			_globalLockManager.LockSchema(_dbId, objectId, lockType, timeout);
		}

		public void UnlockSchema(uint objectId)
		{
			_globalLockManager.UnlockSchema(_dbId, objectId);
		}
		#endregion

		#region Index Lock/Unlock
		public void LockRootIndex(uint indexId, TimeSpan timeout,
			bool writable)
		{
			_globalLockManager.LockRootIndex(_dbId, indexId, timeout, writable);
		}

		public void UnlockRootIndex(uint indexId, bool writable)
		{
			_globalLockManager.UnlockRootIndex(_dbId, indexId, writable);
		}

		public void LockInternalIndex(uint indexId, ulong logicalId,
			TimeSpan timeout, bool writable)
		{
			_globalLockManager.LockInternalIndex(_dbId, indexId, logicalId, timeout, writable);
		}

		public void UnlockInternalIndex(uint indexId, ulong logicalId,
			bool writable)
		{
			_globalLockManager.UnlockInternalIndex(_dbId, indexId, logicalId, writable);
		}
		#endregion

		#region Data Lock/Unlock
		public void LockData(uint objectId, ulong logicalId, DataLockType lockType, TimeSpan timeout)
		{
			_globalLockManager.LockData(_dbId, objectId, logicalId, lockType, timeout);
		}

		public void UnlockData(uint objectId, ulong logicalId)
		{
			_globalLockManager.UnlockData(_dbId, objectId, logicalId);
		}
		#endregion
		#endregion

		#region Private Methods
		/// <summary>
		/// Gets the root lock.
		/// </summary>
		/// <param name="fileGroupId">The file group id.</param>
		/// <returns></returns>
		internal RootLock GetRootLock(byte fileGroupId)
		{
			return _globalLockManager.GetRootLock(_dbId, fileGroupId);
		}

		/// <summary>
		/// Gets the distribution lock.
		/// </summary>
		/// <param name="virtualPageId">The virtual page id.</param>
		/// <returns></returns>
		internal ObjectLock GetDistributionLock(ulong virtualPageId)
		{
			return _globalLockManager.GetDistributionLock(_dbId, virtualPageId);
		}

		/// <summary>
		/// Gets the extent lock.
		/// </summary>
		/// <param name="virtualPageId">The virtual page id.</param>
		/// <param name="extentIndex">Index of the extent.</param>
		/// <returns></returns>
		internal DataLock GetExtentLock(ulong virtualPageId, uint extentIndex)
		{
			return _globalLockManager.GetExtentLock(_dbId, virtualPageId, extentIndex);
		}

		/// <summary>
		/// Gets a lock object suitable for locking an owned object such as
		/// a table or sample.
		/// </summary>
		/// <remarks>
		/// To read or update the definition of either a table or sample a
		/// schema lock must also be obtained.
		/// </remarks>
		/// <param name="objectId"></param>
		/// <returns></returns>
		internal ObjectLock GetObjectLock(uint objectId)
		{
			return _globalLockManager.GetObjectLock(_dbId, objectId);
		}

		/// <summary>
		/// Gets a lock object suitable for locking an object schema.
		/// </summary>
		/// <param name="objectId"></param>
		/// <returns></returns>
		internal SchemaLock GetSchemaLock(uint objectId)
		{
			return _globalLockManager.GetSchemaLock(_dbId, objectId);
		}

		/// <summary>
		/// Gets a lock object suitable for locking an owned data page.
		/// </summary>
		/// <param name="objectId"></param>
		/// <param name="logicalId"></param>
		/// <returns></returns>
		internal DataLock GetDataLock(uint objectId, ulong logicalId)
		{
			return _globalLockManager.GetDataLock(_dbId, objectId, logicalId);
		}
		#endregion
	}
}
