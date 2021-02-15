using System;
using System.IO;
using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    public interface IPageBuffer : IDisposable
    {
        int BufferSize { get; }

        bool CanFree { get; }

        bool IsDirty { get; }

        VirtualPageId PageId { get; }

        bool IsDeleted { get; set; }

        bool IsNew { get; set; }

        bool IsReadPending { get; }

        bool IsWritePending { get; }

        LogicalPageId LogicalPageId { get; set; }

        long Timestamp { get; set; }

        void AddRef();

        void Release();

        Task SetDirtyAsync();

        Task SetFreeAsync();

        Stream GetBufferStream(int offset, int count, bool writable);

        void EnlistInTransaction();

        Task InitAsync(VirtualPageId pageId, LogicalPageId logicalId);

        Task LoadAsync();

        Task RequestLoadAsync(VirtualPageId pageId, LogicalPageId logicalId);

        Task SaveAsync();
    }
}