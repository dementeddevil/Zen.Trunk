namespace Zen.Trunk.Storage.Locking
{
	using System;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
	public class LockException : Exception
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="LockException"/> class.
        /// </summary>
        public LockException()
			: this("Lock exception occurred.")
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="LockException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public LockException(string message)
			: base(message)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="LockException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public LockException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

    /// <summary>
    /// Lock request has been aborted by deadlock detection.
    /// </summary>
    [Serializable]
    public class LockRejectedException : LockException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LockRejectedException"/> class.
        /// </summary>
        public LockRejectedException()
            : this("Asynchronous lock operation was rejected.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockRejectedException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public LockRejectedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockRejectedException" /> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public LockRejectedException(string message, Exception innerException)
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
        /// <summary>
        /// Initializes a new instance of the <see cref="LockAbortedException"/> class.
        /// </summary>
        public LockAbortedException()
			: this("Lock Abort exception occurred.")
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="LockAbortedException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public LockAbortedException(string message)
			: base(message)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="LockAbortedException" /> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public LockAbortedException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Locking.LockException" />
    [Serializable]
	public class LockTimeoutException : LockException
	{
		private readonly TimeSpan _timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="LockTimeoutException" /> class.
        /// </summary>
        public LockTimeoutException()
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="LockTimeoutException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public LockTimeoutException(string message)
			: base(message)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="LockTimeoutException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeout">The timeout.</param>
        public LockTimeoutException(string message, TimeSpan timeout)
			: base(message)
		{
			_timeout = timeout;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="LockTimeoutException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public LockTimeoutException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="LockTimeoutException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="innerException">The inner exception.</param>
        public LockTimeoutException(string message, TimeSpan timeout, Exception innerException)
			: base(message, innerException)
		{
			_timeout = timeout;
		}

        /// <summary>
        /// Gets the timeout.
        /// </summary>
        /// <value>
        /// The timeout.
        /// </value>
        public TimeSpan Timeout => _timeout;
	}
}
