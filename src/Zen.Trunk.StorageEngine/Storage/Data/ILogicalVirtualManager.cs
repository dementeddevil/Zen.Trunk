using System;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Data
{
    public interface ILogicalVirtualManager : IDisposable
    {
        Task<LogicalPageId> GetNewLogicalAsync();

        Task<LogicalPageId> AddLookupAsync(DevicePageId pageId, LogicalPageId logicalId);

        Task<LogicalPageId> GetLogicalAsync(DevicePageId pageId);

        Task<DevicePageId> GetVirtualAsync(LogicalPageId logicalId);
    }
}