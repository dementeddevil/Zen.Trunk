using System;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Zen.Trunk.Storage.Data.Audio
{
    public interface IDatabaseAudio
    {
        LogicalPageId DataFirstLogicalPageId { get; }
        LogicalPageId DataLastLogicalPageId { get; set; }
        IFileGroupDevice FileGroupDevice { get; }
        FileGroupId FileGroupId { get; }
        string FileGroupName { get; }
        bool HasData { get; }
        bool IsLoading { get; }
        bool IsNewAudio { get; }
        TimeSpan LockTimeout { get; set; }
        ObjectId ObjectId { get; }
        LogicalPageId SchemaFirstLogicalPageId { get; }
        LogicalPageId SchemaLastLogicalPageId { get; }
        AudioSchemaPage SchemaRootPage { get; }

        Task AppendAudioData(WaveFileReader waveReader);
        Task<AudioDataPage> InitDataPageAndLinkAsync(AudioDataPage prevDataPage);
        Task LoadSchemaAsync(LogicalPageId firstLogicalPageId);
    }
}