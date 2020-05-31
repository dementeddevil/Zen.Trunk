using System.Collections.Generic;
using System.Threading.Tasks;
using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Data
{
    public interface IPrimaryFileGroupRootPage : IRootPage
    {
        ICollection<DeviceReferenceBufferFieldWrapper> Devices { get; }

        ICollection<ObjectReferenceBufferFieldWrapper> Objects { get; }

        Task CreateSlaveDataDevices(FileGroupDevice pageDevice);
    }
}