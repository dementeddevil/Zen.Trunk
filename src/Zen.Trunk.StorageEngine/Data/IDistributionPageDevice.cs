using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    public interface IDistributionPageDevice : IMountableDevice
    {
        DeviceId DeviceId { get; }

        uint DistributionPageOffset { get; }

        IFileGroupDevice FileGroupDevice { get; }

        bool IsPrimary { get; }

        Task<VirtualPageId> AllocateDataPageAsync(AllocateDataPageParameters allocParams);

        Task DeallocateDataPageAsync(DeallocateDataPageParameters deallocParams);

        Task<IRootPage> InitRootPageAsync();

        Task<IRootPage> LoadRootPageAsync();
    }
}