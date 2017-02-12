// -----------------------------------------------------------------------
// <copyright file="DistributionLockOwnerBlock.cs" company="Zen Design Software">
// © Zen Design Software 2009 - 2016
// </copyright>
// -----------------------------------------------------------------------

using Zen.Trunk.VirtualMemory;

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
	/// <para>
	/// GetOwnerLock returns a distribution lock keyed on the virtual page id.
	/// GetItemLock returns an extent lock keyed on both the virtual page id and the extent index.
	/// </para>
	/// </remarks>
	internal class DistributionLockOwnerBlock : LockOwnerBlockBase<uint>
	{
		#region Private Fields
		private readonly VirtualPageId _virtualPageId;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DistributionLockOwnerBlock" /> class.
		/// </summary>
		/// <param name="manager">The manager.</param>
		/// <param name="virtualPageId">The virtual page unique identifier.</param>
		/// <param name="maxExtentLocks">The maximum extent locks.</param>
		public DistributionLockOwnerBlock(IDatabaseLockManager manager, VirtualPageId virtualPageId, uint maxExtentLocks = 10)
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
