namespace Zen.Trunk.Storage.Data.Table
{
    public interface IDatabaseTableFactory
    {
        IDatabaseTable GetScopeForExistingTable(ObjectId objectId);

        IDatabaseTable GetScopeForNewTable(ObjectId objectId);
    }
}