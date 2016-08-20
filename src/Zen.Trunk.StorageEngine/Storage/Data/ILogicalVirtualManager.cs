using System;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Data
{
    public interface ILogicalVirtualManager : IDisposable
    {
        Task<LogicalPageId> GetNewLogicalAsync();

        Task<LogicalPageId> AddLookupAsync(VirtualPageId pageId, LogicalPageId logicalId);

        Task<LogicalPageId> GetLogicalAsync(VirtualPageId pageId);

        Task<VirtualPageId> GetVirtualAsync(LogicalPageId logicalId);
    }
}