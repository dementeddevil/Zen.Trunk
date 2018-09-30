using System;
using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Locking
{
	/// <summary>
	/// <c>DatabaseLockManager</c> represents an abstraction of the lock manager
	/// that is directly associated with a given database instance.
	/// </summary>
	public class DatabaseLockManager : IDatabaseLockManager
	{
		#region Private Fields
		private readonly IGlobalLockManager _globalLockManager;
	    #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseLockManager"/> class.
        /// </summary>
        /// <param name="globalLockManager">The global lock manager.</param>
        /// <param name="dbId">The database identifier.</param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
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
			DatabaseId = dbId;
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the database identifier.
        /// </summary>
        /// <value>
        /// The database identifier.
        /// </value>
        public DatabaseId DatabaseId { get; }
	    #endregion

        #region Public Methods
        #region Database Lock/Unlock
        /// <summary>
        /// Locks the database.
        /// </summary>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public Task LockDatabaseAsync(DatabaseLockType lockType, TimeSpan timeout)
		{
			return _globalLockManager.LockDatabaseAsync(DatabaseId, lockType, timeout);
		}

        /// <summary>
        /// Unlocks the database.
        /// </summary>
        /// <returns></returns>
        public Task UnlockDatabaseAsync()
		{
			return _globalLockManager.UnlockDatabaseAsync(DatabaseId);
		}

        /// <summary>
        /// Gets the database lock.
        /// </summary>
        /// <returns></returns>
        public IDatabaseLock GetDatabaseLock()
	    {
	        return _globalLockManager.GetDatabaseLock(DatabaseId);
	    }
        #endregion

        #region File-Group Root Lock/Unlock
        /// <summary>
        /// Locks the root.
        /// </summary>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public Task LockFileGroupAsync(FileGroupId fileGroupId, FileGroupRootLockType lockType, TimeSpan timeout)
		{
			return _globalLockManager.LockFileGroupAsync(DatabaseId, fileGroupId, lockType, timeout);
		}

        /// <summary>
        /// Unlocks the root.
        /// </summary>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <returns></returns>
        public Task UnlockFileGroupAsync(FileGroupId fileGroupId)
		{
			return _globalLockManager.UnlockFileGroupAsync(DatabaseId, fileGroupId);
		}

        /// <summary>
        /// Gets the root lock.
        /// </summary>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <returns></returns>
        public IFileGroupLock GetFileGroupLock(FileGroupId fileGroupId)
		{
			return _globalLockManager.GetFileGroupLock(DatabaseId, fileGroupId);
		}
        #endregion

        #region Distribution Page Locks
        /// <summary>
        /// Locks the distribution page.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public Task LockDistributionPageAsync(VirtualPageId virtualPageId, ObjectLockType lockType, TimeSpan timeout)
		{
			return _globalLockManager.LockDistributionPageAsync(DatabaseId, virtualPageId, lockType, timeout);
		}

        /// <summary>
        /// Unlocks the distribution page.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <returns></returns>
        public Task UnlockDistributionPageAsync(VirtualPageId virtualPageId)
		{
			return _globalLockManager.UnlockDistributionPageAsync(DatabaseId, virtualPageId);
		}

        /// <summary>
        /// Locks the distribution extent.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="extentIndex">Index of the extent.</param>
        /// <param name="distLockType">Type of the dist lock.</param>
        /// <param name="extentLockType">Type of the extent lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public Task LockDistributionExtentAsync(VirtualPageId virtualPageId, uint extentIndex,
			ObjectLockType distLockType, DataLockType extentLockType, TimeSpan timeout)
		{
			return _globalLockManager.LockDistributionExtentAsync(DatabaseId, virtualPageId, extentIndex, distLockType, extentLockType, timeout);
		}

        /// <summary>
        /// Unlocks the distribution extent.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="extentIndex">Index of the extent.</param>
        /// <returns></returns>
        public Task UnlockDistributionExtentAsync(VirtualPageId virtualPageId, uint extentIndex)
		{
			return _globalLockManager.UnlockDistributionExtentAsync(DatabaseId, virtualPageId, extentIndex);
		}

        /// <summary>
        /// Locks the distribution header.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="timeout">The timeout.</param>
        public void LockDistributionHeader(VirtualPageId virtualPageId, TimeSpan timeout)
		{
			_globalLockManager.LockDistributionHeader(DatabaseId, virtualPageId, timeout);
		}

        /// <summary>
        /// Unlocks the distribution header.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        public void UnlockDistributionHeader(VirtualPageId virtualPageId)
		{
			_globalLockManager.UnlockDistributionHeader(DatabaseId, virtualPageId);
		}

        /// <summary>
        /// Gets the distribution lock.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <returns></returns>
        public IObjectLock GetDistributionLock(VirtualPageId virtualPageId)
		{
			return _globalLockManager.GetDistributionLock(DatabaseId, virtualPageId);
		}

        /// <summary>
        /// Gets the extent lock.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="extentIndex">Index of the extent.</param>
        /// <returns></returns>
        public IDataLock GetDistributionExtentLock(VirtualPageId virtualPageId, uint extentIndex)
		{
			return _globalLockManager.GetDistributionExtentLock(DatabaseId, virtualPageId, extentIndex);
		}
        #endregion

        #region Object Lock/Unlock
        /// <summary>
        /// Locks the object.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public Task LockObjectAsync(ObjectId objectId, ObjectLockType lockType, TimeSpan timeout)
		{
			return _globalLockManager.LockObjectAsync(DatabaseId, objectId, lockType, timeout);
		}

        /// <summary>
        /// Unlocks the object.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        public Task UnlockObjectAsync(ObjectId objectId)
		{
			return _globalLockManager.UnlockObjectAsync(DatabaseId, objectId);
		}

        /// <summary>
        /// Gets the object lock.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        public IObjectLock GetObjectLock(ObjectId objectId)
		{
			return _globalLockManager.GetObjectLock(DatabaseId, objectId);
		}
        #endregion

        #region Object-Schema Lock/Unlock
        /// <summary>
        /// Locks the schema.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public Task LockSchemaAsync(ObjectId objectId, SchemaLockType lockType, TimeSpan timeout)
		{
			return _globalLockManager.LockSchemaAsync(DatabaseId, objectId, lockType, timeout);
		}

        /// <summary>
        /// Unlocks the schema.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        public Task UnlockSchemaAsync(ObjectId objectId)
		{
			return _globalLockManager.UnlockSchemaAsync(DatabaseId, objectId);
		}

        /// <summary>
        /// Gets the schema lock.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        public ISchemaLock GetSchemaLock(ObjectId objectId)
		{
			return _globalLockManager.GetSchemaLock(DatabaseId, objectId);
		}
        #endregion

        #region Index Lock/Unlock
        /// <summary>
        /// Locks the index of the root.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <param name="timeout">The timeout.</param>
        public void LockRootIndex(ObjectId objectId, IndexId indexId, bool writable, TimeSpan timeout)
		{
			_globalLockManager.LockRootIndex(DatabaseId, objectId, indexId, writable, timeout);
		}

        /// <summary>
        /// Unlocks the index of the root.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        public void UnlockRootIndex(ObjectId objectId, IndexId indexId, bool writable)
		{
			_globalLockManager.UnlockRootIndex(DatabaseId, objectId, indexId, writable);
		}

        /// <summary>
        /// Locks the index of the internal.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <param name="timeout">The timeout.</param>
        public void LockInternalIndex(ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable, TimeSpan timeout)
		{
			_globalLockManager.LockInternalIndex(DatabaseId, objectId, indexId, logicalId, writable, timeout);
		}

        /// <summary>
        /// Unlocks the index of the internal.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        public void UnlockInternalIndex(ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable)
		{
			_globalLockManager.UnlockInternalIndex(DatabaseId, objectId, indexId, logicalId, writable);
		}

        /// <summary>
        /// Locks the index of the leaf.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <param name="timeout">The timeout.</param>
        public void LockLeafIndex(ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable, TimeSpan timeout)
        {
            _globalLockManager.LockLeafIndex(DatabaseId, objectId, indexId, logicalId, writable, timeout);
        }

        /// <summary>
        /// Unlocks the index of the leaf.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        public void UnlockLeafIndex(ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable)
        {
            _globalLockManager.UnlockLeafIndex(DatabaseId, objectId, indexId, logicalId, writable);
        }
        #endregion

        #region Data Lock/Unlock
        /// <summary>
        /// Locks the data.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public Task LockDataAsync(ObjectId objectId, LogicalPageId logicalId, DataLockType lockType, TimeSpan timeout)
		{
			return _globalLockManager.LockDataAsync(DatabaseId, objectId, logicalId, lockType, timeout);
		}

        /// <summary>
        /// Unlocks the data.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <returns></returns>
        public Task UnlockDataAsync(ObjectId objectId, LogicalPageId logicalId)
		{
			return _globalLockManager.UnlockDataAsync(DatabaseId, objectId, logicalId);
		}

        /// <summary>
        /// Gets the data lock.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <returns></returns>
        public IDataLock GetDataLock(ObjectId objectId, LogicalPageId logicalId)
		{
			return _globalLockManager.GetDataLock(DatabaseId, objectId, logicalId);
		}
		#endregion
		#endregion
	}
}
