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
	/// This object maintains dictionaries of file-group locks, object schema
	/// locks, distribution lock owner blocks and data lock owner blocks.
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
		private readonly ConcurrentDictionary<FileGroupId, IFileGroupLock> _fileGroupLocks;
		private readonly ConcurrentDictionary<ObjectId, ISchemaLock> _schemaLocks;
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
			_fileGroupLocks = new ConcurrentDictionary<FileGroupId, IFileGroupLock>();
			_schemaLocks = new ConcurrentDictionary<ObjectId, ISchemaLock>();
			_distributionOwnerBlocks = new ConcurrentDictionary<VirtualPageId, DistributionLockOwnerBlock>();
			_dataOwnerBlocks = new ConcurrentDictionary<ObjectId, DataLockOwnerBlock>();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Gets (or creates) a file-group lock.
		/// </summary>
		/// <param name="fileGroupId">The file group unique identifier.</param>
		/// <returns></returns>
		public IFileGroupLock GetOrCreateFileGroupLock(FileGroupId fileGroupId)
		{
			return _fileGroupLocks.GetOrAdd(
				fileGroupId,
				id => _lockManager.GetFileGroupLock(id));
		}

		/// <summary>
		/// Gets (or creates) a schema lock.
		/// </summary>
		/// <param name="objectId">The object unique identifier.</param>
		/// <returns></returns>
		public ISchemaLock GetOrCreateSchemaLock(ObjectId objectId)
		{
			return _schemaLocks.GetOrAdd(
				objectId,
				id => _lockManager.GetSchemaLock(id));
		}

		/// <summary>
		/// Gets (or creates) a distribution lock owner block.
		/// </summary>
		/// <param name="virtualPageId">The virtual page unique identifier.</param>
		/// <param name="maxExtentLocks">The maximum extent locks.</param>
		/// <returns>
		/// A <see cref="DistributionLockOwnerBlock"/> for the distribution page.
		/// </returns>
		public DistributionLockOwnerBlock GetOrCreateDistributionLockOwnerBlock(
		    VirtualPageId virtualPageId, uint maxExtentLocks = 10)
		{
			return _distributionOwnerBlocks.GetOrAdd(
				virtualPageId,
				id => new DistributionLockOwnerBlock(_lockManager, id, maxExtentLocks));
		}

		/// <summary>
		/// Gets (or creates) a data lock owner block.
		/// </summary>
		/// <param name="objectId">Object ID</param>
		/// <param name="maxPageLocks">The maximum page locks.</param>
		/// <returns>
		/// A <see cref="DataLockOwnerBlock"/> for the object.
		/// </returns>
		public DataLockOwnerBlock GetOrCreateDataLockOwnerBlock(
		    ObjectId objectId, uint maxPageLocks = 100)
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
			while (_fileGroupLocks.Count > 0)
			{
                if (_fileGroupLocks.TryRemove(
                    _fileGroupLocks.Keys.First(),
                    out var lockObject))
                {
                    await lockObject.UnlockAsync().ConfigureAwait(false);
                    lockObject.ReleaseRefLock();
                }
            }

			while (_schemaLocks.Count > 0)
			{
                if (_schemaLocks.TryRemove(
                    _schemaLocks.Keys.First(),
                    out var lockObject))
                {
                    await lockObject.UnlockAsync().ConfigureAwait(false);
                    lockObject.ReleaseRefLock();
                }
            }

			while (_distributionOwnerBlocks.Count > 0)
			{
                if (_distributionOwnerBlocks.TryRemove(
                    _distributionOwnerBlocks.Keys.First(),
                    out var block))
                {
                    await block.ReleaseLocksAsync().ConfigureAwait(false);
                    block.Dispose();
                }
            }

			while (_dataOwnerBlocks.Count > 0)
			{
                if (_dataOwnerBlocks.TryRemove(
                    _dataOwnerBlocks.Keys.First(),
                    out var block))
                {
                    await block.ReleaseLocksAsync().ConfigureAwait(false);
                    block.Dispose();
                }
            }
		}
		#endregion
	}
}
