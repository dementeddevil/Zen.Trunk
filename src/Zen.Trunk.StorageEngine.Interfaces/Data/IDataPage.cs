using System;

namespace Zen.Trunk.Storage.Data
{
    public interface IDataPage : IPage
    {
        FileGroupId FileGroupId { get; set; }

        bool HoldLock { get; set; }

        TimeSpan LockTimeout { get; set; }

        long Timestamp { get; }

        IPageBuffer DataBuffer { get; set; }
    }
}