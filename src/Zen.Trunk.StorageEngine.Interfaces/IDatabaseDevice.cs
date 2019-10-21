using System;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage
{
    public interface IDatabaseDevice
    {
        Task UseDatabaseAsync(TimeSpan lockTimeout);

        Task UnuseDatabaseAsync();
    }
}