namespace Zen.Trunk.Storage.Locking
{
    public interface ISchemaLock : IChildTransactionLock<SchemaLockType, ObjectLockType>
    {
    }
}