namespace Zen.Trunk.Storage.Locking
{
	using System;
	using System.Collections.Concurrent;
	using System.Diagnostics;

	/// <summary>
	/// Lock manager
	/// </summary>
	internal class GlobalLockManager
	{
		#region Private Fields
		private LockHandler<DatabaseLock, DatabaseLockType> _databaseLocks;
		private LockHandler<RootLock, RootLockType> _rootLocks;
		private LockHandler<ObjectLock, ObjectLockType> _objectLocks;
		private LockHandler<SchemaLock, SchemaLockType> _schemaLocks;
		private LockHandler<DataLock, DataLockType> _dataLocks;
		private RLockHandler _rLocks = new RLockHandler();
		#endregion

		#region Public Constructors
		public GlobalLockManager()
		{
			_databaseLocks = new LockHandler<DatabaseLock, DatabaseLockType>(0);
			_rootLocks = new LockHandler<RootLock, RootLockType>();
			_objectLocks = new LockHandler<ObjectLock, ObjectLockType>();
			_schemaLocks = new LockHandler<SchemaLock, SchemaLockType>();
			_dataLocks = new LockHandler<DataLock, DataLockType>();
		}
		#endregion

		#region Public Methods
		#region Database Lock/Unlock
		public void LockDatabase(ushort dbId, DatabaseLockType lockType, TimeSpan timeout)
		{
			DatabaseLock databaseLock = GetDatabaseLock(dbId);
			try
			{
				databaseLock.Lock(lockType, timeout);
			}
			finally
			{
				databaseLock.ReleaseRefLock();
			}
		}

		public void UnlockDatabase(ushort dbId)
		{
			DatabaseLock databaseLock = GetDatabaseLock(dbId);
			try
			{
				databaseLock.Unlock();
			}
			finally
			{
				databaseLock.ReleaseRefLock();
			}
		}
		#endregion

		#region Root Lock/Unlock
		public void LockRoot(ushort dbId, byte fileGroupId, RootLockType lockType, TimeSpan timeout)
		{
			RootLock rootLock = GetRootLock(dbId, fileGroupId);
			try
			{
				rootLock.Lock(lockType, timeout);
			}
			finally
			{
				rootLock.ReleaseRefLock();
			}
		}

		public void UnlockRoot(ushort dbId, byte fileGroupId)
		{
			RootLock rootLock = GetRootLock(dbId, fileGroupId);
			try
			{
				rootLock.Unlock();
			}
			finally
			{
				rootLock.ReleaseRefLock();
			}
		}

		public RootLock GetRootLock(ushort dbId, byte fileGroupId)
		{
			string key = LockIdent.GetFileGroupRootKey(dbId, fileGroupId);
			RootLock lockObject = _rootLocks.GetOrCreateLock(key);

			// Ensure parent lock has been resolved
			if (lockObject.Parent == null)
			{
				DatabaseLock parentLockObject = GetDatabaseLock(dbId);
				lockObject.Parent = parentLockObject; // assign to parent will addref
				parentLockObject.ReleaseRefLock();
			}
			return lockObject;
		}
		#endregion

		#region Distribution Page Locks
		public void LockDistributionPage(ushort dbId, ulong virtualPageId, ObjectLockType lockType, TimeSpan timeout)
		{
			ObjectLock objectLock = GetDistributionLock(dbId, virtualPageId);
			try
			{
				objectLock.Lock(lockType, timeout);
			}
			finally
			{
				objectLock.ReleaseRefLock();
			}
		}

		public void UnlockDistributionPage(ushort dbId, ulong virtualPageId)
		{
			ObjectLock objectLock = GetDistributionLock(dbId, virtualPageId);
			try
			{
				objectLock.Unlock();
			}
			finally
			{
				objectLock.ReleaseRefLock();
			}
		}

		public void LockDistributionExtent(ushort dbId, ulong virtualPageId, uint extentIndex,
			ObjectLockType distLockType, DataLockType extentLockType, TimeSpan timeout)
		{
			DataLock extentLock = GetExtentLock(dbId, virtualPageId, extentIndex);
			ObjectLock distLock = extentLock.Parent;
			try
			{
				distLock.Lock(distLockType, timeout);
				extentLock.Lock(extentLockType, timeout);
			}
			finally
			{
				distLock.ReleaseRefLock();
				extentLock.ReleaseRefLock();
			}
		}

