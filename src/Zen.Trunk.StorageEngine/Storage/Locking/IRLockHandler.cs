using System;

namespace Zen.Trunk.Storage.Locking
{
    internal interface IRLockHandler : ILockHandler
    {
        void LockResource(string resource, bool writable, TimeSpan timeout);

        void UnlockResource(string resource, bool writable);
    }
}