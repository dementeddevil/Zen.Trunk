using System;
using SSocket = SuperSocket.SocketBase.Logging;
using Serilog;
using Serilog.Events;

namespace Zen.Trunk.Network
{
    internal class SuperSocketLog : SSocket.ILog
    {
        public SuperSocketLog()
        {
        }

        public bool IsFatalEnabled => Log.IsEnabled(LogEventLevel.Fatal);

        public bool IsErrorEnabled => Log.IsEnabled(LogEventLevel.Error);

        public bool IsWarnEnabled => Log.IsEnabled(LogEventLevel.Warning);

        public bool IsInfoEnabled => Log.IsEnabled(LogEventLevel.Information);

        public bool IsDebugEnabled => Log.IsEnabled(LogEventLevel.Debug);

        public void Fatal(object message)
        {
            Log.Fatal(message.ToString());
        }

        public void Fatal(object message, Exception exception)
        {
            Log.Fatal(exception, message.ToString());
        }

        public void FatalFormat(string format, params object[] args)
        {
            Log.Fatal(format, args);
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            Log.Fatal(format, args);
        }

        public void FatalFormat(string format, object arg0)
        {
            Log.Fatal(format, arg0);
        }

        public void FatalFormat(string format, object arg0, object arg1)
        {
            Log.Fatal(format, arg0, arg1);
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            Log.Fatal(format, arg0, arg1, arg2);
        }

        public void Error(object message)
        {
            Log.Error(message.ToString());
        }

        public void Error(object message, Exception exception)
        {
            Log.Error(exception, message.ToString());
        }

        public void ErrorFormat(string format, params object[] args)
        {
            Log.Error(format, args);
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            Log.Error(format, args);
        }

        public void ErrorFormat(string format, object arg0)
        {
            Log.Error(format, arg0);
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            Log.Error(format, arg0, arg1);
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            Log.Error(format, arg0, arg1, arg2);
        }

        public void Warn(object message)
        {
            Log.Warning(message.ToString());
        }

        public void Warn(object message, Exception exception)
        {
            Log.Warning(exception, message.ToString());
        }

        public void WarnFormat(string format, params object[] args)
        {
            Log.Warning(format, args);
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            Log.Warning(format, args);
        }

        public void WarnFormat(string format, object arg0)
        {
            Log.Warning(format, arg0);
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            Log.Warning(format, arg0, arg1);
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            Log.Warning(format, arg0, arg1, arg2);
        }

        public void Info(object message)
        {
            Log.Information(message.ToString());
        }

        public void Info(object message, Exception exception)
        {
            Log.Information(exception, message.ToString());
        }

        public void InfoFormat(string format, params object[] args)
        {
            Log.Information(format, args);
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            Log.Information(format, args);
        }

        public void InfoFormat(string format, object arg0)
        {
            Log.Information(format, arg0);
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            Log.Information(format, arg0, arg1);
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            Log.Information(format, arg0, arg1, arg2);
        }

        public void Debug(object message)
        {
            Log.Debug(message.ToString());
        }

        public void Debug(object message, Exception exception)
        {
            Log.Debug(exception, message.ToString());
        }

        public void DebugFormat(string format, params object[] args)
        {
            Log.Debug(format, args);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            var message = string.Format(provider, format, args);
            Log.Debug(message);
        }

        public void DebugFormat(string format, object arg0)
        {
            Log.Debug(format, arg0);
        }

        public void DebugFormat(string format, object arg0, object arg1)
        {
            Log.Debug(format, arg0, arg1);
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            Log.Debug(format, arg0, arg1, arg2);
        }
    }
}