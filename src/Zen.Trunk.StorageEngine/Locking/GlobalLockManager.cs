using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>GlobalLockManager</c> tracks all lock objects across all daatabases
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Locking.IGlobalLockManager" />
    public class GlobalLockManager : IGlobalLockManager
    {
        #region Private Fields
        private readonly LockHandler<DatabaseLock, DatabaseLockType> _databaseLocks =
            new LockHandler<DatabaseLock, DatabaseLockType>(0);
        private readonly LockHandler<FileGroupRootLock, FileGroupRootLockType> _rootLocks =
            new LockHandler<FileGroupRootLock, FileGroupRootLockType>();
        private readonly LockHandler<ObjectLock, ObjectLockType> _objectLocks =
            new LockHandler<ObjectLock, ObjectLockType>();
        private readonly LockHandler<SchemaLock, SchemaLockType> _schemaLocks =
            new LockHandler<SchemaLock, SchemaLockType>();
        private readonly LockHandler<DataLock, DataLockType> _dataLocks =
            new LockHandler<DataLock, DataLockType>();
        private readonly RLockHandler _rLocks = new RLockHandler();
        #endregion

        #region Public Methods
        #region Database Lock/Unlock
        /// <summary>
        /// Locks the database.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public async Task LockDatabaseAsync(DatabaseId dbId, DatabaseLockType lockType, TimeSpan timeout)
        {
            var databaseLock = GetDatabaseLock(dbId);
            try
            {
                await databaseLock.LockAsync(lockType, timeout).ConfigureAwait(false);
            }
            finally
            {
                databaseLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Unlocks the database.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <returns></returns>
        public async Task UnlockDatabaseAsync(DatabaseId dbId)
        {
            var databaseLock = GetDatabaseLock(dbId);
            try
            {
                await databaseLock.UnlockAsync().ConfigureAwait(false);
            }
            finally
            {
                databaseLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Gets a lock object suitable for locking a database instance.
        /// </summary>
        /// <param name="dbId">The db id.</param>
        /// <returns>
        /// A <see cref="DatabaseLock" /> instance.
        /// </returns>
        public IDatabaseLock GetDatabaseLock(DatabaseId dbId)
        {
            var key = LockIdentity.GetDatabaseKey(dbId);
            return _databaseLocks.GetOrCreateLock(key);
        }
        #endregion

        #region Root Lock/Unlock
        /// <summary>
        /// Locks the file-group root page.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task LockFileGroupAsync(DatabaseId dbId, FileGroupId fileGroupId, FileGroupRootLockType lockType, TimeSpan timeout)
        {
            var rootLock = GetFileGroupLock(dbId, fileGroupId);
            try
            {
                await rootLock.LockAsync(lockType, timeout).ConfigureAwait(false);
            }
            finally
            {
                rootLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Unlocks the file-group root page.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task UnlockFileGroupAsync(DatabaseId dbId, FileGroupId fileGroupId)
        {
            var rootLock = GetFileGroupLock(dbId, fileGroupId);
            try
            {
                await rootLock.UnlockAsync().ConfigureAwait(false);
            }
            finally
            {
                rootLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Gets the file-group root lock.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <returns>
        /// A <see cref="FileGroupRootLock" /> instance.
        /// </returns>
        public IFileGroupLock GetFileGroupLock(DatabaseId dbId, FileGroupId fileGroupId)
        {
            var key = LockIdentity.GetFileGroupRootKey(dbId, fileGroupId);
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
        /// <summary>
        /// Locks the distribution page.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task LockDistributionPageAsync(DatabaseId dbId, VirtualPageId virtualPageId, ObjectLockType lockType, TimeSpan timeout)
        {
            var objectLock = GetDistributionLock(dbId, virtualPageId);
            try
            {
                await objectLock.LockAsync(lockType, timeout).ConfigureAwait(false);
            }
            finally
            {
                objectLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Unlocks the distribution page.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task UnlockDistributionPageAsync(DatabaseId dbId, VirtualPageId virtualPageId)
        {
            var objectLock = GetDistributionLock(dbId, virtualPageId);
            try
            {
                await objectLock.UnlockAsync().ConfigureAwait(false);
            }
            finally
            {
                objectLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Locks the distribution extent.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="extentIndex">Index of the extent.</param>
        /// <param name="distLockType">Type of the dist lock.</param>
        /// <param name="extentLockType">Type of the extent lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task LockDistributionExtentAsync(DatabaseId dbId, VirtualPageId virtualPageId, uint extentIndex,
            ObjectLockType distLockType, DataLockType extentLockType, TimeSpan timeout)
        {
            var extentLock = GetDistributionExtentLock(dbId, virtualPageId, extentIndex);
            var distLock = extentLock.Parent;
            try
            {
                await distLock.LockAsync(distLockType, timeout).ConfigureAwait(false);
                await extentLock.LockAsync(extentLockType, timeout).ConfigureAwait(false);
            }
            finally
            {
                distLock.ReleaseRefLock();
                extentLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Unlocks the distribution extent.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="extentIndex">Index of the extent.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task UnlockDistributionExtentAsync(DatabaseId dbId, VirtualPageId virtualPageId, uint extentIndex)
        {
            var extentLock = GetDistributionExtentLock(dbId, virtualPageId, extentIndex);
            var distLock = extentLock.Parent;
            try
            {
                await distLock.UnlockAsync().ConfigureAwait(false);
                await extentLock.UnlockAsync().ConfigureAwait(false);
            }
            finally
            {
                distLock.ReleaseRefLock();
                extentLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Locks the distribution header.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="timeout">The timeout.</param>
        public void LockDistributionHeader(DatabaseId dbId, VirtualPageId virtualPageId, TimeSpan timeout)
        {
            Trace.TraceInformation("LDH:{0}:{1}", dbId, virtualPageId);
            var distKey = LockIdentity.GetDistributionKey(dbId, virtualPageId);
            _rLocks.LockResource(distKey, true, timeout);
        }

        /// <summary>
        /// Unlocks the distribution header.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        public void UnlockDistributionHeader(DatabaseId dbId, VirtualPageId virtualPageId)
        {
            Trace.TraceInformation("UDH:{0}:{1}", dbId, virtualPageId);
            var distKey = LockIdentity.GetDistributionKey(dbId, virtualPageId);
            _rLocks.UnlockResource(distKey, true);
        }

        /// <summary>
        /// Gets the distribution lock.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <returns>
        /// An <see cref="ObjectLock" /> instance.
        /// </returns>
        public IObjectLock GetDistributionLock(DatabaseId dbId, VirtualPageId virtualPageId)
        {
            var key = LockIdentity.GetDistributionKey(dbId, virtualPageId);
            var lockObject = _objectLocks.GetOrCreateLock(key);

            if (lockObject.Parent == null)
            {
                var parentLockObject = GetDatabaseLock(dbId);
                lockObject.Parent = parentLockObject; // assign to parent will addref
                parentLockObject.ReleaseRefLock();
            }

            return lockObject;
        }

        /// <summary>
        /// Gets the extent lock.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="extentIndex">Index of the extent.</param>
        /// <returns>
        /// A <see cref="DataLock" /> instance.
        /// </returns>
        public IDataLock GetDistributionExtentLock(DatabaseId dbId, VirtualPageId virtualPageId, uint extentIndex)
        {
            var key = LockIdentity.GetDistributionExtentLockKey(dbId, virtualPageId, extentIndex);
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
        /// <summary>
        /// Locks the object.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public async Task LockObjectAsync(DatabaseId dbId, ObjectId objectId, ObjectLockType lockType, TimeSpan timeout)
        {
            var objectLock = GetObjectLock(dbId, objectId);
            try
            {
                await objectLock.LockAsync(lockType, timeout).ConfigureAwait(false);
            }
            finally
            {
                objectLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Unlocks the object.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        public async Task UnlockObjectAsync(DatabaseId dbId, ObjectId objectId)
        {
            var objectLock = GetObjectLock(dbId, objectId);
            try
            {
                await objectLock.UnlockAsync().ConfigureAwait(false);
            }
            finally
            {
                objectLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Gets the object lock.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        public IObjectLock GetObjectLock(DatabaseId dbId, ObjectId objectId)
        {
            var key = LockIdentity.GetObjectLockKey(dbId, objectId);
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
        /// <summary>
        /// Locks the schema.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public async Task LockSchemaAsync(DatabaseId dbId, ObjectId objectId, SchemaLockType lockType, TimeSpan timeout)
        {
            var schemaLock = GetSchemaLock(dbId, objectId);
            try
            {
                await schemaLock.LockAsync(lockType, timeout).ConfigureAwait(false);
            }
            finally
            {
                schemaLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Unlocks the schema.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        public async Task UnlockSchemaAsync(DatabaseId dbId, ObjectId objectId)
        {
            var schemaLock = GetSchemaLock(dbId, objectId);
            try
            {
                await schemaLock.UnlockAsync().ConfigureAwait(false);
            }
            finally
            {
                schemaLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Gets the schema lock.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        public ISchemaLock GetSchemaLock(DatabaseId dbId, ObjectId objectId)
        {
            // Get schema lock
            var key = LockIdentity.GetSchemaLockKey(dbId, objectId);
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
        /// <summary>
        /// Locks the root index.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <param name="timeout">The timeout.</param>
        public void LockRootIndex(DatabaseId dbId, ObjectId objectId, IndexId indexId, bool writable, TimeSpan timeout)
        {
            var resourceKey = LockIdentity.GetIndexRootKey(dbId, objectId, indexId);
            LockResource(resourceKey, writable, timeout);
        }

        /// <summary>
        /// Unlocks the root index.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        public void UnlockRootIndex(DatabaseId dbId, ObjectId objectId, IndexId indexId, bool writable)
        {
            var resourceKey = LockIdentity.GetIndexRootKey(dbId, objectId, indexId);
            UnlockResource(resourceKey, writable);
        }

        /// <summary>
        /// Locks the internal index.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <param name="timeout">The timeout.</param>
        public void LockInternalIndex(DatabaseId dbId, ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable, TimeSpan timeout)
        {
            var resourceKey = LockIdentity.GetIndexInternalKey(dbId, objectId, indexId, logicalId);
            LockResource(resourceKey, writable, timeout);
        }

        /// <summary>
        /// Unlocks the internal index.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        public void UnlockInternalIndex(DatabaseId dbId, ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable)
        {
            var resourceKey = LockIdentity.GetIndexInternalKey(dbId, objectId, indexId, logicalId);
            UnlockResource(resourceKey, writable);
        }

        /// <summary>
        /// Locks the leaf index.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <param name="timeout">The timeout.</param>
        public void LockLeafIndex(DatabaseId dbId, ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable, TimeSpan timeout)
        {
            var resourceKey = LockIdentity.GetIndexLeafKey(dbId, objectId, indexId, logicalId);
            LockResource(resourceKey, writable, timeout);
        }

        /// <summary>
        /// Unlocks the leaf index.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        public void UnlockLeafIndex(DatabaseId dbId, ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable)
        {
            var resourceKey = LockIdentity.GetIndexLeafKey(dbId, objectId, indexId, logicalId);
            UnlockResource(resourceKey, writable);
        }
        #endregion

        #region Data Lock/Unlock
        /// <summary>
        /// Locks the data.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public async Task LockDataAsync(DatabaseId dbId, ObjectId objectId, LogicalPageId logicalId, DataLockType lockType, TimeSpan timeout)
        {
            var dataLock = GetDataLock(dbId, objectId, logicalId);
            try
            {
                await dataLock.LockAsync(lockType, timeout).ConfigureAwait(false);
            }
            finally
            {
                dataLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Unlocks the data.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <returns></returns>
        public async Task UnlockDataAsync(DatabaseId dbId, ObjectId objectId, LogicalPageId logicalId)
        {
            var dataLock = GetDataLock(dbId, objectId, logicalId);
            try
            {
                await dataLock.UnlockAsync().ConfigureAwait(false);
            }
            finally
            {
                dataLock.ReleaseRefLock();
            }
        }

        /// <summary>
        /// Gets the data lock.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <returns></returns>
        public IDataLock GetDataLock(DatabaseId dbId, ObjectId objectId, LogicalPageId logicalId)
        {
            var key = LockIdentity.GetDataLockKey(dbId, objectId, logicalId);
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
        private void LockResource(string resource, bool writable, TimeSpan timeout)
        {
            _rLocks.LockResource(resource, writable, timeout);
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
        #endregion
    }
}
