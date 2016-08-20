namespace Zen.Trunk.Storage.Locking
{
	using System.Collections.Concurrent;
	using System.Linq;

	/// <summary>
	/// A <b>TransactionLockOwnerBlock</b> object tracks all the owned objects
	/// locked by a given transaction and their associated locked data pages.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This object actually maintains a dictionary of <see cref="DataLockOwnerBlock"/> 
	/// keyed against the Object ID of the owner.
	/// </para>
	/// <para>
	/// This class must become the single entrypoint for acquiring page locks
	/// so that these can all be released in a coordinated fashion during
	/// commit and rollback operations.
	/// This means that the implementation in the LockManager should call methods
	/// in this class and this class should call methods in the GlobalLockManager
	/// (via LockOwnerBlock derived classes as necessary)
	/// </para>
	/// </remarks>
	internal class TransactionLockOwnerBlock
	{
		#region Private Fields
		private IDatabaseLockManager _lockManager;
		private uint _transactionId;
		private ConcurrentDictionary<byte, RootLock> _rootLocks;
		private ConcurrentDictionary<uint, SchemaLock> _schemaLocks;
		private ConcurrentDictionary<ulong, DistributionLockOwnerBlock> _distributionOwnerBlocks;
		private ConcurrentDictionary<uint, DataLockOwnerBlock> _dataOwnerBlocks;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Creates a TransactionLockOwnerBlock associated with the given
		/// Transaction ID.
		/// </summary>
		/// <param name="lockManager">Lock Manager</param>
		/// <param name="transactionId">Transaction ID</param>
		public TransactionLockOwnerBlock(IDatabaseLockManager lockManager, uint transactionId)
		{
			_lockManager = lockManager;
			_transactionId = transactionId;
			_rootLocks = new ConcurrentDictionary<byte, RootLock>();
			_schemaLocks = new ConcurrentDictionary<uint, SchemaLock>();
			_distributionOwnerBlocks = new ConcurrentDictionary<ulong, DistributionLockOwnerBlock>();
			_dataOwnerBlocks = new ConcurrentDictionary<uint, DataLockOwnerBlock>();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Gets the original create root lock.
		/// </summary>
		/// <param name="fileGroupId">The file group unique identifier.</param>
		/// <returns></returns>
		public RootLock GetOrCreateRootLock(byte fileGroupId)
		{
			return _rootLocks.GetOrAdd(
				fileGroupId,
				(id) =>
				{
					return _lockManager.GetRootLock(id);
				});
		}

		/// <summary>
		/// Gets the original create schema lock.
		/// </summary>
		/// <param name="objectId">The object unique identifier.</param>
		/// <returns></returns>
		public SchemaLock GetOrCreateSchemaLock(uint objectId)
		{
			return _schemaLocks.GetOrAdd(
				objectId,
				(id) =>
				{
					return _lockManager.GetSchemaLock(id);
				});
		}

		/// <summary>
		/// Gets a <see cref="DistributionLockOwnerBlock" /> object associated with the
		/// specified Object ID.
		/// </summary>
		/// <param name="virtualId">The virtual unique identifier.</param>
		/// <param name="maxExtentLocks">The maximum extent locks.</param>
		/// <returns>
		/// Lock Owner Block
		/// </returns>
		public DistributionLockOwnerBlock GetOrCreateDistributionLockOwnerBlock(ulong virtualId, uint maxExtentLocks = 10)
		{
			return _distributionOwnerBlocks.GetOrAdd(
				virtualId,
				(id) =>
				{
					return new DistributionLockOwnerBlock(_lockManager, id, maxExtentLocks);
				});
		}

		/// <summary>
		/// Gets a <see cref="DataLockOwnerBlock" /> object associated with the
		/// specified Object ID.
		/// </summary>
		/// <param name="objectId">Object ID</param>
		/// <param name="maxPageLocks">The maximum page locks.</param>
		/// <returns>
		/// Lock Owner Block
		/// </returns>
		public DataLockOwnerBlock GetOrCreateDataLockOwnerBlock(uint objectId, uint maxPageLocks = 100)
		{
			return _dataOwnerBlocks.GetOrAdd(
				objectId,
				(id) =>
				{
					return new DataLockOwnerBlock(_lockManager, id, maxPageLocks);
				});
		}

		/// <summary>
		/// Releases all locks held by all <see cref="DataLockOwnerBlock"/> and
		/// <see cref="DistributionLockOwnerBlock"/> objects.
		/// </summary>
		/// <remarks>
		/// This method is called by transaction cleanup code after all buffers
		/// enlisted in the current transaction have been committed.
		/// </remarks>
		public void ReleaseAll()
		{
			while (_rootLocks.Count > 0)
			{
				RootLock lockObject = null;
				if (_rootLocks.TryRemove(_rootLocks.Keys.First(), out lockObject))
				{
					lockObject.Unlock();
					lockObject.ReleaseRefLock();
				}
			}

			while (_schemaLocks.Count > 0)
			{
				SchemaLock lockObject = null;
				if (_schemaLocks.TryRemove(_schemaLocks.Keys.First(), out lockObject))
				{
					lockObject.Unlock();
					lockObject.ReleaseRefLock();
				}
			}

			while (_distributionOwnerBlocks.Count > 0)
			{
				DistributionLockOwnerBlock block = null;
				if (_distributionOwnerBlocks.TryRemove(_distributionOwnerBlocks.Keys.First(), out block))
				{
					block.ReleaseLocks();
					block.Dispose();
				}
			}

			while (_dataOwnerBlocks.Count > 0)
			{
				DataLockOwnerBlock block = null;
				if (_dataOwnerBlocks.TryRemove(_dataOwnerBlocks.Keys.First(), out block))
				{
					block.ReleaseLocks();
					block.Dispose();
				}
			}
		}
		#endregion
	}
}
