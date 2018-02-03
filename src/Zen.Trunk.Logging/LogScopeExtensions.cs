using System;
using System.Diagnostics;

namespace Zen.Trunk.Logging
{
    /// <summary>
    /// Extension methods to <see cref="ILog"/> that expose helpers for
    ///  logging the start and end of an operation
    /// </summary>
    public static class LogScopeExtensions
    {
        private class DisposableActionScope : IDisposable
        {
            private readonly Action _logEnd;
            private readonly Func<bool> _logEnabled;
            private bool _disposed;

            public DisposableActionScope(
                Action logStart,
                Action logEnd,
                Func<bool> logEnabled = null)
            {
                _logEnabled = logEnabled;
                _logEnd = logEnd;

                if (_logEnabled == null || _logEnabled())
                {
                    logStart();
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }

            private void Dispose(bool disposing)
            {
                if (disposing && !_disposed)
                {
                    _disposed = true;
                    if (_logEnabled == null || _logEnabled())
                    {
                        _logEnd();
                    }
                }
            }
        }


        public static IDisposable BeginDebugLogScope(
            this ILog logger, string enterScopeMessage, Func<string> exitScopeMessage)
        {
            return new DisposableActionScope(
                () => logger.Debug(enterScopeMessage),
                () => logger.Debug(exitScopeMessage()),
                logger.IsDebugEnabled);
        }

        public static IDisposable BeginInfoLogScope(
            this ILog logger, string enterScopeMessage, Func<string> exitScopeMessage)
        {
            return new DisposableActionScope(
                () => logger.Info(enterScopeMessage),
                () => logger.Info(exitScopeMessage()),
                logger.IsInfoEnabled);
        }

        public static IDisposable BeginDebugTimingLogScope(
            this ILog logger, string blockName)
        {
            var sw = new Stopwatch();
            return new DisposableActionScope(
                () =>
                {
                    logger.Debug($"ENTER : {blockName}");
                    sw.Start();
                },
                () =>
                {
                    sw.Stop();
                    var elapsed = sw.Elapsed;
                    logger.Debug($"LEAVE : {blockName} => Elapsed time: {elapsed.TotalMilliseconds}ms");
                },
                logger.IsDebugEnabled);
        }

        public static IDisposable BeginInfoTimingLogScope(
            this ILog logger, string blockName)
        {
            var sw = new Stopwatch();
            return new DisposableActionScope(
                () =>
                {
                    logger.Info($"ENTER : {blockName}");
                    sw.Start();
                },
                () =>
                {
                    sw.Stop();
                    var elapsed = sw.Elapsed;
                    logger.Info($"LEAVE : {blockName} => Elapsed time: {elapsed.TotalMilliseconds}ms");
                },
                logger.IsInfoEnabled);
        }
    }
}
