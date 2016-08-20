namespace Zen.Trunk.Storage.Locking
{
	using System;
	using System.Threading;

	public class RLock
	{
		#region Private Fields
		private object readLock = new object();
		private int readCount = 0;
		private object writeLock = new object();
		private int lockCount = 0;
		#endregion

		#region Public Constructors
		public RLock()
		{
		}
		#endregion

		#region Public Properties
		public int LockCount
		{
			get
			{
				return lockCount;
			}
		}
		#endregion

		#region Public Methods
		public void Lock(TimeSpan timeout, bool writable)
		{
			Interlocked.Increment(ref lockCount);
			try
			{
				DateTime start = DateTime.Now;
				if (!writable)
				{
					// If we can lock read then we are in
					if (!Monitor.TryEnter(readLock, timeout))
					{
						throw new LockException("Failed to acquire read lock on RLock.");
					}

					// Increment number of readers and exit
					Interlocked.Increment(ref readCount);
					Monitor.Exit(readLock);
				}
				else
				{
					// Get read lock first - to block new readers
					if (!Monitor.TryEnter(readLock, timeout))
					{
						throw new LockException("Failed to acquire read lock on RLock.");
					}

					// Wait for read count to fall to zero
					while (readCount > 0)
					{
						if ((DateTime.Now - start) < timeout)
						{
							Thread.Sleep(50);
						}
						else
						{
							Monitor.Exit(readLock);
							throw new LockException("Timeout acquiring write lock while waiting for concurrent readers to release RLock.");
						}
					}

					// Get write lock second
					if (!Monitor.TryEnter(writeLock, timeout))
					{
						Monitor.Exit(readLock);
						throw new LockException("Failed to acquire write lock on RLock.");
					}
				}
			}
			catch
			{
				Interlocked.Decrement(ref lockCount);
				throw;
			}
		}

		public bool Unlock(bool writable)
		{
			if (!writable)
			{
				Interlocked.Decrement(ref readCount);
			}
			else
			{
				Monitor.Exit(writeLock);
				Monitor.Exit(readLock);
			}
			return Interlocked.Decrement(ref lockCount) == 0;
		}
		#endregion
	}
}
