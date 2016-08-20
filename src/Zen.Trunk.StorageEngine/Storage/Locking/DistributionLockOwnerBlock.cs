// -----------------------------------------------------------------------
// <copyright file="DistributionLockOwnerBlock.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.Storage.Locking
{
	/// <summary>
	/// <c>DistributionLockOwnerBlock</c> track the locks for a given
	/// distribution page on behalf of a transaction.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This object is required in order to implement lock escalation.
	/// </para>
	/// </remarks>
	internal class DistributionLockOwnerBlock : LockOwnerBlockBase<uint>
	{
		#region Private Fields
		private readonly ulong _virtualPageId;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DistributionLockOwnerBlock" /> class.
		/// </summary>
		/// <param name="manager">The manager.</param>
		/// <param name="virtualPageId">The virtual page unique identifier.</param>
		/// <param name="maxExtentLocks">The maximum extent locks.</param>
		public DistributionLockOwnerBlock(IDatabaseLockManager manager, ulong virtualPageId, uint maxExtentLocks = 10)
			: base(manager, maxExtentLocks)
		{
			_virtualPageId = virtualPageId;
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Gets the owner lock.
		/// </summary>
		/// <returns></returns>
		protected override ObjectLock GetOwnerLock()
		{
			return LockManager.GetDistributionLock(_virtualPageId);
		}

		/// <summary>
		/// Gets the item lock.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns></returns>
		protected override DataLock GetItemLock(uint key)
		{
			return LockManager.GetExtentLock(_virtualPageId, key);
		}
		#endregion
	}
}
