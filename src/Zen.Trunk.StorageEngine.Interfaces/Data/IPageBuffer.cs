using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    public interface IPageBuffer : IStatefulBuffer
    {
        bool IsDeleted { get; set; }

        bool IsNew { get; set; }

        bool IsReadPending { get; }

        bool IsWritePending { get; }

        LogicalPageId LogicalPageId { get; set; }

        long Timestamp { get; set; }

        void EnlistInTransaction();

        Task InitAsync(VirtualPageId pageId, LogicalPageId logicalId);

        Task LoadAsync();

        Task RequestLoadAsync(VirtualPageId pageId, LogicalPageId logicalId);

        Task SaveAsync();
    }
}