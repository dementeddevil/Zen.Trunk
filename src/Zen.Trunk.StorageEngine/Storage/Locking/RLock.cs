namespace Zen.Trunk.Storage.Locking
{
	using System;
	using System.Threading;

	public class RLock
	{
		#region Private Fields
		private readonly object _readLock = new object();
		private readonly object _writeLock = new object();
		private int _readCount;
		private int _lockCount;
		#endregion

		#region Public Constructors
		public RLock()
		{
		}
		#endregion

		#region Public Properties
        /// <summary>
        /// Gets the current lock count.
        /// </summary>
		public int LockCount => _lockCount;
	    #endregion

		#region Public Methods
        /// <summary>
        /// Locks the resource protected by this lock.
        /// </summary>
        /// <param name="writable">
        /// <c>true</c> for a writable lock; otherwise <c>false</c> for a readable lock.
        /// </param>
        /// <param name="timeout"></param>
        /// <remarks>
        /// This operation will block for as long as necessary until the resource becomes available.
        /// </remarks>
		public void Lock(bool writable, TimeSpan timeout)
		{
			Interlocked.Increment(ref _lockCount);
			try
			{
				var start = DateTime.UtcNow;
				if (!writable)
				{
					// If we can lock read then we are in
					if (!Monitor.TryEnter(_readLock, timeout))
					{
						throw new LockException("Failed to acquire read lock on RLock.");
					}

					// Increment number of readers and exit
					Interlocked.Increment(ref _readCount);
					Monitor.Exit(_readLock);
				}
				else
				{
					// Get read lock first - to block new readers
					if (!Monitor.TryEnter(_readLock, timeout))
					{
						throw new LockException("Failed to acquire read lock on RLock.");
					}

					// Wait for read count to fall to zero
					while (_readCount > 0)
					{
						if ((DateTime.UtcNow - start) < timeout)
						{
							Thread.Sleep(50);
						}
						else
						{
							Monitor.Exit(_readLock);
							throw new LockException("Timeout acquiring write lock while waiting for concurrent readers to release RLock.");
						}
					}

					// Get write lock second
					if (!Monitor.TryEnter(_writeLock, timeout))
					{
						Monitor.Exit(_readLock);
						throw new LockException("Failed to acquire write lock on RLock.");
					}
				}
			}
			catch
			{
				Interlocked.Decrement(ref _lockCount);
				throw;
			}
		}

        /// <summary>
        /// Unlocks the resource protected by this lock.
        /// </summary>
        /// <param name="writable">
        /// <c>true</c> for a writable lock; otherwise <c>false</c> for a readable lock.
        /// </param>
        /// <returns></returns>
		public bool Unlock(bool writable)
		{
			if (!writable)
			{
				Interlocked.Decrement(ref _readCount);
			}
			else
			{
				Monitor.Exit(_writeLock);
				Monitor.Exit(_readLock);
			}
			return Interlocked.Decrement(ref _lockCount) == 0;
		}
		#endregion
	}
}
