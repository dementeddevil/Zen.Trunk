// -----------------------------------------------------------------------
// <copyright file="TimeoutHelper.cs" company="Zen Design Corp">
// TODO: Update copyright text.
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

		public TimeoutHelper(TimeSpan timeout)
		{
			if (timeout <= TimeSpan.Zero)
			{
				throw new ArgumentException("Timeout must be positive.");
			}

			_timeout = timeout;
		}

		public DateTime EndTime => _startTime + _timeout;

	    public TimeSpan RemainingTime => EndTime - DateTime.UtcNow;

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
