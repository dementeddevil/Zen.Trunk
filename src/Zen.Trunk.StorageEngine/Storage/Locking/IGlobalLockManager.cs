using System;

namespace Zen.Trunk.Storage.Locking
{
    public interface IGlobalLockManager
    {
        void LockDatabase(DatabaseId dbId, DatabaseLockType lockType, TimeSpan timeout);
        void UnlockDatabase(DatabaseId dbId);
        void LockRoot(DatabaseId dbId, FileGroupId fileGroupId, RootLockType lockType, TimeSpan timeout);
        void UnlockRoot(DatabaseId dbId, FileGroupId fileGroupId);
        RootLock GetRootLock(DatabaseId dbId, FileGroupId fileGroupId);
        void LockDistributionPage(DatabaseId dbId, VirtualPageId virtualPageId, ObjectLockType lockType, TimeSpan timeout);
        void UnlockDistributionPage(DatabaseId dbId, VirtualPageId virtualPageId);

        void LockDistributionExtent(DatabaseId dbId, VirtualPageId virtualPageId, uint extentIndex,
            ObjectLockType distLockType, DataLockType extentLockType, TimeSpan timeout);

        void UnlockDistributionExtent(DatabaseId dbId, VirtualPageId virtualPageId, uint extentIndex);
        void LockDistributionHeader(DatabaseId dbId, VirtualPageId virtualPageId, TimeSpan timeout);
        void UnlockDistributionHeader(DatabaseId dbId, VirtualPageId virtualPageId);
        ObjectLock GetDistributionLock(DatabaseId dbId, VirtualPageId virtualPageId);
        DataLock GetExtentLock(DatabaseId dbId, VirtualPageId virtualPageId, uint extentIndex);
        void LockObject(DatabaseId dbId, ObjectId objectId, ObjectLockType lockType, TimeSpan timeout);
        void UnlockObject(DatabaseId dbId, ObjectId objectId);
        ObjectLock GetObjectLock(DatabaseId dbId, ObjectId objectId);
        void LockSchema(DatabaseId dbId, ObjectId objectId, SchemaLockType lockType, TimeSpan timeout);
        void UnlockSchema(DatabaseId dbId, ObjectId objectId);
        SchemaLock GetSchemaLock(DatabaseId dbId, ObjectId objectId);

        void LockRootIndex(DatabaseId dbId, ObjectId ObjectId, TimeSpan timeout,
            bool writable);

        void UnlockRootIndex(DatabaseId dbId, ObjectId ObjectId, bool writable);

        void LockInternalIndex(DatabaseId dbId, ObjectId ObjectId, LogicalPageId logicalId,
            TimeSpan timeout, bool writable);

        void UnlockInternalIndex(DatabaseId dbId, ObjectId ObjectId, LogicalPageId logicalId,
            bool writable);

        void LockData(DatabaseId dbId, ObjectId objectId, LogicalPageId logicalId, DataLockType lockType, TimeSpan timeout);
        void UnlockData(DatabaseId dbId, ObjectId objectId, LogicalPageId logicalId);
        DataLock GetDataLock(DatabaseId dbId, ObjectId objectId, LogicalPageId logicalId);
    }
}