		public void UnlockDistributionExtent(ushort dbId, ulong virtualPageId, uint extentIndex)
		{
			DataLock extentLock = GetExtentLock(dbId, virtualPageId, extentIndex);
			ObjectLock distLock = extentLock.Parent;
			try
			{
				distLock.Unlock();
				extentLock.Unlock();
			}
			finally
			{
				distLock.ReleaseRefLock();
				extentLock.ReleaseRefLock();
			}
		}

		public void LockDistributionHeader(ushort dbId, ulong virtualPageId, TimeSpan timeout)
		{
			Trace.TraceInformation("LDH:{0}:{1}", dbId, virtualPageId);
			string distKey = LockIdent.GetDistributionKey(dbId, virtualPageId);
			_rLocks.LockResource(distKey, timeout, true);
		}

		public void UnlockDistributionHeader(ushort dbId, ulong virtualPageId)
		{
			Trace.TraceInformation("UDH:{0}:{1}", dbId, virtualPageId);
			string distKey = LockIdent.GetDistributionKey(dbId, virtualPageId);
			_rLocks.UnlockResource(distKey, true);
		}

		public ObjectLock GetDistributionLock(ushort dbId, ulong virtualPageId)
		{
			string key = LockIdent.GetDistributionKey(dbId, virtualPageId);
			ObjectLock lockObject = _objectLocks.GetOrCreateLock(key);
			if (lockObject.Parent == null)
			{
				DatabaseLock parentLockObject = GetDatabaseLock(dbId);
				lockObject.Parent = parentLockObject; // assign to parent will addref
				parentLockObject.ReleaseRefLock();
			}
			return lockObject;
		}

		public DataLock GetExtentLock(ushort dbId, ulong virtualPageId, uint extentIndex)
		{
			string key = LockIdent.GetExtentLockKey(dbId, virtualPageId, extentIndex);
			DataLock lockObject = _dataLocks.GetOrCreateLock(key);
			if (lockObject.Parent == null)
			{
				ObjectLock parentLockObject = GetDistributionLock(dbId, virtualPageId);
				lockObject.Parent = parentLockObject; // assign to parent will addref
				parentLockObject.ReleaseRefLock();
			}
			return lockObject;
		}
		#endregion

		#region Object Lock/Unlock
		public void LockObject(ushort dbId, uint objectId, ObjectLockType lockType, TimeSpan timeout)
		{
			ObjectLock objectLock = GetObjectLock(dbId, objectId);
			try
			{
				objectLock.Lock(lockType, timeout);
			}
			finally
			{
				objectLock.ReleaseRefLock();
			}
		}

		public void UnlockObject(ushort dbId, uint objectId)
		{
			ObjectLock objectLock = GetObjectLock(dbId, objectId);
			try
			{
				objectLock.Unlock();
			}
			finally
			{
				objectLock.ReleaseRefLock();
			}
		}

		public ObjectLock GetObjectLock(ushort dbId, uint objectId)
		{
			string key = LockIdent.GetObjectLockKey(dbId, objectId);
			ObjectLock lockObject = _objectLocks.GetOrCreateLock(key);

			// Ensure parent lock has been resolved
			if (lockObject.Parent == null)
			{
				DatabaseLock parentLockObject = GetDatabaseLock(dbId);
				lockObject.Parent = parentLockObject; // assign to parent will addref
				parentLockObject.ReleaseRefLock();
			}
			return lockObject;
		}
		#endregion

		#region Object-Schema Lock/Unlock
		public void LockSchema(ushort dbId, uint objectId, SchemaLockType lockType, TimeSpan timeout)
		{
			SchemaLock schemaLock = GetSchemaLock(dbId, objectId);
			try
			{
				schemaLock.Lock(lockType, timeout);
			}
			finally
			{
				schemaLock.ReleaseRefLock();
			}
		}

