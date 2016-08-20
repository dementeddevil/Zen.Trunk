namespace Zen.Trunk.Storage.Locking
{
	/// <summary>
	/// Lock owner blocks track the locks for a given table or sample on
	/// behalf of a transaction.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This object is required in order to implement lock escalation. When
	/// a LOB is created the current number of pages owned by the ObjectID
	/// will need to be given to the block so it will have an idea when an
	/// appropriate number of data locks have been allocated on behalf of the
	/// current transaction.
	/// </para>
	/// <para>
	/// <b>Note:</b> Read, Update and Exclusive data locks are each counted seperately
	/// and any of these locks can cause an associated escalation on the owner
	/// lock object.
	/// </para>
	/// </remarks>
	internal class DataLockOwnerBlock : LockOwnerBlockBase<LogicalPageId>
	{
		#region Private Fields
		private readonly ObjectId _objectId;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DataLockOwnerBlock"/> class.
		/// </summary>
		/// <param name="manager">The manager.</param>
		/// <param name="objectId">The object unique identifier.</param>
		/// <param name="maxPageLocks">The maximum page locks.</param>
		public DataLockOwnerBlock(IDatabaseLockManager manager, ObjectId objectId, uint maxPageLocks = 100)
			: base(manager, maxPageLocks)
		{
			_objectId = objectId;
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Gets the owner lock.
		/// </summary>
		/// <returns></returns>
		protected override ObjectLock GetOwnerLock()
		{
			return LockManager.GetObjectLock(_objectId);
		}

		/// <summary>
		/// Gets the item lock.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns></returns>
		protected override DataLock GetItemLock(LogicalPageId key)
		{
			return LockManager.GetDataLock(_objectId, key);
		}
		#endregion
	}
}
