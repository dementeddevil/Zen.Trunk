namespace Zen.Trunk.Storage.Locking
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

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
	internal abstract class LockOwnerBlockBase<ItemLockIdType> : TraceableObject, IDisposable
	{
		#region Private Types
		private class ItemLockDictionary : Dictionary<ItemLockIdType, DataLock>
		{
			public bool TryReleaseLock(ItemLockIdType key)
			{
				var removed = false;
				if (ContainsKey(key))
				{
					var lockObject = this[key];
					if (lockObject != null)
					{
						lockObject.Unlock();
						lockObject.ReleaseRefLock();
					}
					removed = Remove(key);
				}
				return removed;
			}

			public void ReleaseLocks(Action nullLockAction)
			{
				foreach (var key in Keys.ToArray())
				{
					var lockObject = this[key];
					if (lockObject != null)
					{
						lockObject.Unlock();
						lockObject.ReleaseRefLock();
					}
					else if (nullLockAction != null)
					{
						nullLockAction();
					}
					Remove(key);
				}
			}
		}
		#endregion

		#region Private Fields
		private readonly IDatabaseLockManager _manager;
		private uint _ownerLockCount;
		private readonly uint _maxItemLocks;
		private readonly SpinLockClass _syncBlock;
		private ObjectLock _ownerLock;
		private readonly ItemLockDictionary _readLocks;
		private readonly ItemLockDictionary _updateLocks;
		private readonly ItemLockDictionary _writeLocks;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="LockOwnerBlockBase{ItemLockIdType}" /> class.
		/// </summary>
		/// <param name="manager">The database lock manager.</param>
		/// <param name="maxItemLocks">The maximum item locks before lock escalation occurs.</param>
		public LockOwnerBlockBase(IDatabaseLockManager manager, uint maxItemLocks)
		{
			_manager = manager;
			_maxItemLocks = maxItemLocks;
			_syncBlock = new SpinLockClass();
			_ownerLock = GetOwnerLock();
			_readLocks = new ItemLockDictionary();
			_updateLocks = new ItemLockDictionary();
			_writeLocks = new ItemLockDictionary();
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
			DisposeManagedObjects();
		}

		/// <summary>
		/// Locks the owner.
		/// </summary>
		/// <param name="lockType">Type of the lock.</param>
		/// <param name="timeout">The timeout.</param>
		public void LockOwner(ObjectLockType lockType, TimeSpan timeout)
		{
			ThrowIfDisposed();

			// TODO: Validate lock request
			_ownerLock.Lock(lockType, timeout);
		}

		/// <summary>
		/// Locks the item.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="lockType">Type of the lock.</param>
		/// <param name="timeout">The timeout.</param>
		public void LockItem(ItemLockIdType key, DataLockType lockType, TimeSpan timeout)
		{
			ThrowIfDisposed();

			// Skip none lock requests
			if (lockType == DataLockType.None)
			{
				return;
			}

			_syncBlock.Execute(
				() =>
				{
					if (lockType == DataLockType.Shared && !_readLocks.ContainsKey(key))
					{
						LockItemShared(key, timeout);
					}
					else if (lockType == DataLockType.Update && !_updateLocks.ContainsKey(key))
					{
						LockItemUpdate(key, timeout);
					}
					else if (lockType == DataLockType.Exclusive && !_writeLocks.ContainsKey(key))
					{
						LockItemExclusive(key, timeout);
					}
				});
		}

		/// <summary>
		/// Determines whether the specified owner lock is held by the
		/// transaction associated with the current thread.
		/// </summary>
		/// <param name="lockType">Type of the lock.</param>
		/// <returns></returns>
		public bool HasOwnerLock(ObjectLockType lockType)
		{
			if (lockType == ObjectLockType.None)
			{
				return true;
			}

			return _ownerLock.HasLock(lockType);
		}

		/// <summary>
		/// Determines whether the specified data lock is held for the
		/// specified key by the transaction associated with the current thread.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="lockType">Type of the lock.</param>
		/// <returns></returns>
		public bool HasItemLock(ItemLockIdType key, DataLockType lockType)
		{
			switch (lockType)
			{
				case DataLockType.None:
					return true;

				case DataLockType.Shared:
					if (_ownerLock.HasLock(ObjectLockType.Shared)||
						_ownerLock.HasLock(ObjectLockType.Exclusive))
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
					if (_ownerLock.HasLock(ObjectLockType.Exclusive))
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
					if (_ownerLock.HasLock(ObjectLockType.Exclusive))
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
		public void UnlockItem(ItemLockIdType key)
		{
			ThrowIfDisposed();

			// Locate id in tables
			_syncBlock.Execute(
				() =>
				{
					if (_readLocks.TryReleaseLock(key) ||
						_updateLocks.TryReleaseLock(key) ||
						_writeLocks.TryReleaseLock(key))
					{
						--_ownerLockCount;
					}
				});
		}

		/// <summary>
		/// Unlocks the owner.
		/// </summary>
		public void UnlockOwner()
		{
			ThrowIfDisposed();

			if (_ownerLockCount == 0)
			{
				//Tracer.WriteVerboseLine(
				//    "Unlocking lock owner block for object {0}", _objectId);
				_ownerLock.Unlock();
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
		public void ReleaseLocks()
		{
			ThrowIfDisposed();

			_writeLocks.ReleaseLocks(() => --_ownerLockCount);
			_updateLocks.ReleaseLocks(() => --_ownerLockCount);
			_readLocks.ReleaseLocks(() => --_ownerLockCount);
			UnlockOwner();
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
		protected virtual void DisposeManagedObjects()
		{
			if (_ownerLock != null)
			{
				_ownerLock.ReleaseRefLock();
				_ownerLock = null;
			}
		}

		/// <summary>
		/// Creates the tracer.
		/// </summary>
		/// <param name="tracerName">Name of the tracer.</param>
		/// <returns></returns>
		protected override ITracer CreateTracer(string tracerName)
		{
			return TS.CreateLockBlockTracer(tracerName);
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
		protected abstract DataLock GetItemLock(ItemLockIdType key);
		#endregion

		#region Private Methods
		private void LockItemShared(ItemLockIdType key, TimeSpan timeout)
		{
			if (_ownerLock.HasLock(ObjectLockType.Shared) ||
				_ownerLock.HasLock(ObjectLockType.Exclusive))
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
			if (!_ownerLock.HasLock(ObjectLockType.SharedIntentExclusive) &&
				!_ownerLock.HasLock(ObjectLockType.IntentExclusive) &&
				!_ownerLock.HasLock(ObjectLockType.IntentShared))
			{
				_ownerLock.Lock(ObjectLockType.IntentShared, timeout);
			}

			// Obtain an object data lock for the given key
			if (!_updateLocks.ContainsKey(key) &&
				!_writeLocks.ContainsKey(key))
			{
				// Get data lock object then lock accordingly
				var lockObj = GetItemLock(key); // lock is already addref'ed
				lockObj.Lock(DataLockType.Shared, timeout);
				_ownerLockCount++;

				// Add lock to the read-lock list
				_readLocks.Add(key, lockObj);

				// Check whether we can escalate this lock
				if (_readLocks.Count > _maxItemLocks)
				{
					Tracer.WriteVerboseLine("Attempting lock owner block SHARED lock escalation");

					var hasEscalatedLock = false;
					try
					{
						// Acquire full shared lock on owner
						_ownerLock.Lock(ObjectLockType.Shared, timeout);
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
							lockPair.Value.Unlock();
							lockPair.Value.ReleaseRefLock();
							_readLocks[lockPair.Key] = null;
						}
					}
				}
			}
		}

		private void LockItemUpdate(ItemLockIdType key, TimeSpan timeout)
		{
			if (_ownerLock.HasLock(ObjectLockType.Exclusive))
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
			if (!_ownerLock.HasLock(ObjectLockType.SharedIntentExclusive) &&
				!_ownerLock.HasLock(ObjectLockType.IntentExclusive))
			{
				if (_ownerLock.HasLock(ObjectLockType.Shared))
				{
					_ownerLock.Lock(ObjectLockType.SharedIntentExclusive, timeout);
				}
				else
				{
					_ownerLock.Lock(ObjectLockType.IntentExclusive, timeout);
				}
			}

			DataLock lockObj = null;

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
				lockObj.Lock(DataLockType.Update, timeout);
				_readLocks.Remove(key);
			}
			else
			{
				// Ever access this resource so get a new lock and attempt to
				//	acquire the update lock requested
				lockObj = GetItemLock(key);

				// TODO: Do we need to get a shared lock first?
				//lockObj.Lock(DataLockType.Shared, timeout);
				lockObj.Lock(DataLockType.Update, timeout);
				_ownerLockCount++;
			}
			_updateLocks.Add(key, lockObj);
		}

		private void LockItemExclusive(ItemLockIdType key, TimeSpan timeout)
		{
			if (_ownerLock.HasLock(ObjectLockType.Exclusive))
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
			if (!_ownerLock.HasLock(ObjectLockType.SharedIntentExclusive) &&
				!_ownerLock.HasLock(ObjectLockType.IntentExclusive))
			{
				if (_ownerLock.HasLock(ObjectLockType.Shared))
				{
					_ownerLock.Lock(ObjectLockType.SharedIntentExclusive, timeout);
				}
				else
				{
					_ownerLock.Lock(ObjectLockType.IntentExclusive, timeout);
				}
			}

			// Technically we can only obtain an exclusive lock via an update
			//	lock...
			// However we support attempts to gain an exclusive lock directly
			DataLock lockObj = null;
			if (_updateLocks.ContainsKey(key))
			{
				// TODO: If ObjectLock ever supports escalation of update locks
				//	then this code will need to be revised...
				lockObj = _updateLocks[key];
				lockObj.Lock(DataLockType.Exclusive, timeout);
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
				lockObj.Lock(DataLockType.Exclusive, timeout);
				_readLocks.Remove(key);
			}
			else
			{
				lockObj = GetItemLock(key);
				lockObj.Lock(DataLockType.Exclusive, timeout);
				_ownerLockCount++;
			}
			_writeLocks.Add(key, lockObj);

			// Check whether we can escalate this lock
			if (_writeLocks.Count > _maxItemLocks)
			{
				Tracer.WriteVerboseLine("Attempting lock owner block EXCLUSIVE lock escalation");

				var hasEscalated = false;
				try
				{
					// Acquire full exclusive lock on owner
					_ownerLock.Lock(ObjectLockType.Exclusive, timeout);
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
						lockPair.Value.Unlock();
						lockPair.Value.ReleaseRefLock();
						_readLocks[lockPair.Key] = null;
					}
					foreach (var lockPair in _updateLocks.ToArray())
					{
						lockPair.Value.Unlock();
						lockPair.Value.ReleaseRefLock();
						_updateLocks[lockPair.Key] = null;
					}
					foreach (var lockPair in _writeLocks.ToArray())
					{
						lockPair.Value.Unlock();
						lockPair.Value.ReleaseRefLock();
						_writeLocks[lockPair.Key] = null;
					}
				}
			}
		}
		#endregion
	}
}
