using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public interface ISession
    {
        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        /// <value>
        /// The session identifier.
        /// </value>
        SessionId SessionId { get; }

        /// <summary>
        /// Gets the bound session identifier.
        /// </summary>
        /// <value>
        /// The bound session identifier.
        /// </value>
        VirtualSessionId VirtualSessionId { get; }

        /// <summary>
        /// Gets the process identifier that owns this instance.
        /// </summary>
        /// <value>
        /// The process identifier.
        /// </value>
        ProcessId ProcessId { get; }
    }

    public class Session : ISession
    {
        public Session(SessionId sessionId, ProcessId processId)
        {
            SessionId = sessionId;
            ProcessId = processId;
            VirtualSessionId = VirtualSessionId.Zero;
        }

        public SessionId SessionId { get; }

        public ProcessId ProcessId { get; }

        public VirtualSessionId VirtualSessionId { get; internal set; }
    }

    public class SessionManager
    {
        private readonly IDictionary<SessionId, Session> _activeSessions =
            new Dictionary<SessionId, Session>();
        private SessionId _nextSessionId = new SessionId(1);
        private VirtualSessionId _nextVirtualSessionId = new VirtualSessionId(1);

        public ISession CreateSession()
        {
            var sessionId = _nextSessionId;
            _nextSessionId = new SessionId(sessionId.Value + 1);

            var session = new Session(sessionId, ProcessId.Zero);
            _activeSessions.Add(sessionId, session);
            return session;
        }

        public VirtualSessionId GetVirtualSessionId(SessionId sessionId)
        {
            var session = _activeSessions[sessionId];
            if (session.VirtualSessionId == VirtualSessionId.Zero)
            {
                session.VirtualSessionId = _nextVirtualSessionId;
                _nextVirtualSessionId = new VirtualSessionId(_nextVirtualSessionId.Value + 1);
            }
            return _nextVirtualSessionId;
        }

        public void JoinSession(SessionId sessionId, VirtualSessionId virtualSessionId)
        {
            var session = _activeSessions[sessionId];
            if (session.VirtualSessionId != VirtualSessionId.Zero)
            {
                throw new ArgumentException("Session is already bound.");
            }

            session.VirtualSessionId = virtualSessionId;
        }
    }
}
