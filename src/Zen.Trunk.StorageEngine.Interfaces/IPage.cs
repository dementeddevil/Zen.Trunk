using Autofac;
using System;
using System.IO;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    public interface IPage : IDisposable
    {
        uint DataSize { get; }

        uint HeaderSize { get; }

        bool IsDirty { get; }

        bool IsManagedData { get; set; }

        bool IsNewPage { get; }

        uint MinHeaderSize { get; }

        uint PageSize { get; }

        PageType PageType { get; }

        bool ReadOnly { get; set; }

        VirtualPageId VirtualPageId { get; set; }

        Stream CreateDataStream(bool readOnly);

        void Save();

        void SetLifetimeScope(ILifetimeScope scope);

        void PreInitInternal();

        void OnInitInternal();

        void PreLoadInternal();

        void PostLoadInternal();

        void SetDirty();

        void SetHeaderDirty();

        void SetDataDirty();

        void CheckReadOnly();
    }
}