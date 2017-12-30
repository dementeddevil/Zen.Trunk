using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    public class TransactionLockHierarchyBuilder
    {
        private class RootLockBuilder : IRootLockBuilder
        {
            private readonly TransactionLockHierarchyBuilder _owner;

            public RootLockBuilder(TransactionLockHierarchyBuilder owner)
            {
                _owner = owner;
            }

            public IDatabaseLockBuilder WithDatabaseLock(string lockId)
            {
                var lockObject = new DatabaseLock();
                lockObject.Id = lockId;
                lockObject.Initialise();
                lockObject.AddRefLock();
                lockObject.FinalRelease += _owner._lockReleaseTracker.OnLockFinalRelease;
                _owner._databaseLocks.Add(lockId, lockObject);
                return new DatabaseLockBuilder(_owner, lockObject);
            }
        }

        private class DatabaseLockBuilder : IDatabaseLockBuilder
        {
            private readonly TransactionLockHierarchyBuilder _owner;
            private readonly DatabaseLock _parentLock;

            public DatabaseLockBuilder(TransactionLockHierarchyBuilder owner, DatabaseLock parentLock)
            {
                _owner = owner;
                _parentLock = parentLock;
            }

            public IObjectLockBuilder WithObjectLock(string lockId)
            {
                var lockObject = new ObjectLock();
                lockObject.Id = lockId;
                lockObject.Parent = _parentLock;
                lockObject.Initialise();
                lockObject.AddRefLock();
                lockObject.FinalRelease += _owner._lockReleaseTracker.OnLockFinalRelease;
                _owner._objectLocks.Add(lockId, lockObject);
                return new ObjectLockBuilder(_owner, lockObject);
            }
        }

        private class ObjectLockBuilder : IObjectLockBuilder
        {
            private readonly TransactionLockHierarchyBuilder _owner;
            private readonly ObjectLock _parentLock;

            public ObjectLockBuilder(TransactionLockHierarchyBuilder owner, ObjectLock parentLock)
            {
                _owner = owner;
                _parentLock = parentLock;
            }

            public IObjectLockBuilder WithSchemaLock(string lockId)
            {
                var lockObject = new SchemaLock();
                lockObject.Id = lockId;
                lockObject.Parent = _parentLock;
                lockObject.Initialise();
                lockObject.AddRefLock();
                lockObject.FinalRelease += _owner._lockReleaseTracker.OnLockFinalRelease;
                _owner._schemaLocks.Add(lockId, lockObject);
                return this;
            }

            public IObjectLockBuilder WithDataLock(string lockId)
            {
                var lockObject = new DataLock();
                lockObject.Id = lockId;
                lockObject.Parent = _parentLock;
                lockObject.Initialise();
                lockObject.AddRefLock();
                lockObject.FinalRelease += _owner._lockReleaseTracker.OnLockFinalRelease;
                _owner._dataLocks.Add(lockId, lockObject);
                return this;
            }
        }

        private class TransactionLockHierarchy : ITransactionLockHierarchy
        {
            private readonly TransactionLockHierarchyBuilder _owner;

            public TransactionLockHierarchy(TransactionLockHierarchyBuilder owner)
            {
                _owner = owner;

                DatabaseLocks = new ReadOnlyDictionary<string, DatabaseLock>(owner._databaseLocks);
                ObjectLocks = new ReadOnlyDictionary<string, ObjectLock>(owner._objectLocks);
                SchemaLocks = new ReadOnlyDictionary<string, SchemaLock>(owner._schemaLocks);
                DataLocks = new ReadOnlyDictionary<string, DataLock>(owner._dataLocks);
            }

            public IDictionary<string, DatabaseLock> DatabaseLocks { get; }

            public IDictionary<string, ObjectLock> ObjectLocks { get; }

            public IDictionary<string, SchemaLock> SchemaLocks { get; }

            public IDictionary<string, DataLock> DataLocks { get; }

            public int ExpectedFinalReleaseCount { get; private set; }

            public int ActualFinalReleaseCount { get; private set; }

            public void Dispose()
            {
                ExpectedFinalReleaseCount =
                    DataLocks.Count +
                    SchemaLocks.Count +
                    ObjectLocks.Count +
                    DatabaseLocks.Count;

                foreach (var lockObject in DataLocks.Values)
                {
                    lockObject.ReleaseRefLock();
                }
                foreach (var lockObject in SchemaLocks.Values)
                {
                    lockObject.ReleaseRefLock();
                }
                foreach (var lockObject in ObjectLocks.Values)
                {
                    lockObject.ReleaseRefLock();
                }
                foreach (var lockObject in DatabaseLocks.Values)
                {
                    lockObject.ReleaseRefLock();
                }

                ActualFinalReleaseCount = _owner._lockReleaseTracker.FinalReleaseCount;
            }
        }

        private class LockReleaseTracker
        {
            public int FinalReleaseCount { get; private set; }

            public void OnLockFinalRelease(object sender, EventArgs e)
            {
                ++FinalReleaseCount;
            }
        }

        private readonly LockReleaseTracker _lockReleaseTracker =
            new LockReleaseTracker();
        private readonly IDictionary<string, DatabaseLock> _databaseLocks =
            new Dictionary<string, DatabaseLock>();
        private readonly IDictionary<string, ObjectLock> _objectLocks =
            new Dictionary<string, ObjectLock>();
        private readonly IDictionary<string, SchemaLock> _schemaLocks =
            new Dictionary<string, SchemaLock>();
        private readonly IDictionary<string, DataLock> _dataLocks =
            new Dictionary<string, DataLock>();

        public IRootLockBuilder WithRootLock()
        {
            return new RootLockBuilder(this);
        }

        public ITransactionLockHierarchy Build()
        {
            return new TransactionLockHierarchy(this);
        }
    }
}