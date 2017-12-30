namespace Zen.Trunk.Storage
{
    public interface IRootLockBuilder
    {
        IDatabaseLockBuilder WithDatabaseLock(string lockId);
    }
}