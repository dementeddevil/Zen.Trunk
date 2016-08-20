namespace Zen.Trunk.Torrent.Common
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class RateMonitor
	{
		private const int DefaultAveragePeriod = 12;

		private long _total;
		private int _rate;
		private int _rateIndex;
		private int[] _rates;
		private DateTime _lastUpdated;
		private int _bytesTransferredSinceLastUpdate;

		public RateMonitor()
			: this(DefaultAveragePeriod)
		{
		}

		public RateMonitor(int averagingPeriod)
		{
			this._lastUpdated = DateTime.UtcNow;
			this._rates = new int[averagingPeriod];
		}

		public int Rate
		{
			get
			{
				return this._rate;
			}
		}

		public long Total
		{
			get
			{
				return this._total;
			}
		}

		public void AddDelta(int bytesTransferred)
		{
			this._total += bytesTransferred;
			this._bytesTransferredSinceLastUpdate += bytesTransferred;
		}

		public void Reset()
		{
			this._total = 0;
			this._rate = 0;
			this._rateIndex = 0;
			this._bytesTransferredSinceLastUpdate = 0;
			this._lastUpdated = DateTime.UtcNow;
			for (int i = 0; i < this._rates.Length; i++)
			{
				_rates[i] = 0;
			}
		}

		public void Tick()
		{
			// Find how many milliseconds have passed since the last update and the current tick count
			DateTime utcNow = DateTime.UtcNow;
			double difference = (utcNow - this._lastUpdated).TotalMilliseconds;
			if (difference >= 800)
			{
				// Take the amount of bytes sent since the last tick and divide it by the number of seconds
				// since the last tick. This gives the calculated bytes/second transfer rate.
				// difference is in miliseconds, so divide by 1000 to get it in seconds
				this._rates[this._rateIndex++] = (int)((double)_bytesTransferredSinceLastUpdate * 1000.0 / difference);

				// If we've gone over the array bounds, reset to the first index
				// to start overwriting the old values
				if (this._rateIndex == _rates.Length)
				{
					this._rateIndex = 0;
				}

				// What we do here is add up all the bytes/second readings held in each array
				// and divide that by the number of non-zero entries. The number of non-zero entries
				// is given by ArraySize - count. This is to avoid the problem where a connection which
				// is just starting would have a lot of zero entries making the speed estimate inaccurate.
				int count = 0;
				int total = 0;
				for (int i = 0; i < this._rates.Length; i++)
				{
					if (this._rates[i] != 0)
					{
						count++;
					}

					total += this._rates[i];
				}

				// Update current average speed
				if (count == 0)
				{
					this._rate = 0;
				}
				else
				{
					this._rate = (total / count);
				}
				this._bytesTransferredSinceLastUpdate = 0;
				this._lastUpdated = utcNow;
			}
		}
	}
}
