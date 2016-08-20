//
// RateLimiter.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Threading;

namespace Zen.Trunk.Torrent.Client
{
	public class RateLimiter
	{
		#region Private Fields
		private int _savedError;
		private int _chunks;
		#endregion

		#region Public Properties
		public int Chunks
		{
			get
			{
				return _chunks;
			}
		}
		#endregion

		#region Public Methods
		public void DecrementChunks()
		{
			Interlocked.Decrement(ref _chunks);
		}

		public void AdjustChunks(int delta)
		{
			Interlocked.Add(ref _chunks, delta);
		}

		public void UpdateChunks(int maxRate, int actualRate)
		{
			if (maxRate == 0)
			{
				Interlocked.Exchange(ref _chunks, 0);
			}
			else
			{
				// From experimentation, i found that increasing by 5% gives more accuate rate limiting
				// for peer communications. For disk access and whatnot, a 5% overshoot is fine.
				maxRate = (int)(maxRate * 1.05);
				int errorRateDown = maxRate - actualRate;
				int delta = (int)(0.4 * errorRateDown + 0.6 * _savedError);
				_savedError = errorRateDown;

				// Determine delta change to chunk count and calculate new chunk value
				int increaseAmount = (int)((maxRate + delta) / ConnectionManager.ChunkLength);
				int newChunks = _chunks + increaseAmount;

				// Keep chunk count within bounds
				int minChunks = (int)(maxRate * 1.2 / ConnectionManager.ChunkLength);
				int maxChunks = (int)(maxRate * 2.0 / ConnectionManager.ChunkLength);
				newChunks = Math.Max(minChunks, Math.Min(maxChunks, newChunks));

				// Exchange to chunk property field
				Interlocked.Exchange(ref _chunks, newChunks);
			}
		}
		#endregion
	}
}
