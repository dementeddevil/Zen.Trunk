namespace Zen.Trunk.Storage.Locking
{
    public interface IDataLock : IChildTransactionLock<DataLockType, ObjectLockType>
    {
    }
}