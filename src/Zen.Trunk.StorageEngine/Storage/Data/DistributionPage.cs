namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;
	using Zen.Trunk.Storage;
	using Zen.Trunk.Storage.IO;
	using Zen.Trunk.Storage.Locking;

	/// <summary>
	/// Distribution pages track the allocation status of 512 pages across a
	/// total of 64 extents.
	/// </summary>
	/// <remarks>
	/// <para>
	///	For the last distribution page on a given device (or the first if there
	///	is only a single dist page) it is possible that the extents listed 
	///	might not all be usable as it is not a requirement for devices to be
	///	valid for all pages managed by a distribution page.
	/// </para>
	/// <para>
	///	The shrink/expand methods in a device must update the IsUsable flag for
	///	each extent as appropriate.
	/// </para>
	/// <para>
	/// For an extent to be marked as usable, all its associated pages must be
	/// usable.
	/// </para>
	/// </remarks>
	public class DistributionPage : DataPage
	{
		#region Internal Objects
		internal class ExtentInfo : BufferFieldWrapper
		{
			// 5 bytes
			private BufferFieldBitVector8 _status;
			private BufferFieldUInt32 _objectId;

			// 14 bytes * 8
			private PageInfo[] _pageState;

			public const uint ExtentInfoBytes = 5 + (14 * PagesPerExtent);

			internal ExtentInfo()
			{
				_status = new BufferFieldBitVector8();
				_objectId = new BufferFieldUInt32(_status);
				_pageState = new PageInfo[PagesPerExtent];
				for (int index = 0; index < PagesPerExtent; ++index)
				{
					_pageState[index] = new PageInfo();
				}
			}

			protected override BufferField FirstField
			{
				get
				{
					return _status;
				}
			}

			protected override BufferField LastField
			{
				get
				{
					return _objectId;
				}
			}

			internal bool IsFree
			{
				get
				{
					return (!IsMixedExtent) && (!IsFull) && (ObjectId == 0);
				}
			}

			internal bool IsMixedExtent
			{
				get
				{
					return _status.GetBit(0);
				}
				set
				{
					_status.SetBit(0, value);
				}
			}

			internal bool IsFull
			{
				get
				{
					return _status.GetBit(1);
				}
				set
				{
					_status.SetBit(1, value);
				}
			}

			internal bool IsUsable
			{
				get
				{
					return _status.GetBit(2);
				}
				set
				{
					_status.SetBit(2, value);
				}
			}

			internal uint ObjectId
			{
				get
				{
					return _objectId.Value;
				}
				set
				{
					_objectId.Value = value;
				}
			}

			internal PageInfo[] Pages
			{
				get
				{
					return _pageState;
				}
			}

			public void ReadFrom(PageBuffer buffer, uint headerSize, uint extentIndex)
			{
				using (Stream stream = buffer.GetBufferStream(
					(int)(headerSize + (extentIndex * ExtentInfoBytes)),
					(int)ExtentInfoBytes,
					false))
				{
					ReadFrom(stream);
				}
			}

			protected override void DoRead(BufferReaderWriter streamManager)
			{
				// Base class will read the extent information
				base.DoRead(streamManager);

				// We need to read the page info too
				for (int index = 0; index < PagesPerExtent; ++index)
				{
					_pageState[index].Read(streamManager);
				}
			}

			protected override void DoWrite(BufferReaderWriter streamManager)
			{
				// Base class will write the extent information
				base.DoWrite(streamManager);

				// We need to write the page info too
				for (int index = 0; index < PagesPerExtent; ++index)
				{
					_pageState[index].Write(streamManager);
				}
			}
		}

		internal class PageInfo : BufferFieldWrapper
		{
			private BufferFieldUInt64 _logicalId;
			private BufferFieldUInt32 _objectId;
			private BufferFieldByte _objectType;
			private BufferFieldByte _allocationStatus;

			public PageInfo()
			{
				_logicalId = new BufferFieldUInt64();
				_objectId = new BufferFieldUInt32(_logicalId);
				_objectType = new BufferFieldByte(_objectId);
				_allocationStatus = new BufferFieldByte(_objectType);
			}

			protected override BufferField FirstField
			{
				get
				{
					return _logicalId;
				}
			}

			protected override BufferField LastField
			{
				get
				{
					return _allocationStatus;
				}
			}

			/// <summary>
			/// Logical ID
			/// </summary>
			internal ulong LogicalId
			{
				get
				{
					return _logicalId.Value;
				}
				set
				{
					_logicalId.Value = value;
				}
			}

			/// <summary>
			/// Owner object ID
			/// </summary>
			internal uint ObjectId
			{
				get
				{
					return _objectId.Value;
				}
				set
				{
					_objectId.Value = value;
				}
			}

			/// <summary>
			/// Object Type
			/// 1 = Root
			/// 2 = Table
			/// 3 = Sample
			/// 4 = Index
			/// </summary>
			internal byte ObjectType
			{
				get
				{
					return _objectType.Value;
				}
				set
				{
					_objectType.Value = value;
				}
			}

			/// <summary>
			/// Allocation Status
			/// </summary>
			internal bool AllocationStatus
			{
				get
				{
					return _allocationStatus.Value != 0;
				}
				set
				{
					_allocationStatus.Value = value ? (byte)1 : (byte)0;
				}
			}
		}
		#endregion

		#region Public Fields
		/// <summary>
		/// Number of extents tracked by a distribution page.
		/// </summary>
		public const uint ExtentTrackingCount = 64;

		/// <summary>
		/// Number of pages tracked by a distribution page.
		/// </summary>
		public const uint PageTrackingCount = 512;

		/// <summary>
		/// Number of pages tracked by an extent.
		/// </summary>
		public const uint PagesPerExtent = PageTrackingCount / ExtentTrackingCount;
		#endregion

		#region Private Fields
		private ExtentInfo[] _extents;
		private List<uint> _lockedExtents;

		private ObjectLockType _distributionLock = ObjectLockType.IntentShared;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DistributionPage"/> class.
		/// </summary>
		public DistributionPage()
		{
			_extents = new ExtentInfo[ExtentTrackingCount];
			for (int index = 0; index < ExtentTrackingCount; ++index)
			{
				_extents[index] = new ExtentInfo();
			}
		}
		#endregion

		#region Public Properties
		public ObjectLockType DistributionLock
		{
			get
			{
				return _distributionLock;
			}
			set
			{
				if (_distributionLock != value)
				{
					ObjectLockType oldLock = _distributionLock;
					try
					{
						_distributionLock = value;
						LockPage();
					}
					catch (Exception e)
					{
						_distributionLock = oldLock;
						throw e;
					}
				}
			}
		}

		public override PageType PageType
		{
			get
			{
				return PageType.Distribution;
			}
		}
		#endregion

		#region Internal Properties
		internal DistributionLockOwnerBlock LockBlock
		{
			get
			{
				if (TrunkTransactionContext.Current == null)
				{
					throw new InvalidOperationException("No current transaction.");
				}

				// If we have no transaction locks then we should be in dispose
				var txnLocks = TransactionLocks;
				if (txnLocks == null)
				{
					return null;
				}

				// Return the lock-owner block for this object instance
				return txnLocks.GetOrCreateDistributionLockOwnerBlock(VirtualId);
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Allocates a page to an object in the distribution table.
		/// </summary>
		/// <param name="allocParams">The alloc parameters.</param>
		/// <returns>
		/// The virtual page ID of the allocated page or zero if no space or
		/// if failed to allocate due to lock-timeout issues.
		/// </returns>
		/// <remarks>
		/// This method will lock and unlock extents whilst searching
		/// using the default lock timeout for the page.
		/// </remarks>
		public ulong AllocatePage(AllocateDataPageParameters allocParams)
		{
			System.Diagnostics.Debug.WriteLine(
				string.Format("Allocate page via DistributionPage {0}",
				new DevicePageId(VirtualId)));
			IDatabaseLockManager lm = (IDatabaseLockManager)GetService(typeof(IDatabaseLockManager));

			// Ensure we have some kind of lock on the page...
			if (DistributionLock == ObjectLockType.None)
			{
				DistributionLock = ObjectLockType.IntentShared;
			}

			// Phase #1: Look for existing extent we can use for this object
			bool hasAcquiredLock = false, useExtent = false;
			uint extent = 0;
			for (uint index = 0; !useExtent && index < ExtentTrackingCount; ++index)
			{
				try
				{
					// Get the extent information
					ExtentInfo info = GetLatestExtentInfoWithLock(lm, index, out hasAcquiredLock);

					// If this extent is unusable then stop as we have reached
					//	the end of the device...
					if (!info.IsUsable)
					{
						break;
					}

					// Skip extents that are full
					if (info.IsFull)
					{
						continue;
					}

					// Determine whether this is an extent we can use
					if ((allocParams.MixedExtent && info.IsMixedExtent) ||
						(!allocParams.MixedExtent && !info.IsMixedExtent && info.ObjectId == allocParams.ObjectId))
					{
						extent = index;
						useExtent = true;
					}
				}
				catch (LockException)
				{
				}
				finally
				{
					// If we are not using this extent and we have a lock then
					//	release the extent lock now.
					if (!useExtent && hasAcquiredLock)
					{
						UnlockExtent(index);
					}
				}
			}

			// Phase #2: Look for a free extent we can use for this object
			if (!useExtent)
			{
				for (uint index = 0; !useExtent && index < ExtentTrackingCount; ++index)
				{
					try
					{
						// Get the extent information
						ExtentInfo info = GetLatestExtentInfoWithLock(lm, index, out hasAcquiredLock);

						// If this extent is unusable then stop as we have reached
						//	the end of the device...
						if (!info.IsUsable)
						{
							break;
						}

						// If extent is free then we will use it
						if (info.IsFree)
						{
							extent = index;
							useExtent = true;
						}
					}
					catch (LockException)
					{
					}
					finally
					{
						// If we are not using this extent and we have a lock then
						//	release the extent lock now.
						if (!useExtent && hasAcquiredLock)
						{
							UnlockExtent(index);
						}
					}
				}
			}

			// If we have a suitable extent
			ulong virtPageId = 0;
			if (useExtent)
			{
				// Escalate locks
				try
				{
					// Escalate the distribution lock if necessary
					if (DistributionLock != ObjectLockType.IntentExclusive &&
						DistributionLock != ObjectLockType.Exclusive)
					{
						DistributionLock = ObjectLockType.IntentExclusive;
					}

					// Escalate the extent lock if necessary
					if (DistributionLock != ObjectLockType.Exclusive)
					{
						DataLock extentLock = lm.GetExtentLock(VirtualId, extent);
						if (!extentLock.HasLock(DataLockType.Exclusive))
						{
							LockExtent(extent, DataLockType.Update);
							LockExtent(extent, DataLockType.Exclusive);
						}
					}
				}
				catch (Exception e)
				{
					// If we acquired the lock in this method then release
					//	before throwing...
					if (hasAcquiredLock)
					{
						UnlockExtent(extent);
					}
					throw e;
				}

				// Check extent is still available and usable
				// NOTE: We do not need to reload since we've had (at a minimum)
				//	a shared read lock since identifying the extent...
				ExtentInfo info = _extents[extent];
				if (info.IsFull || !info.IsUsable)
				{
					return 0;
				}

				// Setup the extent info
				if (info.IsFree)
				{
					_extents[extent].IsMixedExtent = allocParams.MixedExtent;
					if (!allocParams.MixedExtent)
					{
						_extents[extent].ObjectId = allocParams.ObjectId;
					}
				}

				// Find a free page in this extent
				for (uint index = 0; index < PagesPerExtent; ++index)
				{
					if (!info.Pages[index].AllocationStatus)
					{
						info.Pages[index].AllocationStatus = true;
						info.Pages[index].LogicalId = allocParams.LogicalId;
						info.Pages[index].ObjectId = allocParams.ObjectId;
						info.Pages[index].ObjectType = allocParams.ObjectType;

						// Determine the virtual id for this page
						uint pageIndex = (extent * PagesPerExtent) + index;
						virtPageId = VirtualId + pageIndex + 1;
						break;
					}
				}

				// Check whether the extent is now full
				bool extentFull = true;
				for (int index = 0; extentFull && index < PagesPerExtent; ++index)
				{
					if (!info.Pages[index].AllocationStatus)
					{
						extentFull = false;
						break;
					}
				}
				info.IsFull = extentFull;

				SetDirty();
				Save();
				//WriteData();
				//SetHeaderDirty();
			}
			return virtPageId;
		}

		public void FreePage(uint offset, TimeSpan timeout)
		{
			// Sanity checks
			if (offset >= PageTrackingCount)
			{
				throw new ArgumentOutOfRangeException("Offset outside page tracking range for this page.");
			}
			CheckReadOnly();

			// Determine extent and page index
			uint extent = offset / PagesPerExtent;
			uint pageIndex = offset % PagesPerExtent;
			if (_extents[extent].Pages[pageIndex].AllocationStatus)
			{
				IDatabaseLockManager lm = GetService<IDatabaseLockManager>();

				// We need extent lock before we can free page
				if (DistributionLock != ObjectLockType.IntentExclusive &&
					DistributionLock != ObjectLockType.Exclusive)
				{
					DistributionLock = ObjectLockType.IntentExclusive;
				}
				if (!_lockedExtents.Contains(extent))
				{
					System.Diagnostics.Debug.Assert(extent < ExtentTrackingCount);
					LockExtent(extent, DataLockType.Update);
					LockExtent(extent, DataLockType.Exclusive);
				}

				// Update page information
				_extents[extent].Pages[pageIndex].AllocationStatus = false;
				_extents[extent].Pages[pageIndex].ObjectId = 0;
				_extents[extent].Pages[pageIndex].LogicalId = 0;
				_extents[extent].Pages[pageIndex].ObjectType = 0;

				// Update extent information
				_extents[extent].IsFull = false;
				if (!_extents[extent].Pages.Any((pi) => pi.AllocationStatus))
				{
					_extents[extent].IsMixedExtent = false;
					_extents[extent].ObjectId = 0;
				}

				SetDirty();
				Save();
				//WriteData();
				//SetHeaderDirty();
			}
		}

		public Task Import(ILogicalVirtualManager logicalVirtualManager)
		{
			DevicePageId startPageId = new DevicePageId(VirtualId).NextPage;

			// Loop through next 512 device pages adding logical
			//	lookups where we have allocated pages.
			List<Task> addTasks = new List<Task>();

			for (uint extentIndex = 0; extentIndex < ExtentTrackingCount; ++extentIndex)
			{
				// Check extent is usable
				if (!_extents[extentIndex].IsUsable)
				{
					break;
				}

				// Only process allocated pages...
				for (uint pageIndex = 0; pageIndex < PagesPerExtent; ++pageIndex)
				{
					if (_extents[extentIndex].Pages[pageIndex].AllocationStatus)
					{
						uint pageOffset = extentIndex * PagesPerExtent + pageIndex;
						DevicePageId pageId = new DevicePageId(
							startPageId.DeviceId,
							startPageId.PhysicalPageId + pageOffset);

						addTasks.Add(logicalVirtualManager.AddLookupAsync(
							pageId, _extents[extentIndex].Pages[pageIndex].LogicalId));
					}
				}
			}

			// Return task that will complete when dist page has been processed
			return TaskExtra.WhenAllOrEmpty(addTasks.ToArray());
		}

		/// <summary>
		/// Initialises the valid extents for this distribution page
		/// </summary>
		/// <param name="devicePageCapacity">
		/// The number of pages allocated to the underlying device.
		/// </param>
		/// <remarks>
		/// This method is called during the handling of InitDistributionPage
		/// in the distribution page device.
		/// </remarks>
		public void InitialiseValidExtents(uint devicePageCapacity)
		{
			// TODO: We need to test this code
			//	It does not appear to be writing to the underlying buffer...

			// Ensure we have an exclusive lock on this page
			if (DistributionLock != ObjectLockType.Exclusive)
			{
				DistributionLock = ObjectLockType.Exclusive;
			}

			// Determine the physical page index for this page
			DevicePageId pageId = new DevicePageId(VirtualId);
			uint pageIndex = pageId.PhysicalPageId;

			// Determine the number of extents that are usable
			uint usableExtents = ExtentTrackingCount;
			if ((pageIndex + PageTrackingCount) >= devicePageCapacity)
			{
				// This distribution page covers more than the device has pages
				// NOTE: We do not consider partially usable extents
				uint validPages = devicePageCapacity - pageIndex - 1;
				usableExtents = validPages / PagesPerExtent;
			}

			// Set extent usability state
			for (int index = 0; index < ExtentTrackingCount; ++index)
			{
				if (index < usableExtents)
				{
					_extents[index].IsUsable = true;
					_extents[index].IsMixedExtent = false;
					_extents[index].IsFull = false;
					_extents[index].ObjectId = 0;
				}
				else
				{
					_extents[index].IsUsable = false;
				}
			}

			SetDirty();
			Save();
			//WriteData();
			//SetHeaderDirty();
		}
		#endregion

		#region Protected Methods
		protected override void PreUpdateTimestamp()
		{
			// Acquire distribution page spin lock
			IDatabaseLockManager lm = (IDatabaseLockManager)GetService(typeof(IDatabaseLockManager));
			if (lm != null)
			{
				lm.LockDistributionHeader(
					DataBuffer.PageId.VirtualPageId,
					TimeSpan.FromMilliseconds(40));

				// Reload header
				// This means we effectively have phantom reads possible on
				//	this header region but ensures we always have a unique
				//	timestamp. Be aware of this when processing other 
				//	page information!
				ReadHeader();
			}

			base.PreUpdateTimestamp();
		}

		protected override void PostUpdateTimestamp()
		{
			IDatabaseLockManager lm = (IDatabaseLockManager)GetService(typeof(IDatabaseLockManager));
			if (lm != null)
			{
				// Force write of header information to DataBuffer
				WriteHeader();

				// Unlock distribution page header
				lm.UnlockDistributionHeader(DataBuffer.PageId.VirtualPageId);
			}

			base.PostUpdateTimestamp();
		}

		protected override void WriteData(BufferReaderWriter streamManager)
		{
			// Save locked extents and page information unless this
			//	is a new page
			bool hasExclusiveLock = false;
			if (DistributionLock == ObjectLockType.Exclusive)
			{
				hasExclusiveLock = true;
			}
			for (uint index = 0; index < ExtentTrackingCount; ++index)
			{
				if (_lockedExtents != null && !hasExclusiveLock)
				{
					streamManager.IsWritable = _lockedExtents.Contains(index);
				}
				else
				{
					streamManager.IsWritable = true;
				}
				_extents[index].Write(streamManager);
			}
		}

		protected override void ReadData(BufferReaderWriter streamManager)
		{
			for (int index = 0; index < ExtentTrackingCount; ++index)
			{
				_extents[index].Read(streamManager);
			}
		}

		/// <summary>
		/// Performs operations on this instance prior to being initialised.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// <remarks>
		/// Overrides to this method must set their desired lock prior to
		/// calling the base class.
		/// The base class method will enable the locking primitives and call
		/// LockPage.
		/// This mechanism ensures that all lock states have been set prior to
		/// the first call to LockPage.
		/// </remarks>
		protected override void OnPreInit(EventArgs e)
		{
			// We need an exclusive lock
			DistributionLock = ObjectLockType.Exclusive;
			base.OnPreInit(e);
		}

		protected override void OnInit(EventArgs e)
		{
			// Reset allocation maps
			for (int index = 0; index < ExtentTrackingCount; ++index)
			{
				_extents[index].ObjectId = 0;
				foreach (PageInfo page in _extents[index].Pages)
				{
					page.AllocationStatus = false;
					page.ObjectType = 0;
					page.ObjectId = 0;
					page.LogicalId = 0;
				}
			}
			base.OnInit(e);
		}

		protected override void OnPreLoad(EventArgs e)
		{
			// We need a shared read lock if nothing specified
			if (DistributionLock == ObjectLockType.None)
			{
				DistributionLock = ObjectLockType.Shared;
			}
			base.OnPreLoad(e);
		}

		/// <summary>
		/// Called to apply suitable locks to this page.
		/// </summary>
		/// <param name="lm">A reference to the <see cref="IDatabaseLockManager"/>.</param>
		protected override void OnLockPage(IDatabaseLockManager lm)
		{
			base.OnLockPage(lm);
			try
			{
				// Lock owner via lock owner block
				LockBlock.LockOwner(DistributionLock, LockTimeout);
			}
			catch
			{
				base.OnUnlockPage(lm);
				throw;
			}
		}

		/// <summary>
		/// Overridden. Called to remove locks applied to this page in a prior
		/// call to <see cref="M:DatabasePage.OnLockPage"/>.
		/// </summary>
		/// <param name="lm">A reference to the <see cref="IDatabaseLockManager"/>.</param>
		protected override void OnUnlockPage(IDatabaseLockManager lm)
		{
			try
			{
				if (_lockedExtents != null)
				{
					// Release all distribution page locks
					foreach (uint extent in _lockedExtents)
					{
						lm.UnlockDistributionExtent(VirtualId, extent);
					}
					_lockedExtents.Clear();
					_lockedExtents = null;
				}

				// Release distribution page lock
				DistributionLockOwnerBlock lob = LockBlock;
				if (lob != null)
				{
					LockBlock.UnlockOwner();
				}
			}
			finally
			{
				base.OnUnlockPage(lm);
			}
		}
		#endregion

		#region Private Methods
		private ExtentInfo GetLatestExtentInfoWithLock(
			IDatabaseLockManager lm, uint extentIndex, out bool hasAcquiredLock)
		{
			hasAcquiredLock = false;
			bool alreadyHasLock = false;

			// Check whether we already have this extent locked
			//	in case we are allocating more than once in the same txn
			if (_lockedExtents != null && _lockedExtents.Contains(extentIndex))
			{
				alreadyHasLock = true;
			}

			// Check whether the active transaction has an extent lock
			//	in this case we must not lock otherwise we will 
			//	downgrade the extent lock already held... nasty
			if (!alreadyHasLock)
			{
				// NOTE: We only need to check for exclusive lock
				DataLock extentLock = lm.GetExtentLock(VirtualId, extentIndex);
				alreadyHasLock = extentLock.HasLock(DataLockType.Exclusive);
			}

			// Gain extent lock
			if (!alreadyHasLock)
			{
				LockExtent(extentIndex, DataLockType.Shared);
				hasAcquiredLock = true;
			}

			// Re-read extent information (it may have changed)
			ExtentInfo info = _extents[extentIndex];

			// Do not re-read extent if we already held the lock
			//	as we will overwrite previous changes unless lock was acquired
			//	from a different page object...
			if (!alreadyHasLock ||
				_lockedExtents == null ||
				!_lockedExtents.Contains(extentIndex))
			{
				info.ReadFrom(DataBuffer, HeaderSize, extentIndex);
			}
			return info;
		}

		private void CheckPageId(uint pageId)
		{
			if (pageId >= PageTrackingCount)
			{
				throw new ArgumentOutOfRangeException(
					"pageId", pageId,
					"Page ID out of range (0-" +
					(PageTrackingCount - 1).ToString() + ").");
			}
		}

		private void LockExtent(uint extentIndex, DataLockType lockType)
		{
			LockBlock.LockItem(extentIndex, lockType, LockTimeout);
			if (_lockedExtents == null)
			{
				_lockedExtents = new List<uint>();
			}
			if (!_lockedExtents.Contains(extentIndex))
			{
				_lockedExtents.Add(extentIndex);
			}
		}

		private void UnlockExtent(uint extentIndex)
		{
			try
			{
				LockBlock.UnlockItem(extentIndex);
			}
			finally
			{
				if (_lockedExtents != null)
				{
					_lockedExtents.Remove(extentIndex);
					if (_lockedExtents.Count == 0)
					{
						_lockedExtents = null;
					}
				}
			}
		}
		#endregion
	}
}