		public void UnlockSchema(ushort dbId, uint objectId)
		{
			SchemaLock schemaLock = GetSchemaLock(dbId, objectId);
			try
			{
				schemaLock.Unlock();
			}
			finally
			{
				schemaLock.ReleaseRefLock();
			}
		}

		public SchemaLock GetSchemaLock(ushort dbId, uint objectId)
		{
			// Get schema lock
			string key = LockIdent.GetSchemaLockKey(dbId, objectId);
			SchemaLock lockObject = _schemaLocks.GetOrCreateLock(key);
			if (lockObject.Parent == null)
			{
				ObjectLock parentLockObject = GetObjectLock(dbId, objectId);
				lockObject.Parent = parentLockObject; // assign to parent will addref
				parentLockObject.ReleaseRefLock();
			}
			return lockObject;
		}
		#endregion

		#region Index Lock/Unlock
		public void LockRootIndex(ushort dbId, uint indexId, TimeSpan timeout,
			bool writable)
		{
			string resourceKey = LockIdent.GetIndexRootKey(dbId, indexId);
			LockResource(resourceKey, timeout, writable);
		}

		public void UnlockRootIndex(ushort dbId, uint indexId, bool writable)
		{
			string resourceKey = LockIdent.GetIndexRootKey(dbId, indexId);
			UnlockResource(resourceKey, writable);
		}

		public void LockInternalIndex(ushort dbId, uint indexId, ulong logicalId,
			TimeSpan timeout, bool writable)
		{
			string resourceKey = LockIdent.GetIndexInternalKey(dbId, indexId, logicalId);
			LockResource(resourceKey, timeout, writable);
		}

		public void UnlockInternalIndex(ushort dbId, uint indexId, ulong logicalId,
			bool writable)
		{
			string resourceKey = LockIdent.GetIndexInternalKey(dbId, indexId, logicalId);
			UnlockResource(resourceKey, writable);
		}
		#endregion

		#region Data Lock/Unlock
		public void LockData(ushort dbId, uint objectId, ulong logicalId, DataLockType lockType, TimeSpan timeout)
		{
			DataLock dataLock = GetDataLock(dbId, objectId, logicalId);
			try
			{
				dataLock.Lock(lockType, timeout);
			}
			finally
			{
				dataLock.ReleaseRefLock();
			}
		}

		public void UnlockData(ushort dbId, uint objectId, ulong logicalId)
		{
			DataLock dataLock = GetDataLock(dbId, objectId, logicalId);
			try
			{
				dataLock.Unlock();
			}
			finally
			{
				dataLock.ReleaseRefLock();
			}
		}

		public DataLock GetDataLock(ushort dbId, uint objectId, ulong logicalId)
		{
			string key = LockIdent.GetDataLockKey(dbId, objectId, logicalId);
			DataLock lockObject = _dataLocks.GetOrCreateLock(key);
			if (lockObject.Parent == null)
			{
				ObjectLock parentLockObject = GetObjectLock(dbId, objectId);
				lockObject.Parent = parentLockObject; // assign to parent will addref
				parentLockObject.ReleaseRefLock();
			}
			return lockObject;
		}
		#endregion
		#endregion

		#region Private Methods
		/// <summary>
		/// Locks the specified resource with a resource lock (spinlock).
		/// </summary>
		/// <param name="resource">The resource.</param>
		/// <param name="timeout">The timeout.</param>
		/// <param name="writable">if set to <c>true</c> [writable].</param>
		private void LockResource(string resource, TimeSpan timeout, bool writable)
		{
			_rLocks.LockResource(resource, timeout, writable);
		}

		/// <summary>
		/// Unlocks the specified resource with a resource lock (spinlock).
		/// </summary>
		/// <param name="resource">The resource.</param>
		/// <param name="writable">if set to <c>true</c> [writable].</param>
		private void UnlockResource(string resource, bool writable)
		{
			_rLocks.UnlockResource(resource, writable);
		}

		/// <summary>
		/// Gets a lock object suitable for locking a database instance.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <returns></returns>
		private DatabaseLock GetDatabaseLock(ushort dbId)
		{
			string key = LockIdent.GetDatabaseKey(dbId);
			return _databaseLocks.GetOrCreateLock(key);
		}
		#endregion
	}
}
