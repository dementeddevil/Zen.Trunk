namespace Zen.Trunk.Storage
{
    public interface IObjectLockBuilder
    {
        IObjectLockBuilder WithSchemaLock(string lockId);

        IObjectLockBuilder WithDataLock(string lockId);
    }
}