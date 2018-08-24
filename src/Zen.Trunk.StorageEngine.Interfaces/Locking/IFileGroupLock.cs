namespace Zen.Trunk.Storage.Locking
{
    public interface IFileGroupLock : IChildTransactionLock<FileGroupRootLockType, DatabaseLockType>
    {
    }
}