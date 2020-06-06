using Autofac;

namespace Zen.Trunk.Storage.Data.Audio
{
    public class DatabaseAudioFactory : IDatabaseAudioFactory
    {
        private readonly ILifetimeScope _parentLifetimeScope;

        public DatabaseAudioFactory(ILifetimeScope parentLifetimeScope)
        {
            _parentLifetimeScope = parentLifetimeScope;
        }

        public IDatabaseAudio GetScopeForNewAudio(ObjectId objectId)
        {
            return new DatabaseAudio(_parentLifetimeScope, objectId, true);
        }

        public IDatabaseAudio GetScopeForExistingAudio(ObjectId objectId)
        {
            return new DatabaseAudio(_parentLifetimeScope, objectId, false);
        }
    }
}