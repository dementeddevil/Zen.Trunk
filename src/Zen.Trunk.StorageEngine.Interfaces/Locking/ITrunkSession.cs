using System;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>ITrunkSession</c> defines core information used by session
    /// manager and available on every thread.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface ITrunkSession : IDisposable
    {
        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        /// <value>
        /// The session identifier.
        /// </value>
        SessionId SessionId { get; }

        /// <summary>
        /// Gets the timeout.
        /// </summary>
        /// <value>
        /// The timeout.
        /// </value>
        TimeSpan TransactionTimeout { get; }

        /// <summary>
        /// Switches the shared database lock.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="lockTimeout">The lock timeout.</param>
        /// <returns></returns>
        Task SwitchSharedDatabaseLockAsync(IDatabaseDevice from, IDatabaseDevice to, TimeSpan lockTimeout);
    }
}