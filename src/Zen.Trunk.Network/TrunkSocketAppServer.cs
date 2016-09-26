using System;
using Autofac;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Logging;
using SuperSocket.SocketBase.Protocol;
using Zen.Trunk.Logging;
using ILog = SuperSocket.SocketBase.Logging.ILog;

namespace Zen.Trunk.Network
{
    /// <summary>
    /// <c>TrunkSocketAppServer</c> handles the network protocol socket server.
    /// </summary>
    /// <seealso cref="SuperSocket.SocketBase.AppServer{TrunkSocketAppSession}" />
    /// <remarks>
    /// All messages sent to the trunk server consist of the following
    /// Command (4 bytes ascii)
    /// Length (2 bytes)
    /// Payload (variable length)
    /// </remarks>
    public class TrunkSocketAppServer : AppServer<TrunkSocketAppSession, BinaryRequestInfo>
    {
        private readonly ILifetimeScope _lifetimeScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkSocketAppServer"/> class.
        /// </summary>
        /// <param name="lifetimeScope">The autofac lifetime scope.</param>
        public TrunkSocketAppServer(ILifetimeScope lifetimeScope)
            : base(new DefaultReceiveFilterFactory<TrunkReceiveFilter, BinaryRequestInfo>())
        {
            _lifetimeScope = lifetimeScope;
        }

        /// <summary>
        /// create a new TAppSession instance, you can override it to create the session instance in your own way
        /// </summary>
        /// <param name="socketSession">the socket session.</param>
        /// <returns>
        /// the new created session instance
        /// </returns>
        protected override TrunkSocketAppSession CreateAppSession(ISocketSession socketSession)
        {
            // Create session object via Autofac IoC lifetime scope.
            return _lifetimeScope.Resolve<TrunkSocketAppSession>();
        }
    }

    /// <summary>
    /// <c>SuperSocketLogFactory</c> acts as a link to our liblog logging block
    /// </summary>
    /// <seealso cref="SuperSocket.SocketBase.Logging.ILogFactory" />
    public class SuperSocketLogFactory : ILogFactory
    {
        /// <summary>
        /// Gets the log by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public ILog GetLog(string name)
        {
            var libLogLogger = LogProvider.GetLogger(name);
            return new SuperSocketLog(libLogLogger);
        }
    }

    internal class SuperSocketLog : ILog
    {
        private readonly Logging.ILog _innerLog;

        public SuperSocketLog(Zen.Trunk.Logging.ILog innerLog)
        {
            _innerLog = innerLog;
        }

        public bool IsFatalEnabled => _innerLog.IsFatalEnabled();

        public bool IsErrorEnabled => _innerLog.IsErrorEnabled();

        public bool IsWarnEnabled => _innerLog.IsWarnEnabled();

        public bool IsInfoEnabled => _innerLog.IsInfoEnabled();

        public bool IsDebugEnabled => _innerLog.IsDebugEnabled();

        public void Fatal(object message)
        {
            _innerLog.Fatal(message.ToString());
        }

        public void Fatal(object message, Exception exception)
        {
            _innerLog.FatalException(message.ToString(), exception);
        }

        public void FatalFormat(string format, params object[] args)
        {
            _innerLog.FatalFormat(format, args);
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            var message = string.Format(provider, format, args);
            _innerLog.Fatal(message);
        }

        public void FatalFormat(string format, object arg0)
        {
            _innerLog.FatalFormat(format, arg0);
        }

        public void FatalFormat(string format, object arg0, object arg1)
        {
            _innerLog.FatalFormat(format, arg0, arg1);
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            _innerLog.FatalFormat(format, arg0, arg1, arg2);
        }

        public void Error(object message)
        {
            throw new NotImplementedException();
        }

        public void Error(object message, Exception exception)
        {
            throw new NotImplementedException();
        }

        public void ErrorFormat(string format, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void ErrorFormat(string format, object arg0)
        {
            throw new NotImplementedException();
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            throw new NotImplementedException();
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            throw new NotImplementedException();
        }

        public void Debug(object message)
        {
            _innerLog.Debug(message.ToString());
        }

        public void Debug(object message, Exception exception)
        {
            _innerLog.DebugException(message.ToString(), exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            _innerLog.DebugFormat(format, args);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            var message = string.Format(provider, format, args);
            _innerLog.Debug(message);
        }

        public void DebugFormat(string format, object arg0)
        {
            _innerLog.DebugFormat(format, arg0);
        }

        public void DebugFormat(string format, object arg0, object arg1)
        {
            _innerLog.DebugFormat(format, arg0, arg1);
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            _innerLog.DebugFormat(format, arg0, arg1, arg2);
        }

        public void Info(object message)
        {
            throw new NotImplementedException();
        }

        public void Info(object message, Exception exception)
        {
            throw new NotImplementedException();
        }

        public void InfoFormat(string format, object arg0)
        {
            throw new NotImplementedException();
        }

        public void InfoFormat(string format, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            throw new NotImplementedException();
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            throw new NotImplementedException();
        }

        public void Warn(object message)
        {
            throw new NotImplementedException();
        }

        public void Warn(object message, Exception exception)
        {
            throw new NotImplementedException();
        }

        public void WarnFormat(string format, object arg0)
        {
            throw new NotImplementedException();
        }

        public void WarnFormat(string format, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            throw new NotImplementedException();
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            throw new NotImplementedException();
        }
    }
}