using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zen.Trunk.VirtualMemory
{
    public class DefaultSystemClock : ISystemClock
    {
        public DateTime UtcNow => DateTime.UtcNow;

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(delay, cancellationToken);
        }
    }
}