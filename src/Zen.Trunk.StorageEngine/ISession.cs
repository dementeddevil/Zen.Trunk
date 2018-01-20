using System;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>ISession</c> is used to present commands to and retrieve results from
    /// the storage engine.
    /// </summary>
    /// <remarks>
    /// Sessions have a unique session identifier and a virtual bound session
    /// identifier that can be used to link multiple sessions to the same
    /// logical transaction.
    /// Sessions are owned by "processes" and when a process is terminated then
    /// all associated sessions are closed. Active transactions associated with
    /// a closing session are rolled back.
    /// </remarks>
    public interface ISession : IDisposable
    {
        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        /// <value>
        /// The session identifier.
        /// </value>
        SessionId SessionId { get; }
    }
}