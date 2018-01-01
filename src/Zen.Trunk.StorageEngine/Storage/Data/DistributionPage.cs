using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Utils;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// Distribution pages track the allocation status of 512 pages across a
    /// total of 64 extents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Given a page is 8192 bytes; distribution pages cover allocation for
    /// the following 512 * 8192 = 4,194,304 bytes after the distribution page.
    ///	For the last distribution page on a given device (or the first if there
    ///	is only a single distribution page) it's possible that the extents listed 
    ///	might not all be usable as it is not a requirement for devices to be
    ///	valid for all pages managed by a distribution page.
    /// </para>
    /// <para>
    ///	The shrink/expand methods in a device must update the IsUsable flag for
    ///	each extent as appropriate.
    /// </para>
    /// <para>
    /// For an extent to be marked as usable, all its associated pages must be
    /// usable. This means the minimum amount a device can be expanded is
    /// 8 * 8192 = 65,536 bytes (there will be an additional 8192 bytes if a
    /// further distribution page is needed.)
    /// </para>
    /// <para>
    /// A device can only be shrunk if the extents covering the range being
    /// removed are all marked as free (which implies that to fully shrink a
    /// device, some page rewriting may be required in order to free up space
    /// at the end of each associated device.)
    /// </para>
    /// </remarks>
    public class DistributionPage : DataPage
    {
        #region Internal Objects
        private class AllocExtentResult
        {
            public bool HasAcquiredLock { get; set; }

            public uint Extent { get; set; }

            public bool UseExtent { get; set; }
        }

        internal class ExtentInfo : BufferFieldWrapper
        {
            // 5 bytes
            private readonly BufferFieldBitVector8 _status;
            private readonly BufferFieldObjectId _objectId;

            // 14 bytes * 8
            private readonly PageInfo[] _pageState;

            public const uint ExtentInfoBytes = 5 + (14 * PagesPerExtent);

            internal ExtentInfo()
            {
                _status = new BufferFieldBitVector8();
                _objectId = new BufferFieldObjectId(_status);
                _pageState = new PageInfo[PagesPerExtent];
                for (var index = 0; index < PagesPerExtent; ++index)
                {
                    _pageState[index] = new PageInfo();
                }
            }

            protected override BufferField FirstField => _status;

            protected override BufferField LastField => _objectId;

            internal bool IsFree => (!IsMixedExtent) && (!IsFull) && (ObjectId.Value == 0);

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

            internal ObjectId ObjectId
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

            internal PageInfo[] Pages => _pageState;

            public void ReadFrom(PageBuffer buffer, uint headerSize, uint extentIndex)
            {
                using (var stream = buffer.GetBufferStream(
                    (int)(headerSize + (extentIndex * ExtentInfoBytes)),
                    (int)ExtentInfoBytes,
                    false))
                {
                    ReadFrom(stream);
                }
            }

            protected override void OnRead(SwitchingBinaryReader reader)
            {
                // Base class will read the extent information
                base.OnRead(reader);

                // We need to read the page info too
                for (var index = 0; index < PagesPerExtent; ++index)
                {
                    _pageState[index].Read(reader);
                }
            }

            protected override void OnWrite(SwitchingBinaryWriter writer)
            {
                // Base class will write the extent information
                base.OnWrite(writer);

                // We need to write the page info too
                for (var index = 0; index < PagesPerExtent; ++index)
                {
                    _pageState[index].Write(writer);
                }
            }
        }

        internal class PageInfo : BufferFieldWrapper
        {
            private readonly BufferFieldLogicalPageId _logicalId;
            private readonly BufferFieldObjectId _objectId;
            private readonly BufferFieldObjectType _objectType;
            private readonly BufferFieldByte _allocationStatus;

            public PageInfo()
            {
                _logicalId = new BufferFieldLogicalPageId();
                _objectId = new BufferFieldObjectId(_logicalId);
                _objectType = new BufferFieldObjectType(_objectId);
                _allocationStatus = new BufferFieldByte(_objectType);
            }

            protected override BufferField FirstField => _logicalId;

            protected override BufferField LastField => _allocationStatus;

            /// <summary>
            /// Logical ID
            /// </summary>
            internal LogicalPageId LogicalPageId
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
            internal ObjectId ObjectId
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
            internal ObjectType ObjectType
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
            /// Gets or sets a value indicating whether this instance is allocated.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is allocated; otherwise, <c>false</c>.
            /// </value>
            internal bool IsAllocated
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
        private readonly ExtentInfo[] _extents;
        private List<uint> _lockedExtents;
        private IDatabaseLockManager _lockManager;
        private ObjectLockType _distributionLock = ObjectLockType.IntentShared;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DistributionPage"/> class.
        /// </summary>
        public DistributionPage()
        {
            _extents = new ExtentInfo[ExtentTrackingCount];
            for (var index = 0; index < ExtentTrackingCount; ++index)
            {
                _extents[index] = new ExtentInfo();
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the distribution lock.
        /// </summary>
        /// <value>
        /// The distribution lock.
        /// </value>
        public ObjectLockType DistributionLock => _distributionLock;

        /// <summary>
        /// Gets/sets the page type.
        /// </summary>
        public override PageType PageType => PageType.Distribution;
        #endregion

        #region Private Properties
        private IDatabaseLockManager LockManager => _lockManager ?? (_lockManager = GetService<IDatabaseLockManager>());

        private DistributionLockOwnerBlock LockBlock
        {
            get
            {
                if (TrunkTransactionContext.Current == null)
                {
                    throw new InvalidOperationException("No current transaction.");
                }

                // Return the lock-owner block for this object instance
                var txnLocks = TrunkTransactionContext.GetTransactionLockOwnerBlock(LockManager);
                return txnLocks?.GetOrCreateDistributionLockOwnerBlock(VirtualPageId);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Attempts to apply the specified distribution lock.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public async Task SetDistributionLockAsync(ObjectLockType value)
        {
            if (_distributionLock != value)
            {
                var oldLock = _distributionLock;
                try
                {
                    _distributionLock = value;
                    await LockPageAsync().ConfigureAwait(false);
                }
                catch
                {
                    _distributionLock = oldLock;
                    throw;
                }
            }
        }

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
        public async Task<VirtualPageId> AllocatePageAsync(AllocateDataPageParameters allocParams)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Allocate page via DistributionPage {VirtualPageId}");

            // Ensure we have some kind of lock on the page...
            if (DistributionLock == ObjectLockType.None)
            {
                await SetDistributionLockAsync(ObjectLockType.IntentShared).ConfigureAwait(false);
            }

            // Look for extent we can use;
            //   Phase #1: Look for existing extent we can use for this object
            //   Phase #2: Look for a free extent we can use for this object
            var result = await TryFindUsableExistingExtentAsync(allocParams).ConfigureAwait(false);
            if (!result.UseExtent)
            {
                result = await TryFindUsableFreeExtentAsync().ConfigureAwait(false);
            }

            // If we don't have a suitable extent
            if (!result.UseExtent)
            {
                return new VirtualPageId(0);
            }

            // Escalate locks
            try
            {
                // Escalate the distribution lock if necessary
                if (DistributionLock != ObjectLockType.IntentExclusive &&
                    DistributionLock != ObjectLockType.Exclusive)
                {
                    await SetDistributionLockAsync(ObjectLockType.IntentExclusive).ConfigureAwait(false);
                }

                // Escalate the extent lock if necessary
                if (DistributionLock != ObjectLockType.Exclusive)
                {
                    var extentLock = LockManager.GetDistributionExtentLock(VirtualPageId, result.Extent);
                    try
                    {
                        if (!await extentLock.HasLockAsync(DataLockType.Exclusive).ConfigureAwait(false))
                        {
                            await LockExtentAsync(result.Extent, DataLockType.Update).ConfigureAwait(false);
                            await LockExtentAsync(result.Extent, DataLockType.Exclusive).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        extentLock.ReleaseRefLock();
                    }
                }
            }
            catch
            {
                // If we acquired the lock in this method then release
                //	before throwing...
                if (result.HasAcquiredLock)
                {
                    await UnlockExtentAsync(result.Extent).ConfigureAwait(false);
                }
                throw;
            }

            // Check extent is still available and usable
            // NOTE: We do not need to reload since we've had (at a minimum)
            //	a shared read lock since identifying the extent...
            var info = _extents[result.Extent];
            if (info.IsFull || !info.IsUsable)
            {
                return new VirtualPageId(0);
            }

            // Setup the extent info
            if (info.IsFree)
            {
                _extents[result.Extent].IsMixedExtent = allocParams.MixedExtent;
                if (!allocParams.MixedExtent)
                {
                    _extents[result.Extent].ObjectId = allocParams.ObjectId;
                }
            }

            // Find a free page in this extent
            var virtPageId = new VirtualPageId(0);
            for (uint index = 0; index < PagesPerExtent; ++index)
            {
                if (!info.Pages[index].IsAllocated)
                {
                    info.Pages[index].IsAllocated = true;
                    info.Pages[index].LogicalPageId = allocParams.LogicalPageId;
                    info.Pages[index].ObjectId = allocParams.ObjectId;
                    info.Pages[index].ObjectType = allocParams.ObjectType;

                    // Determine the virtual id for this page
                    var pageIndex = (result.Extent * PagesPerExtent) + index;
                    virtPageId = VirtualPageId.Offset((int)(pageIndex + 1));
                    break;
                }
            }

            // Update extent full state
            info.IsFull = info.Pages.All(p => p.IsAllocated);

            // Mark this instance as dirty and force save to underlying page buffer
            SetDirty();
            Save();
            //WriteData();
            //SetHeaderDirty();

            return virtPageId;
        }

        /// <summary>
        /// Deallocates the page.
        /// </summary>
        /// <param name="pageIndex">The zero-based page index.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Offset outside page tracking range.</exception>
        public async Task DeallocatePageAsync(uint pageIndex)
        {
            // Sanity checks
            CheckPageId(pageIndex);
            CheckReadOnly();

            // Determine extent and page index
            var extentIndex = pageIndex / PagesPerExtent;
            var pageIndexInExtent = pageIndex % PagesPerExtent;
            if (_extents[extentIndex].Pages[pageIndexInExtent].IsAllocated)
            {
                // We need extent lock before we can free page
                if (DistributionLock != ObjectLockType.IntentExclusive &&
                    DistributionLock != ObjectLockType.Exclusive)
                {
                    await SetDistributionLockAsync(ObjectLockType.IntentExclusive).ConfigureAwait(false);
                }
                if (!_lockedExtents.Contains(extentIndex))
                {
                    System.Diagnostics.Debug.Assert(extentIndex < ExtentTrackingCount);
                    await LockExtentAsync(extentIndex, DataLockType.Update).ConfigureAwait(false);
                    await LockExtentAsync(extentIndex, DataLockType.Exclusive).ConfigureAwait(false);
                }

                // Update page information
                _extents[extentIndex].Pages[pageIndexInExtent].IsAllocated = false;
                _extents[extentIndex].Pages[pageIndexInExtent].ObjectId = ObjectId.Zero;
                _extents[extentIndex].Pages[pageIndexInExtent].LogicalPageId = LogicalPageId.Zero;
                _extents[extentIndex].Pages[pageIndexInExtent].ObjectType = ObjectType.Unknown;

                // Update extent information
                _extents[extentIndex].IsFull = false;
                if (!_extents[extentIndex].Pages.Any(pi => pi.IsAllocated))
                {
                    _extents[extentIndex].IsMixedExtent = false;
                    _extents[extentIndex].ObjectId = ObjectId.Zero;
                }

                // Perform full save of distribution page
                SetDirty();
                Save();
            }
        }

        /// <summary>
        /// Exports page mapping information to the specified logical virtual manager.
        /// </summary>
        /// <param name="logicalVirtualManager">The logical virtual manager.</param>
        /// <returns></returns>
        public Task ExportPageMappingTo(ILogicalVirtualManager logicalVirtualManager)
        {
            var startPageId = VirtualPageId.NextPage;

            // Loop through next 512 device pages adding logical
            //	lookups where we have allocated pages.
            var addTasks = new List<Task>();

            // Walk list of usable extents
            for (uint extentIndex = 0; extentIndex < ExtentTrackingCount && _extents[extentIndex].IsUsable; ++extentIndex)
            {
                // Walk list of pages in the extent
                for (uint pageIndex = 0; pageIndex < PagesPerExtent; ++pageIndex)
                {
                    // Only concern ourselves with allocated pages
                    if (_extents[extentIndex].Pages[pageIndex].IsAllocated)
                    {
                        // Determine the page offset
                        var pageOffset = extentIndex * PagesPerExtent + pageIndex;

                        // Determine the virtual page identifier for the corresponding page
                        var virtualPageId = new VirtualPageId(
                            startPageId.DeviceId,
                            startPageId.PhysicalPageId + pageOffset);

                        // Pull out the logical page identifier from the page information
                        var logicalPageId = _extents[extentIndex].Pages[pageIndex].LogicalPageId;

                        // Add mapping to the logical/virtual manager
                        addTasks.Add(logicalVirtualManager.AddLookupAsync(virtualPageId, logicalPageId));
                    }
                }
            }

            // Return task that will complete when dist page has been processed
            return TaskExtra.WhenAllOrEmpty(addTasks.ToArray());
        }

        /// <summary>
        /// Updates the valid extents for this distribution page given the page
        /// capacity of the underlying device
        /// </summary>
        /// <param name="devicePageCapacity">
        /// The number of pages allocated to the underlying device.
        /// </param>
        /// <remarks>
        /// This method is called as part of initialisation, expansion and 
        /// shrinking of data devices and for a given page is safe to call
        /// multiple times.
        /// </remarks>
        public async Task UpdateValidExtentsAsync(uint devicePageCapacity)
        {
            // TODO: We need to test this code
            //	It does not appear to be writing to the underlying buffer...

            // Ensure we have an exclusive lock on this page
            if (DistributionLock != ObjectLockType.Exclusive)
            {
                await SetDistributionLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false);
            }

            // Determine the physical page index for this page
            var pageIndex = VirtualPageId.PhysicalPageId;

            // Determine the number of extents that are usable
            var usableExtents = ExtentTrackingCount;
            if ((pageIndex + PageTrackingCount) >= devicePageCapacity)
            {
                // This distribution page covers more than the device has pages
                // NOTE: We do not consider partially usable extents
                var validPages = devicePageCapacity - pageIndex - 1;
                usableExtents = validPages / PagesPerExtent;
            }

            // Set extent usability state
            for (var index = 0; index < ExtentTrackingCount; ++index)
            {
                var extentInfo = _extents[index];
                if (index < usableExtents && !extentInfo.IsUsable)
                {
                    extentInfo.IsUsable = true;
                    extentInfo.IsMixedExtent = false;
                    extentInfo.IsFull = false;
                    extentInfo.ObjectId = ObjectId.Zero;
                }
                else
                {
                    extentInfo.IsUsable = false;
                }
            }

            // Force full save of this distribution page
            SetDirty();
            Save();
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Called by the Storage Engine prior to updating the page timestamp.
        /// </summary>
        protected override void PreUpdateTimestamp()
        {
            // Acquire distribution page spin lock
            var lockManager = GetService<IDatabaseLockManager>();
            if (lockManager != null)
            {
                lockManager.LockDistributionHeader(
                    DataBuffer.PageId,
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

        /// <summary>
        /// Called by the Storage Engine after the timestamp has been updated.
        /// </summary>
        /// <remarks>
        /// By default this method marks the header as dirty.
        /// </remarks>
        protected override void PostUpdateTimestamp()
        {
            var lockManager = GetService<IDatabaseLockManager>();
            if (lockManager != null)
            {
                // Force write of header information to DataBuffer
                WriteHeader();

                // Unlock distribution page header
                lockManager.UnlockDistributionHeader(DataBuffer.PageId);
            }

            base.PostUpdateTimestamp();
        }

        /// <summary>
        /// Writes the page data block to the specified buffer writer.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void WriteData(SwitchingBinaryWriter streamManager)
        {
            // Save locked extents and page information unless this
            //	is a new page
            bool hasExclusiveLock = DistributionLock == ObjectLockType.Exclusive;
            for (uint index = 0; index < ExtentTrackingCount; ++index)
            {
                if (_lockedExtents != null && !hasExclusiveLock)
                {
                    streamManager.WriteToUnderlyingStream = _lockedExtents.Contains(index);
                }
                else
                {
                    streamManager.WriteToUnderlyingStream = true;
                }
                _extents[index].Write(streamManager);
            }
        }

        /// <summary>
        /// Reads the page data block from the specified buffer reader.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void ReadData(SwitchingBinaryReader streamManager)
        {
            for (var index = 0; index < ExtentTrackingCount; ++index)
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
        protected override async Task OnPreInitAsync(EventArgs e)
        {
            // We need an exclusive lock
            await SetDistributionLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false);
            await base.OnPreInitAsync(e).ConfigureAwait(false);
        }

        /// <summary>
        /// Raises the <see cref="E:Init" /> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        /// <returns></returns>
        protected override Task OnInitAsync(EventArgs e)
        {
            // Reset allocation maps
            for (var index = 0; index < ExtentTrackingCount; ++index)
            {
                _extents[index].ObjectId = ObjectId.Zero;
                foreach (var page in _extents[index].Pages)
                {
                    page.IsAllocated = false;
                    page.ObjectType = ObjectType.Unknown;
                    page.ObjectId = ObjectId.Zero;
                    page.LogicalPageId = LogicalPageId.Zero;
                }
            }
            return base.OnInitAsync(e);
        }

        /// <summary>
        /// Overridden. Called by the system prior to loading the page
        /// from persistent storage.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        /// <remarks>
        /// Overrides to this method must set their desired lock prior to
        /// calling the base class.
        /// The base class method will enable the locking primitives and call
        /// <see cref="DataPage.LockPageAsync" /> as necessary.
        /// This mechanism ensures that all lock states have been set prior to
        /// the first call to LockPage.
        /// When the current isolation level is uncommitted read then 
        /// <see cref="DataPage.LockPageAsync" /> will not be called.
        /// When the current isolation level is repeatable read or serializable
        /// then the <see cref="DataPage.HoldLock" /> will be set to <c>true</c>
        /// prior to calling <see cref="DataPage.LockPageAsync" />.
        /// </remarks>
        protected override async Task OnPreLoadAsync(EventArgs e)
        {
            // We need a shared read lock if nothing specified
            if (DistributionLock == ObjectLockType.None)
            {
                await SetDistributionLockAsync(ObjectLockType.Shared).ConfigureAwait(false);
            }
            await base.OnPreLoadAsync(e).ConfigureAwait(false);
        }

        /// <summary>
        /// Called to apply suitable locks to this page.
        /// </summary>
        /// <param name="lockManager">A reference to the <see cref="IDatabaseLockManager"/>.</param>
        protected override async Task OnLockPageAsync(IDatabaseLockManager lockManager)
        {
            await base.OnLockPageAsync(lockManager).ConfigureAwait(false);
            try
            {
                // Lock owner via lock owner block
                await LockBlock.LockOwnerAsync(DistributionLock, LockTimeout).ConfigureAwait(false);
            }
            catch
            {
                await base.OnUnlockPageAsync(lockManager).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Overridden. Called to remove locks applied to this page in a prior
        /// call to <see cref="M:DatabasePage.OnLockPage"/>.
        /// </summary>
        /// <param name="lockManager">A reference to the <see cref="IDatabaseLockManager"/>.</param>
        protected override async Task OnUnlockPageAsync(IDatabaseLockManager lockManager)
        {
            try
            {
                if (_lockedExtents != null)
                {
                    // Release all distribution page locks
                    foreach (var extent in _lockedExtents)
                    {
                        await lockManager.UnlockDistributionExtentAsync(VirtualPageId, extent).ConfigureAwait(false);
                    }
                    _lockedExtents.Clear();
                    _lockedExtents = null;
                }

                // Release distribution page lock
                var lob = LockBlock;
                if (lob != null)
                {
                    await LockBlock.UnlockOwnerAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await base.OnUnlockPageAsync(lockManager).ConfigureAwait(false);
            }
        }
        #endregion

        #region Private Methods
        private async Task<AllocExtentResult> TryFindUsableExistingExtentAsync(AllocateDataPageParameters allocParams)
        {
            var result = new AllocExtentResult();
            for (uint index = 0; !result.UseExtent && index < ExtentTrackingCount; ++index)
            {
                try
                {
                    // Get the extent information
                    var lockInfo = await GetLatestExtentInfoWithLockAsync(index).ConfigureAwait(false);
                    var info = lockInfo.Item1;
                    result.HasAcquiredLock = lockInfo.Item2;

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
                        result.Extent = index;
                        result.UseExtent = true;
                    }
                }
                catch (LockException)
                {
                }
                finally
                {
                    // If we are not using this extent and we have a lock then
                    //	release the extent lock now.
                    if (!result.UseExtent && result.HasAcquiredLock)
                    {
                        await UnlockExtentAsync(index).ConfigureAwait(false);
                    }
                }
            }

            return result;
        }

        private async Task<AllocExtentResult> TryFindUsableFreeExtentAsync()
        {
            var result = new AllocExtentResult();
            for (uint index = 0; !result.UseExtent && index < ExtentTrackingCount; ++index)
            {
                try
                {
                    // Get the extent information
                    var lockInfo = await GetLatestExtentInfoWithLockAsync(index).ConfigureAwait(false);
                    var info = lockInfo.Item1;
                    result.HasAcquiredLock = lockInfo.Item2;

                    // If this extent is unusable then stop as we have reached
                    //	the end of the device...
                    if (!info.IsUsable)
                    {
                        break;
                    }

                    // If extent is free then we will use it
                    if (info.IsFree)
                    {
                        result.Extent = index;
                        result.UseExtent = true;
                    }
                }
                catch (LockException)
                {
                }
                finally
                {
                    // If we are not using this extent and we have a lock then
                    //	release the extent lock now.
                    if (!result.UseExtent && result.HasAcquiredLock)
                    {
                        await UnlockExtentAsync(index).ConfigureAwait(false);
                    }
                }
            }
            return result;
        }

        private async Task<Tuple<ExtentInfo, bool>> GetLatestExtentInfoWithLockAsync(uint extentIndex)
        {
            var hasAcquiredLock = false;

            // Check whether we already have this extent locked
            //	in case we are allocating more than once in the same txn
            var alreadyHasLock = _lockedExtents != null && _lockedExtents.Contains(extentIndex);

            // Check whether the active transaction has an extent lock
            //	in this case we must not lock otherwise we will 
            //	downgrade the extent lock already held... nasty
            if (!alreadyHasLock)
            {
                // NOTE: We only need to check for exclusive lock
                var extentLock = LockManager.GetDistributionExtentLock(VirtualPageId, extentIndex);
                try
                {
                    alreadyHasLock = await extentLock.HasLockAsync(DataLockType.Exclusive).ConfigureAwait(false);
                }
                finally
                {
                    extentLock.ReleaseRefLock();
                }
            }

            // Gain extent lock
            if (!alreadyHasLock)
            {
                await LockExtentAsync(extentIndex, DataLockType.Shared).ConfigureAwait(false);
                hasAcquiredLock = true;
            }

            // Pull extent information from the extent information block
            var info = _extents[extentIndex];

            // Re-read extent information as it may have been changed by 
            //  another transaction/session but do not do so if we already
            //  held the lock as this would overwrite previous changes unless
            //  lock was acquired from a different page object...
            if (!alreadyHasLock ||
                _lockedExtents == null ||
                !_lockedExtents.Contains(extentIndex))
            {
                info.ReadFrom(DataBuffer, HeaderSize, extentIndex);
            }
            return new Tuple<ExtentInfo, bool>(info, hasAcquiredLock);
        }

        private void CheckPageId(uint offset)
        {
            if (offset >= PageTrackingCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offset), offset,
                    $"Page ID out of range (0-{PageTrackingCount - 1}) inclusive");
            }
        }

        private async Task LockExtentAsync(uint extentIndex, DataLockType lockType)
        {
            await LockBlock
                .LockItemAsync(extentIndex, lockType, LockTimeout)
                .ConfigureAwait(false);

            if (_lockedExtents == null)
            {
                _lockedExtents = new List<uint>();
            }
            if (!_lockedExtents.Contains(extentIndex))
            {
                _lockedExtents.Add(extentIndex);
            }
        }

        private async Task UnlockExtentAsync(uint extentIndex)
        {
            try
            {
                await LockBlock
                    .UnlockItemAsync(extentIndex)
                    .ConfigureAwait(false);
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