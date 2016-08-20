namespace Zen.Trunk.Storage.Locking
{
	internal interface ILockHandler
	{
		/// <summary>
		/// Gets/sets maximum number of free locks in the lock pool.
		/// </summary>
		int MaxFreeLocks
		{
			get;
			set;
		}
		/// <summary>
		/// Gets the active lock count.
		/// </summary>
		int ActiveLockCount
		{
			get;
		}

		/// <summary>
		/// Gets the free lock count.
		/// </summary>
		int FreeLockCount
		{
			get;
		}

		/// <summary>
		/// Adds at most maxLocks locks to free pool.
		/// </summary>
		/// <param name="maxLocks"></param>
		void PopulateFreeLockPool(int maxLocks);
	}
}
