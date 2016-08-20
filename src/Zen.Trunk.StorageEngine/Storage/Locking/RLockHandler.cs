namespace Zen.Trunk.Storage.Locking
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Threading;

	internal class RLockHandler : ILockHandler
	{
		#region Private Fields
		private int maxFreeLocks = 100;
		//private SpinLockClass syncLocks = new SpinLockClass();
		private readonly ConcurrentDictionary<string, RLock> _activeLocks =
			new ConcurrentDictionary<string, RLock>();
		private readonly ObjectPool<RLock> _freeLocks =
			new ObjectPool<RLock>(
				() =>
				{
					return new RLock();
				});
		#endregion

		#region Public Constructors
		public RLockHandler()
		{
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Acquires a resource lock on the resource associated with the
		/// resource string.
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="timeout"></param>
		/// <param name="writable"></param>
		/// <remarks>
		/// A resource lock or RLock only support read and write locks.
		/// </remarks>
		public void LockResource(string resource, TimeSpan timeout, bool writable)
		{
			var lockObject = _activeLocks.GetOrAdd(
				resource, (key) => _freeLocks.GetObject());

			// Attempt to lock object
			lockObject.Lock(timeout, writable);
		}

		/// <summary>
		/// Releases a resource lock on the resource associated with the
		/// resource string.
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="writable"></param>
		public void UnlockResource(string resource, bool writable)
		{
			RLock lockObject;
			if(_activeLocks.TryGetValue(resource, out lockObject) &&
				lockObject.Unlock(writable) &&
				lockObject.LockCount == 0)
			{
				RLock temp;
				_activeLocks.TryRemove(resource, out temp);
				if (_freeLocks.Count < maxFreeLocks)
				{
					_freeLocks.PutObject(lockObject);
				}
			}
		}
		#endregion

		#region ILockHandler Members
		int ILockHandler.MaxFreeLocks
		{
			get
			{
				return maxFreeLocks;
			}
			set
			{
				Interlocked.Exchange(ref maxFreeLocks, value);
			}
		}
		int ILockHandler.ActiveLockCount => _activeLocks.Count;

	    int ILockHandler.FreeLockCount => _freeLocks.Count;

	    void ILockHandler.PopulateFreeLockPool(int maxLocks)
		{
			while ((_freeLocks.Count < maxFreeLocks) && (maxLocks > 0))
			{
				_freeLocks.PutObject(new RLock());
				--maxLocks;
			}
		}
		#endregion
	}
}
