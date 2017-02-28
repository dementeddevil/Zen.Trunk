using System;
using SuperSocket.SocketBase.Logging;
using LogExtensions = Zen.Trunk.Logging.LogExtensions;

namespace Zen.Trunk.Network
{
    internal class SuperSocketLog : ILog
    {
        private readonly Logging.ILog _innerLog;

        public SuperSocketLog(Zen.Trunk.Logging.ILog innerLog)
        {
            _innerLog = innerLog;
        }

        public bool IsFatalEnabled => LogExtensions.IsFatalEnabled(_innerLog);

        public bool IsErrorEnabled => LogExtensions.IsErrorEnabled(_innerLog);

        public bool IsWarnEnabled => LogExtensions.IsWarnEnabled(_innerLog);

        public bool IsInfoEnabled => LogExtensions.IsInfoEnabled(_innerLog);

        public bool IsDebugEnabled => LogExtensions.IsDebugEnabled(_innerLog);

        public void Fatal(object message)
        {
            LogExtensions.Fatal(_innerLog, message.ToString());
        }

        public void Fatal(object message, Exception exception)
        {
            LogExtensions.FatalException(_innerLog, message.ToString(), exception);
        }

        public void FatalFormat(string format, params object[] args)
        {
            LogExtensions.FatalFormat(_innerLog, format, args);
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            var message = string.Format(provider, format, args);
            LogExtensions.Fatal(_innerLog, message);
        }

        public void FatalFormat(string format, object arg0)
        {
            LogExtensions.FatalFormat(_innerLog, format, arg0);
        }

        public void FatalFormat(string format, object arg0, object arg1)
        {
            LogExtensions.FatalFormat(_innerLog, format, arg0, arg1);
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            LogExtensions.FatalFormat(_innerLog, format, arg0, arg1, arg2);
        }

        public void Error(object message)
        {
            LogExtensions.Error(_innerLog, message.ToString());
        }

        public void Error(object message, Exception exception)
        {
            LogExtensions.ErrorException(_innerLog, message.ToString(), exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            LogExtensions.ErrorFormat(_innerLog, format, args);
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            var message = string.Format(provider, format, args);
            LogExtensions.Error(_innerLog, message);
        }

        public void ErrorFormat(string format, object arg0)
        {
            LogExtensions.ErrorFormat(_innerLog, format, arg0);
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            LogExtensions.ErrorFormat(_innerLog, format, arg0, arg1);
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            LogExtensions.ErrorFormat(_innerLog, format, arg0, arg1, arg2);
        }

        public void Warn(object message)
        {
            LogExtensions.Warn(_innerLog, message.ToString());
        }

        public void Warn(object message, Exception exception)
        {
            LogExtensions.WarnException(_innerLog, message.ToString(), exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            LogExtensions.WarnFormat(_innerLog, format, args);
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            var message = string.Format(provider, format, args);
            LogExtensions.Warn(_innerLog, message);
        }

        public void WarnFormat(string format, object arg0)
        {
            LogExtensions.WarnFormat(_innerLog, format, arg0);
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            LogExtensions.WarnFormat(_innerLog, format, arg0, arg1);
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            LogExtensions.WarnFormat(_innerLog, format, arg0, arg1, arg2);
        }

        public void Info(object message)
        {
            LogExtensions.Info(_innerLog, message.ToString());
        }

        public void Info(object message, Exception exception)
        {
            LogExtensions.InfoException(_innerLog, message.ToString(), exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            LogExtensions.InfoFormat(_innerLog, format, args);
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            var message = string.Format(provider, format, args);
            LogExtensions.Info(_innerLog, message);
        }

        public void InfoFormat(string format, object arg0)
        {
            LogExtensions.InfoFormat(_innerLog, format, arg0);
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            LogExtensions.InfoFormat(_innerLog, format, arg0, arg1);
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            LogExtensions.InfoFormat(_innerLog, format, arg0, arg1, arg2);
        }

        public void Debug(object message)
        {
            LogExtensions.Debug(_innerLog, message.ToString());
        }

        public void Debug(object message, Exception exception)
        {
            LogExtensions.DebugException(_innerLog, message.ToString(), exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            LogExtensions.DebugFormat(_innerLog, format, args);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            var message = string.Format(provider, format, args);
            LogExtensions.Debug(_innerLog, message);
        }

        public void DebugFormat(string format, object arg0)
        {
            LogExtensions.DebugFormat(_innerLog, format, arg0);
        }

        public void DebugFormat(string format, object arg0, object arg1)
        {
            LogExtensions.DebugFormat(_innerLog, format, arg0, arg1);
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            LogExtensions.DebugFormat(_innerLog, format, arg0, arg1, arg2);
        }
    }
}