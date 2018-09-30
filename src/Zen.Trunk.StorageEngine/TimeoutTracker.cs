using System;
// ReSharper disable MemberCanBePrivate.Global

namespace Zen.Trunk.Storage
{
    public struct TimeoutTracker
    {
        public TimeoutTracker(TimeSpan timeout) : this()
        {
            StartUtc = DateTime.UtcNow;
            OriginalTimeout = timeout;
        }

        public DateTime StartUtc { get; }

        public TimeSpan OriginalTimeout { get; }

        public TimeSpan Elapsed => DateTime.UtcNow - StartUtc;

        public TimeSpan RemainingTimeout => OriginalTimeout - Elapsed;

        public bool IsExpired => RemainingTimeout < TimeSpan.Zero;

        public void ThrowIfExpired(string message = null)
        {
            if (IsExpired)
            {
                throw new TimeoutException(message ?? "Timeout has expired");
            }
        }

        public static TimeoutTracker FromTimeSpan(TimeSpan timeout)
        {
            return new TimeoutTracker(timeout);
        }
    }
}
