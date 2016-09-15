using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>Session</c> is used to present commands to and retrieve results from
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
    public class Session
    {
        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        /// <value>
        /// The session identifier.
        /// </value>
        public SessionId SessionId { get; }

        /// <summary>
        /// Gets the bound session identifier.
        /// </summary>
        /// <value>
        /// The bound session identifier.
        /// </value>
        public VirtualSessionId VirtualSessionId { get; }

        /// <summary>
        /// Gets the process identifier that owns this instance.
        /// </summary>
        /// <value>
        /// The process identifier.
        /// </value>
        public ProcessId ProcessId { get; }
    }

    public class SessionManager
    {
        public Session CreateSession()
        {
            
        }

        public VirtualSessionId GetVirtualSessionId(SessionId sessionId)
        {
            
        }

        public void JoinSession(SessionId sessionId, VirtualSessionId virtualSessionId)
        {
            
        }
    }
}
