using System;
using System.IO;
using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    public interface IStatefulBuffer : IDisposable
    {
        int BufferSize { get; }

        bool CanFree { get; }

        bool IsDirty { get; }

        VirtualPageId PageId { get; }

        void AddRef();

        void Release();

        Task SetDirtyAsync();

        Task SetFreeAsync();

        Stream GetBufferStream(int offset, int count, bool writable);
    }
}