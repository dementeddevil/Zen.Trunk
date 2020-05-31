using System.Threading.Tasks;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    public interface IDistributionPage : IDataPage
    {
        ObjectLockType DistributionLock { get; }

        Task<VirtualPageId> AllocatePageAsync(AllocateDataPageParameters allocParams);

        Task DeallocatePageAsync(uint pageIndex);

        Task ExportPageMappingTo(ILogicalVirtualManager logicalVirtualManager);

        Task SetDistributionLockAsync(ObjectLockType value);

        Task UpdateValidExtentsAsync(uint devicePageCapacity);
    }
}