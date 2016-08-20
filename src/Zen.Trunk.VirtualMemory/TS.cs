namespace Zen.Trunk
{
	using System.Diagnostics;
	using System.Security;

	public interface ITracer
	{
		void WriteErrorLine(string format, params object[] args);
		void WriteWarningLine(string format, params object[] args);
		void WriteInfoLine(string format, params object[] args);
		void WriteVerboseLine(string format, params object[] args);
	}

	/// <summary>
	/// Exposes trace switches for the core system.
	/// </summary>
	public static class TS
	{
		#region Private Objects
		private class Tracer : ITracer
		{
			private TraceSwitch _switch;
			private string _category;

			[SecurityCritical]
			public Tracer(TraceSwitch traceSwitch, string category)
			{
				_switch = traceSwitch;
#if DEBUG
				_switch.Level = TraceLevel.Verbose;
#endif
				_category = category;
			}

			public void WriteErrorLine(string format, params object[] args)
			{
#if USE_INTELLITRACE
				if (_switch.TraceError)
				{
					string text = string.Format(format, args);
					Trace.TraceError("{0}:{1}", _category, text);
				}
#else
				Trace.WriteLineIf(
					_switch.TraceError,
					string.Format(format, args),
					_category);
#endif
			}

			public void WriteWarningLine(string format, params object[] args)
			{
#if USE_INTELLITRACE
				if (_switch.TraceWarning)
				{
					string text = string.Format(format, args);
					Trace.TraceWarning("{0}:{1}", _category, text);
				}
#else
				Trace.WriteLineIf(
					_switch.TraceWarning,
					string.Format(format, args),
					_category);
#endif
			}

			public void WriteInfoLine(string format, params object[] args)
			{
#if USE_INTELLITRACE
				if (_switch.TraceInfo)
				{
					string text = string.Format(format, args);
					Trace.TraceInformation("{0}:{1}", _category, text);
				}
#else
				Trace.WriteLineIf(
					_switch.TraceInfo,
					string.Format(format, args),
					_category);
#endif
			}

			public void WriteVerboseLine(string format, params object[] args)
			{
#if USE_INTELLITRACE
				if (_switch.TraceVerbose)
				{
					string text = string.Format(format, args);
					Trace.TraceInformation("{0}:{1}", _category, text);
				}
#else
				Trace.WriteLineIf(
					_switch.TraceVerbose,
					string.Format(format, args),
					_category);
#endif
			}
		}
		#endregion

		#region Private Fields
		private const string NamespacePrefix = "Zen.Trunk.";
		private static readonly TraceSwitch _coreSwitch =
			new TraceSwitch(NamespacePrefix + "Core", "Core runtime debug switch", "Verbose");
		private static readonly TraceSwitch _serviceSwitch =
			new TraceSwitch(NamespacePrefix + "Services", "Services debug switch", "Verbose");
		private static readonly TraceSwitch _portTrackingSwitch =
			new TraceSwitch(NamespacePrefix + "PortTracking", "Port Tracking debug switch", "Verbose");
		private static readonly TraceSwitch _spinLockSwitch =
			new TraceSwitch(NamespacePrefix + "SpinLock", "Controls trace output for spin-lock objects.", "Error");
		private static readonly TraceSwitch _lockBlockSwitch =
			new TraceSwitch(NamespacePrefix + "LockBlock", "Controls trace output for lock owner block objects.", "Error");
		private static readonly TraceSwitch _transactionLockSwitch =
			new TraceSwitch(NamespacePrefix + "TxnLock", "Controls trace output for transaction-lock objects.", "Error");
		private static readonly TraceSwitch _pageBufferSwitch =
			new TraceSwitch(NamespacePrefix + "PageBuffer", "Controls trace output for page buffer objects.", "Error");
		private static readonly TraceSwitch _pageDeviceSwitch =
			new TraceSwitch(NamespacePrefix + "PageDevice", "Controls trace output for page device objects.", "Error");
		#endregion

		#region Public Methods
		[SecurityCritical]
		public static ITracer CreateCoreTracer(string category)
		{
			return CreateTracer(_coreSwitch, category);
		}

		[SecurityCritical]
		public static ITracer CreateServiceTracer(string category)
		{
			return CreateTracer(_serviceSwitch, category);
		}

		[SecurityCritical]
		public static ITracer CreatePortTrackingTracer(string category)
		{
			return CreateTracer(_portTrackingSwitch, category);
		}

		[SecurityCritical]
		public static ITracer CreateSpinLockTracer(string category)
		{
			return CreateTracer(_spinLockSwitch, category);
		}

		[SecurityCritical]
		public static ITracer CreateTransactionLockTracer(string category)
		{
			return CreateTracer(_transactionLockSwitch, category);
		}

		[SecurityCritical]
		public static ITracer CreateLockBlockTracer(string category)
		{
			return CreateTracer(_lockBlockSwitch, category);
		}

		[SecurityCritical]
		public static ITracer CreatePageBufferTracer(string category)
		{
			return CreateTracer(_pageBufferSwitch, category);
		}

		[SecurityCritical]
		public static ITracer CreatePageDeviceTracer(string category)
		{
			return CreateTracer(_pageDeviceSwitch, category);
		}
		#endregion

		#region Protected Methods
		#endregion

		#region Private Methods
		private static Tracer CreateTracer(TraceSwitch traceSwitch, string category)
		{
			return new Tracer(traceSwitch, category);
		}
		#endregion
	}
}
