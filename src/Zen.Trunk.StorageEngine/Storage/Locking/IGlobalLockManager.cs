using System;
using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>IGlobalLockManager</c> defines the main contract for accessing
    /// the process-wide lock manager.
    /// </summary>
    public interface IGlobalLockManager
    {
        /// <summary>
        /// Locks the database.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task LockDatabaseAsync(DatabaseId dbId, DatabaseLockType lockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the database.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task UnlockDatabaseAsync(DatabaseId dbId);

        /// <summary>
        /// Gets a lock object suitable for locking a database instance.
        /// </summary>
        /// <param name="dbId">The db id.</param>
        /// <returns>
        /// A <see cref="DatabaseLock"/> instance.
        /// </returns>
        DatabaseLock GetDatabaseLock(DatabaseId dbId);

        /// <summary>
        /// Locks the root.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task LockRootAsync(DatabaseId dbId, FileGroupId fileGroupId, RootLockType lockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the root.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task UnlockRootAsync(DatabaseId dbId, FileGroupId fileGroupId);

        /// <summary>
        /// Gets the root lock.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <returns>
        /// A <see cref="RootLock"/> instance.
        /// </returns>
        RootLock GetRootLock(DatabaseId dbId, FileGroupId fileGroupId);

        /// <summary>
        /// Locks the distribution page.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task LockDistributionPageAsync(DatabaseId dbId, VirtualPageId virtualPageId, ObjectLockType lockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the distribution page.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task UnlockDistributionPageAsync(DatabaseId dbId, VirtualPageId virtualPageId);

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
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task LockDistributionExtentAsync(DatabaseId dbId, VirtualPageId virtualPageId, uint extentIndex, ObjectLockType distLockType, DataLockType extentLockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the distribution extent.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="extentIndex">Index of the extent.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task UnlockDistributionExtentAsync(DatabaseId dbId, VirtualPageId virtualPageId, uint extentIndex);

        /// <summary>
        /// Locks the distribution header.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="timeout">The timeout.</param>
        void LockDistributionHeader(DatabaseId dbId, VirtualPageId virtualPageId, TimeSpan timeout);

        /// <summary>
        /// Unlocks the distribution header.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        void UnlockDistributionHeader(DatabaseId dbId, VirtualPageId virtualPageId);

        /// <summary>
        /// Gets the distribution lock.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <returns>
        /// An <see cref="ObjectLock"/> instance.
        /// </returns>
        ObjectLock GetDistributionLock(DatabaseId dbId, VirtualPageId virtualPageId);

        /// <summary>
        /// Gets the extent lock.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="extentIndex">Index of the extent.</param>
        /// <returns>
        /// A <see cref="DataLock"/> instance.
        /// </returns>
        DataLock GetExtentLock(DatabaseId dbId, VirtualPageId virtualPageId, uint extentIndex);

        /// <summary>
        /// Locks the object.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task LockObjectAsync(DatabaseId dbId, ObjectId objectId, ObjectLockType lockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the object.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task UnlockObjectAsync(DatabaseId dbId, ObjectId objectId);

        /// <summary>
        /// Gets the object lock.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <returns>
        /// An <see cref="ObjectLock"/> instance.
        /// </returns>
        ObjectLock GetObjectLock(DatabaseId dbId, ObjectId objectId);

        /// <summary>
        /// Locks the schema.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task LockSchemaAsync(DatabaseId dbId, ObjectId objectId, SchemaLockType lockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the schema.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task UnlockSchemaAsync(DatabaseId dbId, ObjectId objectId);

        /// <summary>
        /// Gets the schema lock.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <returns>
        /// A <see cref="SchemaLock"/> instance.
        /// </returns>
        SchemaLock GetSchemaLock(DatabaseId dbId, ObjectId objectId);

        /// <summary>
        /// Locks the root index.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        void LockRootIndex(DatabaseId dbId, ObjectId objectId, IndexId indexId, bool writable, TimeSpan timeout);

        /// <summary>
        /// Unlocks the root index.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        void UnlockRootIndex(DatabaseId dbId, ObjectId objectId, IndexId indexId, bool writable);

        /// <summary>
        /// Locks the internal index.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <param name="timeout">The timeout.</param>
        void LockInternalIndex(DatabaseId dbId, ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable, TimeSpan timeout);

        /// <summary>
        /// Unlocks the internal index.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <returns></returns>
        void UnlockInternalIndex(DatabaseId dbId, ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable);

        /// <summary>
        /// Locks the leaf index.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        void LockLeafIndex(DatabaseId dbId, ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable, TimeSpan timeout);

        /// <summary>
        /// Unlocks the leaf index.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <returns></returns>
        void UnlockLeafIndex(DatabaseId dbId, ObjectId objectId, IndexId indexId, LogicalPageId logicalId, bool writable);

        /// <summary>
        /// Locks the data.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="lockType">Type of the lock.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task LockDataAsync(DatabaseId dbId, ObjectId objectId, LogicalPageId logicalId, DataLockType lockType, TimeSpan timeout);

        /// <summary>
        /// Unlocks the data.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task UnlockDataAsync(DatabaseId dbId, ObjectId objectId, LogicalPageId logicalId);

        /// <summary>
        /// Gets the data lock.
        /// </summary>
        /// <param name="dbId">The database identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <returns>
        /// A <see cref="DataLock"/> instance.
        /// </returns>
        DataLock GetDataLock(DatabaseId dbId, ObjectId objectId, LogicalPageId logicalId);
    }
}