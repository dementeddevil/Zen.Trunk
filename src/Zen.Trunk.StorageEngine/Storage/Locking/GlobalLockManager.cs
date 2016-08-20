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
		private readonly LockHandler<DatabaseLock, DatabaseLockType> _databaseLocks;
		private readonly LockHandler<RootLock, RootLockType> _rootLocks;
		private readonly LockHandler<ObjectLock, ObjectLockType> _objectLocks;
		private readonly LockHandler<SchemaLock, SchemaLockType> _schemaLocks;
		private readonly LockHandler<DataLock, DataLockType> _dataLocks;
		private readonly RLockHandler _rLocks = new RLockHandler();
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
			var databaseLock = GetDatabaseLock(dbId);
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
			var databaseLock = GetDatabaseLock(dbId);
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
			var rootLock = GetRootLock(dbId, fileGroupId);
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
			var rootLock = GetRootLock(dbId, fileGroupId);
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
			var key = LockIdent.GetFileGroupRootKey(dbId, fileGroupId);
			var lockObject = _rootLocks.GetOrCreateLock(key);

			// Ensure parent lock has been resolved
			if (lockObject.Parent == null)
			{
				var parentLockObject = GetDatabaseLock(dbId);
				lockObject.Parent = parentLockObject; // assign to parent will addref
				parentLockObject.ReleaseRefLock();
			}
			return lockObject;
		}
		#endregion

		#region Distribution Page Locks
		public void LockDistributionPage(ushort dbId, ulong virtualPageId, ObjectLockType lockType, TimeSpan timeout)
		{
			var objectLock = GetDistributionLock(dbId, virtualPageId);
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
			var objectLock = GetDistributionLock(dbId, virtualPageId);
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
			var extentLock = GetExtentLock(dbId, virtualPageId, extentIndex);
			var distLock = extentLock.Parent;
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
			var extentLock = GetExtentLock(dbId, virtualPageId, extentIndex);
			var distLock = extentLock.Parent;
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
			var distKey = LockIdent.GetDistributionKey(dbId, virtualPageId);
			_rLocks.LockResource(distKey, timeout, true);
		}

		public void UnlockDistributionHeader(ushort dbId, ulong virtualPageId)
		{
			Trace.TraceInformation("UDH:{0}:{1}", dbId, virtualPageId);
			var distKey = LockIdent.GetDistributionKey(dbId, virtualPageId);
			_rLocks.UnlockResource(distKey, true);
		}

		public ObjectLock GetDistributionLock(ushort dbId, ulong virtualPageId)
		{
			var key = LockIdent.GetDistributionKey(dbId, virtualPageId);
			var lockObject = _objectLocks.GetOrCreateLock(key);
			if (lockObject.Parent == null)
			{
				var parentLockObject = GetDatabaseLock(dbId);
				lockObject.Parent = parentLockObject; // assign to parent will addref
				parentLockObject.ReleaseRefLock();
			}
			return lockObject;
		}

		public DataLock GetExtentLock(ushort dbId, ulong virtualPageId, uint extentIndex)
		{
			var key = LockIdent.GetExtentLockKey(dbId, virtualPageId, extentIndex);
			var lockObject = _dataLocks.GetOrCreateLock(key);
			if (lockObject.Parent == null)
			{
				var parentLockObject = GetDistributionLock(dbId, virtualPageId);
				lockObject.Parent = parentLockObject; // assign to parent will addref
				parentLockObject.ReleaseRefLock();
			}
			return lockObject;
		}
		#endregion

		#region Object Lock/Unlock
		public void LockObject(ushort dbId, uint objectId, ObjectLockType lockType, TimeSpan timeout)
		{
			var objectLock = GetObjectLock(dbId, objectId);
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
			var objectLock = GetObjectLock(dbId, objectId);
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
			var key = LockIdent.GetObjectLockKey(dbId, objectId);
			var lockObject = _objectLocks.GetOrCreateLock(key);

			// Ensure parent lock has been resolved
			if (lockObject.Parent == null)
			{
				var parentLockObject = GetDatabaseLock(dbId);
				lockObject.Parent = parentLockObject; // assign to parent will addref
				parentLockObject.ReleaseRefLock();
			}
			return lockObject;
		}
		#endregion

		#region Object-Schema Lock/Unlock
		public void LockSchema(ushort dbId, uint objectId, SchemaLockType lockType, TimeSpan timeout)
		{
			var schemaLock = GetSchemaLock(dbId, objectId);
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
			var schemaLock = GetSchemaLock(dbId, objectId);
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
			var key = LockIdent.GetSchemaLockKey(dbId, objectId);
			var lockObject = _schemaLocks.GetOrCreateLock(key);
			if (lockObject.Parent == null)
			{
				var parentLockObject = GetObjectLock(dbId, objectId);
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
			var resourceKey = LockIdent.GetIndexRootKey(dbId, indexId);
			LockResource(resourceKey, timeout, writable);
		}

		public void UnlockRootIndex(ushort dbId, uint indexId, bool writable)
		{
			var resourceKey = LockIdent.GetIndexRootKey(dbId, indexId);
			UnlockResource(resourceKey, writable);
		}

		public void LockInternalIndex(ushort dbId, uint indexId, ulong logicalId,
			TimeSpan timeout, bool writable)
		{
			var resourceKey = LockIdent.GetIndexInternalKey(dbId, indexId, logicalId);
			LockResource(resourceKey, timeout, writable);
		}

		public void UnlockInternalIndex(ushort dbId, uint indexId, ulong logicalId,
			bool writable)
		{
			var resourceKey = LockIdent.GetIndexInternalKey(dbId, indexId, logicalId);
			UnlockResource(resourceKey, writable);
		}
		#endregion

		#region Data Lock/Unlock
		public void LockData(ushort dbId, uint objectId, ulong logicalId, DataLockType lockType, TimeSpan timeout)
		{
			var dataLock = GetDataLock(dbId, objectId, logicalId);
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
			var dataLock = GetDataLock(dbId, objectId, logicalId);
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
			var key = LockIdent.GetDataLockKey(dbId, objectId, logicalId);
			var lockObject = _dataLocks.GetOrCreateLock(key);
			if (lockObject.Parent == null)
			{
				var parentLockObject = GetObjectLock(dbId, objectId);
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
			var key = LockIdent.GetDatabaseKey(dbId);
			return _databaseLocks.GetOrCreateLock(key);
		}
		#endregion
	}
}
