using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zen.Trunk.VirtualMemory
{
    public interface ISystemClock
    {
        DateTime UtcNow { get; }

        Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
    }
}
