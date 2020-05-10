using Autofac.Features.AttributeFilters;
using Serilog;

namespace Zen.Trunk.Storage.Services
{
    public interface IStorageEngineEventService
    {
        void CachingPageBufferFlushScavengeStart(int bufferCount, int threshold);

        void CachingPageBufferFlushScavengeEnd(int bufferCount, int threshold);
    }

    public class StorageEngineEventService : IStorageEngineEventService
    {
        private readonly ILogger _eventLogger;

        public StorageEngineEventService([KeyFilter("EventLogger")] ILogger eventLogger)
        {
            _eventLogger = eventLogger;
        }

        public void CachingPageBufferFlushScavengeStart(int bufferCount, int threshold)
        {
            _eventLogger.Information(
                "{EventId} CachingPageBuffer Scavenge started [Buffer count is {BufferCount} with entry threshold set at {EntryThreshold}",
                StorageEngineEventIds.CachingPageBufferFlushScavengeStart,
                bufferCount,
                threshold);
        }

        public void CachingPageBufferFlushScavengeEnd(int bufferCount, int threshold)
        {
            _eventLogger.Information(
                "{EventId} CachingPageBuffer Scavenge ended [Buffer count is {BufferCount} with exit threshold set at {ExitThreshold}",
                StorageEngineEventIds.CachingPageBufferFlushScavengeStart,
                bufferCount,
                threshold);
        }
    }

    public static class StorageEngineEventIds
    {
        public const int BaseCachingPageBufferDeviceEventId = 10000;
        public const int CachingPageBufferFlushScavengeStart = BaseCachingPageBufferDeviceEventId;
        public const int CachingPageBufferFlushScavengeEnd = BaseCachingPageBufferDeviceEventId + 1;
        //public const int
    }
}
