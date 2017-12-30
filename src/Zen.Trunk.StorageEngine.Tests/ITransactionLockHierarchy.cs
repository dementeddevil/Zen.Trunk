using System;
using System.Collections.Generic;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    public interface ITransactionLockHierarchy : IDisposable
    {
        IDictionary<string, DatabaseLock> DatabaseLocks { get; }

        IDictionary<string, ObjectLock> ObjectLocks { get; }

        IDictionary<string, SchemaLock> SchemaLocks { get; }

        IDictionary<string, DataLock> DataLocks { get; }

        int ExpectedFinalReleaseCount { get; }

        int ActualFinalReleaseCount { get; }
    }
}