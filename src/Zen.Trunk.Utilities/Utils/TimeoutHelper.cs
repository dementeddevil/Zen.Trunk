// -----------------------------------------------------------------------
// <copyright file="TimeoutHelper.cs" company="Zen Design Software">
// © Zen Design Software 2009 - 2016
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.Utils
{
	using System;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class TimeoutHelper
	{
		private readonly DateTime _startTime = DateTime.UtcNow;
		private readonly TimeSpan _timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutHelper"/> class.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <exception cref="ArgumentException">Timeout must be positive.</exception>
        public TimeoutHelper(TimeSpan timeout)
		{
			if (timeout <= TimeSpan.Zero)
			{
				throw new ArgumentException("Timeout must be positive.");
			}

			_timeout = timeout;
		}

        /// <summary>
        /// Gets the end time.
        /// </summary>
        /// <value>
        /// The end time.
        /// </value>
        public DateTime EndTime => _startTime + _timeout;

        /// <summary>
        /// Gets the remaining time.
        /// </summary>
        /// <value>
        /// The remaining time.
        /// </value>
        public TimeSpan RemainingTime => EndTime - DateTime.UtcNow;

        /// <summary>
        /// Gets a value indicating whether this instance is expired.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is expired; otherwise, <c>false</c>.
        /// </value>
        public bool IsExpired => RemainingTime < TimeSpan.Zero;

	    /// <summary>
		/// Gets the remaining time or throws a <see cref="TimeoutException"/>
		/// if timeout has elapsed.
		/// </summary>
		/// <returns></returns>
		public TimeSpan GetRemainingTimeOrThrowIfTimeout()
		{
			return GetRemainingTimeOrThrowIfTimeout(null);
		}

		/// <summary>
		/// Gets the remaining time or throws a <see cref="TimeoutException"/>
		/// if timeout has elapsed.
		/// </summary>
		/// <returns></returns>
		public TimeSpan GetRemainingTimeOrThrowIfTimeout(string errorMessage)
		{
			var remainingTime = RemainingTime;
			if (remainingTime < TimeSpan.Zero)
			{
				if (string.IsNullOrEmpty(errorMessage))
				{
					throw new TimeoutException();
				}
				else
				{
					throw new TimeoutException(errorMessage);
				}
			}

			return remainingTime;
		}
	}
}
