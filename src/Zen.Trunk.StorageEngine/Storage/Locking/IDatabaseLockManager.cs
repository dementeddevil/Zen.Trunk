using System;
using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Locking
{
	/// <summary>
	/// <c>IDatabaseLockManager</c> represents an abstraction of the lock
	/// manager that is directly associated with a given database instance.
	/// </summary>
	public interface IDatabaseLockManager
	{
        /// <summary>
        /// Gets the database identifier.
        /// </summary>
        /// <value>
        /// The database identifier.
        /// </value>
        DatabaseId DatabaseId { get; }

        #region Database Lock/Unlock
        /// <summary>
        /// Locks the database.
        /// </summary>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        Task LockDatabaseAsync(DatabaseLockType lockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the database.
        /// </summary>
        /// <returns></returns>
        Task UnlockDatabaseAsync();

        /// <summary>
        /// Gets the database lock.
        /// </summary>
        /// <returns></returns>
        DatabaseLock GetDatabaseLock();
        #endregion

        #region File-Group Lock/Unlock
        /// <summary>
        /// Locks the file-group root.
        /// </summary>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        Task LockFileGroupAsync(FileGroupId fileGroupId, FileGroupLockType lockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the file-group root.
        /// </summary>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <returns></returns>
        Task UnlockFileGroupAsync(FileGroupId fileGroupId);

        /// <summary>
        /// Gets the file-group root lock.
        /// </summary>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <returns></returns>
        FileGroupLock GetFileGroupLock(FileGroupId fileGroupId);
        #endregion

        #region Distribution Page Locks
        /// <summary>
        /// Locks the distribution page.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        Task LockDistributionPageAsync(VirtualPageId virtualPageId, ObjectLockType lockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the distribution page.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <returns></returns>
        Task UnlockDistributionPageAsync(VirtualPageId virtualPageId);

        /// <summary>
        /// Locks the distribution extent.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="extentIndex">Index of the extent.</param>
        /// <param name="distLockType">Type of the dist lock.</param>
        /// <param name="extentLockType">Type of the extent lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        Task LockDistributionExtentAsync(VirtualPageId virtualPageId, uint extentIndex, ObjectLockType distLockType, DataLockType extentLockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the distribution extent.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="extentIndex">Index of the extent.</param>
        /// <returns></returns>
        Task UnlockDistributionExtentAsync(VirtualPageId virtualPageId, uint extentIndex);

        /// <summary>
        /// Locks the distribution header.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="timeout">The timeout.</param>
        void LockDistributionHeader(VirtualPageId virtualPageId, TimeSpan timeout);

        /// <summary>
        /// Unlocks the distribution header.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        void UnlockDistributionHeader(VirtualPageId virtualPageId);

        /// <summary>
        /// Gets the distribution lock.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <returns></returns>
        ObjectLock GetDistributionLock(VirtualPageId virtualPageId);

        /// <summary>
        /// Gets the distribution extent lock.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="extentIndex">Index of the extent.</param>
        /// <returns></returns>
        DataLock GetDistributionExtentLock(VirtualPageId virtualPageId, uint extentIndex);
        #endregion

        #region Object Lock/Unlock
        /// <summary>
        /// Locks the object.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        Task LockObjectAsync(ObjectId objectId, ObjectLockType lockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the object.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        Task UnlockObjectAsync(ObjectId objectId);

        /// <summary>
        /// Gets the object lock.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        ObjectLock GetObjectLock(ObjectId objectId);
        #endregion

        #region Object-Schema Lock/Unlock
        /// <summary>
        /// Locks the schema.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        Task LockSchemaAsync(ObjectId objectId, SchemaLockType lockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the schema.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        Task UnlockSchemaAsync(ObjectId objectId);

        /// <summary>
        /// Gets the schema lock.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        SchemaLock GetSchemaLock(ObjectId objectId);
        #endregion

        #region Index Lock/Unlock
        /// <summary>
        /// Locks the index of the root.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <param name="timeout">The timeout.</param>
        void LockRootIndex(ObjectId objectId, IndexId indexId, bool writable, TimeSpan timeout);

        /// <summary>
        /// Unlocks the index of the root.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        void UnlockRootIndex(ObjectId objectId, IndexId indexId, bool writable);

        /// <summary>
        /// Locks the index of the internal.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <param name="timeout">The timeout.</param>
        void LockInternalIndex(ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable, TimeSpan timeout);

        /// <summary>
        /// Unlocks the index of the internal.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        void UnlockInternalIndex(ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable);

        /// <summary>
        /// Locks the index of the leaf.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <param name="timeout">The timeout.</param>
        void LockLeafIndex(ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable, TimeSpan timeout);

        /// <summary>
        /// Unlocks the index of the leaf.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        void UnlockLeafIndex(ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable);
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
        Task LockDataAsync(ObjectId objectId, LogicalPageId logicalId, DataLockType lockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the data.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <returns></returns>
        Task UnlockDataAsync(ObjectId objectId, LogicalPageId logicalId);

        /// <summary>
        /// Gets the data lock.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <returns></returns>
        DataLock GetDataLock(ObjectId objectId, LogicalPageId logicalId);
		#endregion
	}
}
