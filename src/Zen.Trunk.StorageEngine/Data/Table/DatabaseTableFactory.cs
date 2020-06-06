using Autofac;

namespace Zen.Trunk.Storage.Data.Table
{
    public class DatabaseTableFactory : IDatabaseTableFactory
    {
        private readonly ILifetimeScope _parentLifetimeScope;

        public DatabaseTableFactory(ILifetimeScope parentLifetimeScope)
        {
            _parentLifetimeScope = parentLifetimeScope;
        }

        public IDatabaseTable GetScopeForNewTable(ObjectId objectId)
        {
            return new DatabaseTable(_parentLifetimeScope, objectId, true);
        }

        public IDatabaseTable GetScopeForExistingTable(ObjectId objectId)
        {
            return new DatabaseTable(_parentLifetimeScope, objectId, false);
        }
    }
}