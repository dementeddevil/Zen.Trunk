using System;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>IAmbientProperties</c> defines core information used by session
    /// manager and available on every thread.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface IAmbientProperties : IDisposable
    {
        /// <summary>
        /// Gets the process identifier.
        /// </summary>
        /// <value>
        /// The process identifier.
        /// </value>
        ProcessId ProcessId { get; }

        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        /// <value>
        /// The session identifier.
        /// </value>
        SessionId SessionId { get; }

        /// <summary>
        /// Gets the virtual session identifier.
        /// </summary>
        /// <value>
        /// The virtual session identifier.
        /// </value>
        VirtualSessionId VirtualSessionId { get; }

        /// <summary>
        /// Gets the timeout.
        /// </summary>
        /// <value>
        /// The timeout.
        /// </value>
        TimeSpan? DefaultTransactionTimeout { get; }
    }
}