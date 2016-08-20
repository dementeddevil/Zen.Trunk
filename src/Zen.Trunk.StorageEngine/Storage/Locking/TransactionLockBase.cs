namespace Zen.Trunk.Storage.Locking
{
	using System.Threading;

	/// <summary>
	/// <c>TransactionLockBase</c> serves as the base class for all transaction
	/// lock objects.
	/// </summary>
	public abstract class TransactionLockBase
	{
#if TRACE
		private static ITracer tracer = TS.CreateTransactionLockTracer("TransactionLock");
		private static int nextTransactionLockId = 0;
		private int transactionLockId;
#endif

		protected TransactionLockBase()
		{
			transactionLockId = Interlocked.Increment(ref nextTransactionLockId);
		}

#if TRACE
		protected virtual string GetTracePrefix()
		{
			return string.Format("{0} {1:X8}", GetType().Name, transactionLockId);
		}
#endif

		protected void TraceError(string format, params object[] args)
		{
#if TRACE
			string prefix = "[" + GetTracePrefix() + "] ";
			tracer.WriteErrorLine(prefix + format, args);
#endif
		}

		protected void TraceWarning(string format, params object[] args)
		{
#if TRACE
			string prefix = "[" + GetTracePrefix() + "] ";
			tracer.WriteWarningLine(prefix + format, args);
#endif
		}

		protected void TraceInformation(string format, params object[] args)
		{
#if TRACE
			string prefix = "[" + GetTracePrefix() + "] ";
			tracer.WriteInfoLine(prefix + format, args);
#endif
		}

		protected void TraceVerbose(string format, params object[] args)
		{
#if TRACE
			string prefix = "[" + GetTracePrefix() + "] ";
			tracer.WriteVerboseLine(prefix + format, args);
#endif
		}
	}
}
