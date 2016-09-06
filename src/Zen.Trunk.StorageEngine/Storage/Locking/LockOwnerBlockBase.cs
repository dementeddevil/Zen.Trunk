using System.Threading.Tasks;
using Zen.Trunk.CoordinationDataStructures;
using Zen.Trunk.Logging;

namespace Zen.Trunk.Storage.Locking
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	
	/// <summary>
	/// <c>LockOwnerBlockBase</c> contains the core functionality needed to
	/// track the lock objects on a database entity on behalf of a transaction.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This object is used in the implementation of lock escalation. When a
	/// LOB is created, the current number of pages owned by the database object
	/// will need to be given to the block so it will have an idea of when an
	/// appropriate number of data locks have been allocated on behalf of the
	/// current transaction and thus escalate locking appropriately.
	/// </para>
	/// <para>
	/// <b>Note:</b> Read, Update and Exclusive data locks are each counted
	/// seperately and any of these locks can cause an associated escalation
	/// on the owner lock object.
	/// </para>
	/// </remarks>
	internal abstract class LockOwnerBlockBase<TItemLockIdType> : IDisposable
	{
		#region Private Types
		private class ItemLockDictionary : Dictionary<TItemLockIdType, DataLock>
		{
			public async Task<bool> TryReleaseLockAsync(TItemLockIdType key)
			{
				var removed = false;
				if (ContainsKey(key))
				{
					var lockObject = this[key];
					if (lockObject != null)
					{
						await lockObject.UnlockAsync().ConfigureAwait(false);
						lockObject.ReleaseRefLock();
					}
					removed = Remove(key);
				}
				return removed;
			}

			public async Task ReleaseLocksAsync(Action nullLockAction)
			{
				foreach (var key in Keys.ToArray())
				{
					var lockObject = this[key];
					if (lockObject != null)
					{
						await lockObject.UnlockAsync().ConfigureAwait(false);
						lockObject.ReleaseRefLock();
					}
					else
					{
					    nullLockAction?.Invoke();
					}
				    Remove(key);
				}
			}
		}
		#endregion

		#region Private Fields
	    private static readonly ILog Logger = LogProvider.For<LockOwnerBlockBase<TItemLockIdType>>();

		private readonly IDatabaseLockManager _manager;
		private uint _ownerLockCount;
		private readonly uint _maxItemLocks;
        private readonly TransactionalSpinLock _sync = new TransactionalSpinLock();
        private ObjectLock _ownerLock;
		private readonly ItemLockDictionary _readLocks = new ItemLockDictionary();
		private readonly ItemLockDictionary _updateLocks = new ItemLockDictionary();
		private readonly ItemLockDictionary _writeLocks = new ItemLockDictionary();
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="LockOwnerBlockBase{TItemLockIdType}" /> class.
		/// </summary>
		/// <param name="manager">The database lock manager.</param>
		/// <param name="maxItemLocks">The maximum item locks before lock escalation occurs.</param>
		public LockOwnerBlockBase(IDatabaseLockManager manager, uint maxItemLocks)
		{
			_manager = manager;
			_maxItemLocks = maxItemLocks;
			_ownerLock = GetOwnerLock();
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the lock manager.
		/// </summary>
		/// <value>
		/// The lock manager.
		/// </value>
		protected IDatabaseLockManager LockManager => _manager;
	    #endregion

		#region Public Methods
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// Locks the owner.
		/// </summary>
		/// <param name="lockType">Type of the lock.</param>
		/// <param name="timeout">The timeout.</param>
		public Task LockOwnerAsync(ObjectLockType lockType, TimeSpan timeout)
		{
			ThrowIfDisposed();

			// TODO: Validate lock request
			return _ownerLock.LockAsync(lockType, timeout);
		}

		/// <summary>
		/// Locks the item.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="lockType">Type of the lock.</param>
		/// <param name="timeout">The timeout.</param>
		public async Task LockItemAsync(TItemLockIdType key, DataLockType lockType, TimeSpan timeout)
		{
			ThrowIfDisposed();

			// Skip none lock requests
			if (lockType == DataLockType.None)
			{
				return;
			}

			await _sync
                .ExecuteAsync(
				    async () =>
				    {
					    if (lockType == DataLockType.Shared && !_readLocks.ContainsKey(key))
					    {
						    await LockItemSharedAsync(key, timeout).ConfigureAwait(false);
					    }
					    else if (lockType == DataLockType.Update && !_updateLocks.ContainsKey(key))
					    {
						    await LockItemUpdateAsync(key, timeout).ConfigureAwait(false);
					    }
					    else if (lockType == DataLockType.Exclusive && !_writeLocks.ContainsKey(key))
					    {
						    await LockItemExclusiveAsync(key, timeout).ConfigureAwait(false);
					    }
				    })
                .ConfigureAwait(false);
		}

		/// <summary>
		/// Determines whether the specified owner lock is held by the
		/// transaction associated with the current thread.
		/// </summary>
		/// <param name="lockType">Type of the lock.</param>
		/// <returns></returns>
		public Task<bool> HasOwnerLockAsync(ObjectLockType lockType)
		{
			if (lockType == ObjectLockType.None)
			{
				return Task.FromResult(true);
			}

			return _ownerLock.HasLockAsync(lockType);
		}

		/// <summary>
		/// Determines whether the specified data lock is held for the
		/// specified key by the transaction associated with the current thread.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="lockType">Type of the lock.</param>
		/// <returns></returns>
		public async Task<bool> HasItemLockAsync(TItemLockIdType key, DataLockType lockType)
		{
			switch (lockType)
			{
				case DataLockType.None:
					return true;

				case DataLockType.Shared:
					if (await _ownerLock.HasLockAsync(ObjectLockType.Shared).ConfigureAwait(false) ||
						await _ownerLock.HasLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false))
					{
						return true;
					}
					if (_readLocks.ContainsKey(key) ||
						_updateLocks.ContainsKey(key) ||
						_writeLocks.ContainsKey(key))
					{
						return true;
					}
					break;

				case DataLockType.Update:
					if (await _ownerLock.HasLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false))
					{
						return true;
					}
					if (_writeLocks.ContainsKey(key) ||
						_updateLocks.ContainsKey(key))
					{
						return true;
					}
					break;

				case DataLockType.Exclusive:
					if (await _ownerLock.HasLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false))
					{
						return true;
					}
					if (_writeLocks.ContainsKey(key))
					{
						return true;
					}
					break;
			}

			return false;
		}

		/// <summary>
		/// Unlocks the item.
		/// </summary>
		/// <param name="key">The key.</param>
		public async Task UnlockItemAsync(TItemLockIdType key)
		{
			ThrowIfDisposed();

			// Locate id in tables
			await _sync.ExecuteAsync(
				async () =>
				{
					if (await _readLocks.TryReleaseLockAsync(key).ConfigureAwait(false) ||
						await _updateLocks.TryReleaseLockAsync(key).ConfigureAwait(false) ||
						await _writeLocks.TryReleaseLockAsync(key).ConfigureAwait(false))
					{
						--_ownerLockCount;
					}
				}).ConfigureAwait(false);
		}

		/// <summary>
		/// Unlocks the owner.
		/// </summary>
		public async Task UnlockOwnerAsync()
		{
			ThrowIfDisposed();

			if (_ownerLockCount == 0)
			{
				//Tracer.WriteVerboseLine(
				//    "Unlocking lock owner block for object {0}", _objectId);
				await _ownerLock.UnlockAsync().ConfigureAwait(false);
			}
			else
			{
				//Tracer.WriteVerboseLine(
				//    "Unlocking lock owner block for object {0} has been deferred {1} outstanding locks.",
				//    _objectId, _ownerLockCount);
			}
		}

		/// <summary>
		/// Releases all the held locks.
		/// </summary>
		public async Task ReleaseLocksAsync()
		{
			ThrowIfDisposed();

			await _writeLocks.ReleaseLocksAsync(() => --_ownerLockCount).ConfigureAwait(false);
			await _updateLocks.ReleaseLocksAsync(() => --_ownerLockCount).ConfigureAwait(false);
            await _readLocks.ReleaseLocksAsync(() => --_ownerLockCount).ConfigureAwait(false);
            await UnlockOwnerAsync().ConfigureAwait(false);
        }
		#endregion

		#region Protected Methods
		/// <summary>
		/// Throws if this instance has been disposed.
		/// </summary>
		/// <exception cref="System.ObjectDisposedException"></exception>
		protected void ThrowIfDisposed()
		{
			if (_ownerLock == null)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}

		/// <summary>
		/// Disposes the managed objects.
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (_ownerLock != null)
			{
				_ownerLock.ReleaseRefLock();
				_ownerLock = null;
			}
		}

		/// <summary>
		/// Gets the owner lock.
		/// </summary>
		/// <returns>
		/// An <see cref="ObjectLock"/> instance.
		/// </returns>
		protected abstract ObjectLock GetOwnerLock();

		/// <summary>
		/// Gets the item lock.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>
		/// An <see cref="DataLock"/> instance.
		/// </returns>
		protected abstract DataLock GetItemLock(TItemLockIdType key);
		#endregion

		#region Private Methods
		private async Task LockItemSharedAsync(TItemLockIdType key, TimeSpan timeout)
		{
			if (await _ownerLock.HasLockAsync(ObjectLockType.Shared).ConfigureAwait(false) ||
                await _ownerLock.HasLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false))
			{
				// We already have the desired lock on the
				//	owner, so increment the owner lock count
				_ownerLockCount++;
				return;
			}

			// Check if we already have a better lock on this resource
			if (_updateLocks.ContainsKey(key) ||
				_writeLocks.ContainsKey(key))
			{
				return;
			}

			// We require a minimum of an intent shared
			//	lock on the owner at this point
			if (!await _ownerLock.HasLockAsync(ObjectLockType.SharedIntentExclusive).ConfigureAwait(false) &&
				!await _ownerLock.HasLockAsync(ObjectLockType.IntentExclusive).ConfigureAwait(false) &&
				!await _ownerLock.HasLockAsync(ObjectLockType.IntentShared).ConfigureAwait(false))
			{
				await _ownerLock.LockAsync(ObjectLockType.IntentShared, timeout).ConfigureAwait(false);
			}

			// Obtain an object data lock for the given key
			if (!_updateLocks.ContainsKey(key) &&
				!_writeLocks.ContainsKey(key))
			{
				// Get data lock object then lock accordingly
				var lockObj = GetItemLock(key); // lock is already addref'ed
				await lockObj.LockAsync(DataLockType.Shared, timeout).ConfigureAwait(false);
				_ownerLockCount++;

				// Add lock to the read-lock list
				_readLocks.Add(key, lockObj);

				// Check whether we can escalate this lock
				if (_readLocks.Count > _maxItemLocks)
				{
				    if (Logger.IsDebugEnabled())
				    {
				        Logger.Debug("Attempting lock owner block SHARED lock escalation");
				    }

					var hasEscalatedLock = false;
					try
					{
						// Acquire full shared lock on owner
						await _ownerLock.LockAsync(ObjectLockType.Shared, timeout).ConfigureAwait(false);
						hasEscalatedLock = true;
					}
					catch
					{
						// Ignore error - we will attempt escalation on next lock
					}

					if (hasEscalatedLock)
					{
						// We get this far then we can release the existing read locks
						foreach (var lockPair in _readLocks.ToArray())
						{
							// Unlock and release reference
							// NOTE: We must leave a hanging key so we correctly
							//	decrement the owner lock count during unlock
							await lockPair.Value.UnlockAsync().ConfigureAwait(false);
							lockPair.Value.ReleaseRefLock();
							_readLocks[lockPair.Key] = null;
						}
					}
				}
			}
		}

		private async Task LockItemUpdateAsync(TItemLockIdType key, TimeSpan timeout)
		{
			if (await _ownerLock.HasLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false))
			{
				_ownerLockCount++;
				return;
			}

			// Check if we already have a better lock on this resource
			if (_writeLocks.ContainsKey(key))
			{
				return;
			}

			// We require a minimum of a shared intent exclusive lock on the
			//	owner at this point
			if (!await _ownerLock.HasLockAsync(ObjectLockType.SharedIntentExclusive).ConfigureAwait(false) &&
				!await _ownerLock.HasLockAsync(ObjectLockType.IntentExclusive).ConfigureAwait(false))
			{
				if (await _ownerLock.HasLockAsync(ObjectLockType.Shared).ConfigureAwait(false))
				{
					await _ownerLock.LockAsync(ObjectLockType.SharedIntentExclusive, timeout).ConfigureAwait(false);
				}
				else
				{
					await _ownerLock.LockAsync(ObjectLockType.IntentExclusive, timeout).ConfigureAwait(false);
				}
			}

			DataLock lockObj;

			// Check whether we have an existing read lock
			if (_readLocks.ContainsKey(key))
			{
				// Reuse the lock object if we still have it otherwise renew it
				lockObj = _readLocks[key];
				if (lockObj == null)
				{
					// We must have escalated read locks earlier
					//	so get distinct lock object from manager
					lockObj = GetItemLock(key);
				}

				// Attempt to upgrade the lock for this resource and remove
				//	read lock reference
				// NOTE: Do not update the reference count
				await lockObj.LockAsync(DataLockType.Update, timeout).ConfigureAwait(false);
				_readLocks.Remove(key);
			}
			else
			{
				// Ever access this resource so get a new lock and attempt to
				//	acquire the update lock requested
				lockObj = GetItemLock(key);

				// TODO: Do we need to get a shared lock first?
				//lockObj.Lock(DataLockType.Shared, timeout);
				await lockObj.LockAsync(DataLockType.Update, timeout).ConfigureAwait(false);
				_ownerLockCount++;
			}
			_updateLocks.Add(key, lockObj);
		}

		private async Task LockItemExclusiveAsync(TItemLockIdType key, TimeSpan timeout)
		{
			if (await _ownerLock.HasLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false))
			{
				_ownerLockCount++;
				return;
			}
			if (_writeLocks.ContainsKey(key))
			{
				return;
			}

			// We require a minimum of a shared intent exclusive lock on the
			//	owner at this point
			if (!await _ownerLock.HasLockAsync(ObjectLockType.SharedIntentExclusive).ConfigureAwait(false) &&
				!await _ownerLock.HasLockAsync(ObjectLockType.IntentExclusive).ConfigureAwait(false))
			{
				if (await _ownerLock.HasLockAsync(ObjectLockType.Shared).ConfigureAwait(false))
				{
					await _ownerLock.LockAsync(ObjectLockType.SharedIntentExclusive, timeout).ConfigureAwait(false);
				}
				else
				{
					await _ownerLock.LockAsync(ObjectLockType.IntentExclusive, timeout).ConfigureAwait(false);
				}
			}

			// Technically we can only obtain an exclusive lock via an update
			//	lock...
			// However we support attempts to gain an exclusive lock directly
			DataLock lockObj;
			if (_updateLocks.ContainsKey(key))
			{
				// TODO: If ObjectLock ever supports escalation of update locks
				//	then this code will need to be revised...
				lockObj = _updateLocks[key];
				await lockObj.LockAsync(DataLockType.Exclusive, timeout).ConfigureAwait(false);
				_updateLocks.Remove(key);
			}
			else if (_readLocks.ContainsKey(key))
			{
				lockObj = _readLocks[key];
				if (lockObj == null)
				{
					// We must have escalated read locks earlier
					//	so get distinct lock object from manager
					lockObj = GetItemLock(key);
				}
				await lockObj.LockAsync(DataLockType.Exclusive, timeout).ConfigureAwait(false);
				_readLocks.Remove(key);
			}
			else
			{
				lockObj = GetItemLock(key);
				await lockObj.LockAsync(DataLockType.Exclusive, timeout).ConfigureAwait(false);
				_ownerLockCount++;
			}
			_writeLocks.Add(key, lockObj);

			// Check whether we can escalate this lock
			if (_writeLocks.Count > _maxItemLocks)
			{
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Attempting lock owner block EXCLUSIVE lock escalation");
                }

				var hasEscalated = false;
				try
				{
					// Acquire full exclusive lock on owner
					await _ownerLock.LockAsync(ObjectLockType.Exclusive, timeout).ConfigureAwait(false);
					hasEscalated = true;
				}
				catch
				{
					// Ignore error - we will attempt escalation on next lock
				}

				if (hasEscalated)
				{
					// We get this far then we can release ALL existing locks
					foreach (var lockPair in _readLocks.ToArray())
					{
						await lockPair.Value.UnlockAsync().ConfigureAwait(false);
						lockPair.Value.ReleaseRefLock();
						_readLocks[lockPair.Key] = null;
					}
					foreach (var lockPair in _updateLocks.ToArray())
					{
						await lockPair.Value.UnlockAsync().ConfigureAwait(false);
						lockPair.Value.ReleaseRefLock();
						_updateLocks[lockPair.Key] = null;
					}
					foreach (var lockPair in _writeLocks.ToArray())
					{
						await lockPair.Value.UnlockAsync().ConfigureAwait(false);
						lockPair.Value.ReleaseRefLock();
						_writeLocks[lockPair.Key] = null;
					}
				}
			}
		}
		#endregion
	}
}
