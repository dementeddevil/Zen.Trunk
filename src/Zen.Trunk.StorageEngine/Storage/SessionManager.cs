using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage
{
    public class SessionManager : ISessionManager
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
        private int _nextSessionIdValue;

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
            return new SessionId((uint)Interlocked.Increment(ref _nextSessionIdValue));
        }

        private void OnSessionDisposed(Session session)
        {
            Session temp;
            _activeSessions.TryRemove(session.SessionId, out temp);
        }
    }
}
