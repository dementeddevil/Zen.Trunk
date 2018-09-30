namespace Zen.Trunk.Storage.Data.Table
{
    public interface IDatabaseTableFactory
    {
        IDatabaseTable GetTableScopeForExistingTable(ObjectId objectId);

        IDatabaseTable GetTableScopeForNewTable(ObjectId objectId);
    }
}