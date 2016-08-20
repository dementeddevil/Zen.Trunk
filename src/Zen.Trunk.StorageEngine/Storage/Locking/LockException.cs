namespace Zen.Trunk.Storage.Locking
{
	using System;

	[Serializable]
	public class LockException : Exception
	{
		public LockException()
			: this("Lock exception occurred.")
		{
		}

		public LockException(string message)
			: base(message)
		{
		}

		public LockException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

	/// <summary>
	/// Lock request has been aborted by deadlock detection.
	/// </summary>
	[Serializable]
	public class LockAbortedException : LockException
	{
		public LockAbortedException()
			: this("Lock Abort exception occurred.")
		{
		}

		public LockAbortedException(string message)
			: base(message)
		{
		}

		public LockAbortedException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

	[Serializable]
	public class LockTimeoutException : LockException
	{
		private TimeSpan _timeout;

		public LockTimeoutException()
		{
		}

		public LockTimeoutException(string message)
			: base(message)
		{
		}

		public LockTimeoutException(string message, TimeSpan timeout)
			: base(message)
		{
			_timeout = timeout;
		}

		public LockTimeoutException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public LockTimeoutException(string message, TimeSpan timeout, Exception innerException)
			: base(message, innerException)
		{
			_timeout = timeout;
		}

		public TimeSpan Timeout
		{
			get
			{
				return _timeout;
			}
		}
	}
}
