using System.Threading.Tasks;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data
{
    public interface IRootPage : ILogicalPage
    {
        uint AllocatedPages { get; set; }

        FileGroupRootLockType FileGroupLock { get; }

        uint GrowthPages { get; set; }

        double GrowthPercent { get; set; }

        bool IsExpandable { get; }

        bool IsExpandableByPercent { get; }

        bool IsRootPage { get; }

        uint MaximumPages { get; set; }

        byte Status { get; set; }

        Task SetRootLockAsync(FileGroupRootLockType value);
    }
}