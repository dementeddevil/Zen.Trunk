using System;
using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    public interface IDatabaseDevice
    {
        Task UseDatabaseAsync(TimeSpan lockTimeout);

        Task UnuseDatabaseAsync();
    }
}