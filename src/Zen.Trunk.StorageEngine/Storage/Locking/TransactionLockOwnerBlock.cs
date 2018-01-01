using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Locking
{
	/// <summary>
	/// A <b>TransactionLockOwnerBlock</b> object tracks all the owned objects
	/// locked by a given transaction and their associated locked data pages.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This object maintains dictionaries of root locks, object schema locks,
	/// distribution lock owner blocks and data lock owner blocks.
	/// </para>
	/// <para>
	/// This class is the single entrypoint for acquiring page locks so that
	/// these can all be released in a coordinated fashion during transaction
	/// commit and rollback operations.
	/// Lock acquisition methods here are typically called by Page classes and
	/// make calls to the Database Lock Manager which in turn calls the Global
	/// Lock Manager.
	/// </para>
	/// </remarks>
	internal class TransactionLockOwnerBlock
	{
		#region Private Fields
		private readonly IDatabaseLockManager _lockManager;
		private readonly ConcurrentDictionary<FileGroupId, FileGroupLock> _rootLocks;
		private readonly ConcurrentDictionary<ObjectId, SchemaLock> _schemaLocks;
		private readonly ConcurrentDictionary<VirtualPageId, DistributionLockOwnerBlock> _distributionOwnerBlocks;
		private readonly ConcurrentDictionary<ObjectId, DataLockOwnerBlock> _dataOwnerBlocks;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Creates a TransactionLockOwnerBlock associated with the given
		/// Transaction ID.
		/// </summary>
		/// <param name="lockManager">Lock Manager</param>
		public TransactionLockOwnerBlock(IDatabaseLockManager lockManager)
		{
			_lockManager = lockManager;
			_rootLocks = new ConcurrentDictionary<FileGroupId, FileGroupLock>();
			_schemaLocks = new ConcurrentDictionary<ObjectId, SchemaLock>();
			_distributionOwnerBlocks = new ConcurrentDictionary<VirtualPageId, DistributionLockOwnerBlock>();
			_dataOwnerBlocks = new ConcurrentDictionary<ObjectId, DataLockOwnerBlock>();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Gets the original create root lock.
		/// </summary>
		/// <param name="fileGroupId">The file group unique identifier.</param>
		/// <returns></returns>
		public FileGroupLock GetOrCreateRootLock(FileGroupId fileGroupId)
		{
			return _rootLocks.GetOrAdd(
				fileGroupId,
				id => _lockManager.GetFileGroupLock(id));
		}

		/// <summary>
		/// Gets the original create schema lock.
		/// </summary>
		/// <param name="objectId">The object unique identifier.</param>
		/// <returns></returns>
		public SchemaLock GetOrCreateSchemaLock(ObjectId objectId)
		{
			return _schemaLocks.GetOrAdd(
				objectId,
				id => _lockManager.GetSchemaLock(id));
		}

		/// <summary>
		/// Gets a <see cref="DistributionLockOwnerBlock" /> object associated with the
		/// specified Object ID.
		/// </summary>
		/// <param name="virtualId">The virtual unique identifier.</param>
		/// <param name="maxExtentLocks">The maximum extent locks.</param>
		/// <returns>
		/// A <see cref="DistributionLockOwnerBlock"/> for the distribution page.
		/// </returns>
		public DistributionLockOwnerBlock GetOrCreateDistributionLockOwnerBlock(VirtualPageId virtualId, uint maxExtentLocks = 10)
		{
			return _distributionOwnerBlocks.GetOrAdd(
				virtualId,
				id => new DistributionLockOwnerBlock(_lockManager, id, maxExtentLocks));
		}

		/// <summary>
		/// Gets a <see cref="DataLockOwnerBlock" /> object associated with the
		/// specified Object ID.
		/// </summary>
		/// <param name="objectId">Object ID</param>
		/// <param name="maxPageLocks">The maximum page locks.</param>
		/// <returns>
		/// A <see cref="DataLockOwnerBlock"/> for the object.
		/// </returns>
		public DataLockOwnerBlock GetOrCreateDataLockOwnerBlock(ObjectId objectId, uint maxPageLocks = 100)
		{
			return _dataOwnerBlocks.GetOrAdd(
				objectId,
				id => new DataLockOwnerBlock(_lockManager, id, maxPageLocks));
		}

		/// <summary>
		/// Releases all locks held by this instance.
		/// </summary>
		/// <remarks>
		/// This method is called by transaction cleanup code after all buffers
		/// enlisted in the current transaction have been committed.
		/// </remarks>
		public async Task ReleaseAllAsync()
		{
			while (_rootLocks.Count > 0)
			{
				FileGroupLock lockObject;
				if (_rootLocks.TryRemove(_rootLocks.Keys.First(), out lockObject))
				{
					await lockObject.UnlockAsync().ConfigureAwait(false);
					lockObject.ReleaseRefLock();
				}
			}

			while (_schemaLocks.Count > 0)
			{
				SchemaLock lockObject;
				if (_schemaLocks.TryRemove(_schemaLocks.Keys.First(), out lockObject))
				{
					await lockObject.UnlockAsync().ConfigureAwait(false);
					lockObject.ReleaseRefLock();
				}
			}

			while (_distributionOwnerBlocks.Count > 0)
			{
				DistributionLockOwnerBlock block;
				if (_distributionOwnerBlocks.TryRemove(_distributionOwnerBlocks.Keys.First(), out block))
				{
					await block.ReleaseLocksAsync().ConfigureAwait(false);
					block.Dispose();
				}
			}

			while (_dataOwnerBlocks.Count > 0)
			{
				DataLockOwnerBlock block;
				if (_dataOwnerBlocks.TryRemove(_dataOwnerBlocks.Keys.First(), out block))
				{
					await block.ReleaseLocksAsync().ConfigureAwait(false);
					block.Dispose();
				}
			}
		}
		#endregion
	}
}
