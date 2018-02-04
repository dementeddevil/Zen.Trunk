using System;
using SSocket = SuperSocket.SocketBase.Logging;
using Zen.Trunk.Logging;

namespace Zen.Trunk.Network
{
    internal class SuperSocketLog : SSocket.ILog
    {
        private readonly ILog _innerLog;

        public SuperSocketLog(ILog innerLog)
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
            _innerLog.Error(message.ToString());
        }

        public void Error(object message, Exception exception)
        {
            _innerLog.ErrorException(message.ToString(), exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            _innerLog.ErrorFormat(format, args);
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            var message = string.Format(provider, format, args);
            _innerLog.Error(message);
        }

        public void ErrorFormat(string format, object arg0)
        {
            _innerLog.ErrorFormat(format, arg0);
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            _innerLog.ErrorFormat(format, arg0, arg1);
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            _innerLog.ErrorFormat(format, arg0, arg1, arg2);
        }

        public void Warn(object message)
        {
            _innerLog.Warn(message.ToString());
        }

        public void Warn(object message, Exception exception)
        {
            _innerLog.WarnException(message.ToString(), exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            _innerLog.WarnFormat(format, args);
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            var message = string.Format(provider, format, args);
            _innerLog.Warn(message);
        }

        public void WarnFormat(string format, object arg0)
        {
            _innerLog.WarnFormat(format, arg0);
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            _innerLog.WarnFormat(format, arg0, arg1);
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            _innerLog.WarnFormat(format, arg0, arg1, arg2);
        }

        public void Info(object message)
        {
            _innerLog.Info(message.ToString());
        }

        public void Info(object message, Exception exception)
        {
            _innerLog.InfoException(message.ToString(), exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            _innerLog.InfoFormat(format, args);
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            var message = string.Format(provider, format, args);
            _innerLog.Info(message);
        }

        public void InfoFormat(string format, object arg0)
        {
            _innerLog.InfoFormat(format, arg0);
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            _innerLog.InfoFormat(format, arg0, arg1);
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            _innerLog.InfoFormat(format, arg0, arg1, arg2);
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
    }
}