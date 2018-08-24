namespace Zen.Trunk.Storage.Locking
{
    public interface IObjectLock : IChildTransactionLock<ObjectLockType, DatabaseLockType>
    {
    }
}