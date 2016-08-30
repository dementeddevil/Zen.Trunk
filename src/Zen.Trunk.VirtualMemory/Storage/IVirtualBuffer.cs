using System;
using System.IO;

namespace Zen.Trunk.Storage
{
    public interface IVirtualBuffer : IComparable<IVirtualBuffer>, IDisposable
    {
        string BufferId { get; }

        int BufferSize { get; }

        bool IsDirty { get; }

        void ClearDirty();

        void CopyTo(IVirtualBuffer destination);

        void CopyTo(byte[] buffer);

        Stream GetBufferStream(int offset, int count, bool writable);

        void InitFrom(byte[] buffer);

        void SetDirty();
    }
}