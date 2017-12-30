namespace Zen.Trunk.Storage
{
    public interface IDatabaseLockBuilder
    {
        IObjectLockBuilder WithObjectLock(string lockId);
    }
}