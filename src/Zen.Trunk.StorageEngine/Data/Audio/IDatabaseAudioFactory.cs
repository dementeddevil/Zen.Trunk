namespace Zen.Trunk.Storage.Data.Audio
{
    public interface IDatabaseAudioFactory
    {
        IDatabaseAudio GetScopeForExistingAudio(ObjectId objectId);

        IDatabaseAudio GetScopeForNewAudio(ObjectId objectId);
    }
}