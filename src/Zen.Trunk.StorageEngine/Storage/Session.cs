using System;
using System.Collections.Concurrent;
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

    public class SessionManager
    {
        private class Session : ISession
        {
            private readonly SessionManager _owner;
            private bool _isDisposed;

            public Session(SessionManager owner, SessionId sessionId)
            {
                _owner = owner;
                SessionId = sessionId;
            }

            public SessionId SessionId { get; }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                    _owner.OnSessionDisposed(this);
                }
            }
        }

        private readonly ConcurrentDictionary<SessionId, Session> _activeSessions =
            new ConcurrentDictionary<SessionId, Session>();
        private readonly object _syncLock = new object();
        private SessionId _nextSessionId = new SessionId(1);

        /// <summary>
        /// Creates a new session.
        /// </summary>
        /// <returns></returns>
        public ISession CreateSession()
        {
            var sessionId = GetNextSessionId();
            var session = new Session(this, sessionId);
            _activeSessions.TryAdd(sessionId, session);
            return session;
        }

        private SessionId GetNextSessionId()
        {
            lock (_syncLock)
            {
                var sessionId = _nextSessionId;
                _nextSessionId = new SessionId(sessionId.Value + 1);
                return sessionId;
            }            
        }

        private void OnSessionDisposed(Session session)
        {
            Session temp;
            _activeSessions.TryRemove(session.SessionId, out temp);
        }
    }
}
