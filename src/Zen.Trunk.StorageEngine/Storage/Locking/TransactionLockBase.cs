using System.Threading;
using Zen.Trunk.Logging;

namespace Zen.Trunk.Storage.Locking
{
	/// <summary>
	/// <c>TransactionLockBase</c> serves as the base class for all transaction
	/// lock objects.
	/// </summary>
	public abstract class TransactionLockBase
	{
	    private static readonly ILog Logger = LogProvider.For<TransactionLockBase>();
		private static int _nextTransactionLockId;
		private readonly int _transactionLockId;

		protected TransactionLockBase()
		{
			_transactionLockId = Interlocked.Increment(ref _nextTransactionLockId);
		}

		protected virtual string GetTracePrefix()
		{
			return $"[{GetType().Name} {_transactionLockId:X8}]";
		}

		protected void TraceError(string format, params object[] args)
		{
		    if (Logger.IsErrorEnabled())
		    {
		        var prefix = GetTracePrefix();
		        var message = string.Format(format, args);
		        Logger.Error($"{prefix} {message}");
		    }
		}

		protected void TraceWarning(string format, params object[] args)
		{
            if (Logger.IsWarnEnabled())
            {
                var prefix = GetTracePrefix();
                var message = string.Format(format, args);
                Logger.Warn($"{prefix} {message}");
            }
        }

        protected void TraceInformation(string format, params object[] args)
		{
            if (Logger.IsInfoEnabled())
            {
                var prefix = GetTracePrefix();
                var message = string.Format(format, args);
                Logger.Info($"{prefix} {message}");
            }
        }

        protected void TraceVerbose(string format, params object[] args)
		{
            if (Logger.IsDebugEnabled())
            {
                var prefix = GetTracePrefix();
                var message = string.Format(format, args);
                Logger.Debug($"{prefix} {message}");
            }
        }
    }
}
