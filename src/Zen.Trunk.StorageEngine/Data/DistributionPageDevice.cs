using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using Serilog.Context;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Utils;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.PageDevice" />
    public abstract class DistributionPageDevice : PageDevice
    {
        #region Private Fields
        private static readonly ILogger Logger = Serilog.Log.ForContext<DistributionPageDevice>();

        private FileGroupDevice _fileGroupDevice;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DistributionPageDevice"/> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        protected DistributionPageDevice(DeviceId deviceId)
        {
            DeviceId = deviceId;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the device identifier.
        /// </summary>
        /// <value>
        /// The device identifier.
        /// </value>
        public DeviceId DeviceId { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is primary.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is primary; otherwise, <c>false</c>.
        /// </value>
        public bool IsPrimary => DeviceId == DeviceId.Primary;

        /// <summary>
        /// Gets the file group device.
        /// </summary>
        /// <value>
        /// The file group device.
        /// </value>
        public FileGroupDevice FileGroupDevice
        {
            get
            {
                if (_fileGroupDevice == null)
                {
                    _fileGroupDevice = GetService<FileGroupDevice>();
                }
                return _fileGroupDevice;
            }
        }

        /// <summary>
        /// Gets the distribution page offset.
        /// </summary>
        /// <value>
        /// The distribution page offset.
        /// </value>
        public abstract uint DistributionPageOffset { get; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Loads the device root page.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{RootPage}"/> task that when completed will yield
        /// the root page object.
        /// </returns>
        public async Task<RootPage> LoadRootPageAsync()
        {
            var rootPage = CreateRootPageAndHookupSite();
            await FileGroupDevice
                .LoadDataPageAsync(new LoadDataPageParameters(rootPage, true))
                .ConfigureAwait(false);
            return rootPage;
        }

        /// <summary>
        /// Create the device root page.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This method is typically only called during the file-group device
        /// open call for newly created devices.
        /// </remarks>
        public async Task<RootPage> InitRootPageAsync()
        {
            var rootPage = CreateRootPageAndHookupSite();
            await FileGroupDevice
                .InitDataPageAsync(new InitDataPageParameters(rootPage))
                .ConfigureAwait(false);
            return rootPage;
        }

        /// <summary>
        /// Allocates the data page.
        /// </summary>
        /// <param name="allocParams">The alloc parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation that
        ///  when complete will return the virtual page identifier of the newly
        ///  allocated page.
        /// </returns>
        /// <exception cref="DeviceFullException"></exception>
        public async Task<VirtualPageId> AllocateDataPageAsync(AllocateDataPageParameters allocParams)
        {
            // Keep looping until we allocate
            var isExpand = false;
            while (true)
            {
                // Load device root page
                using (var rootPage = await LoadRootPageAsync().ConfigureAwait(false))
                {
                    await rootPage.SetRootLockAsync(FileGroupRootLockType.Shared).ConfigureAwait(false);

                    // On this device, loop through all distribution pages
                    var maxDistPage =
                        (
                        (rootPage.AllocatedPages - DistributionPageOffset) /
                        DistributionPage.PageTrackingCount
                        ) + 1;
                    for (uint distPageIndex = 0; distPageIndex < maxDistPage; ++distPageIndex)
                    {
                        // Walk the distribution pages on this device
                        using (var distPage = new DistributionPage())
                        {
                            try
                            {
                                // Load the distribution page
                                // Deal with lock timeout by skipping this page
                                // NOTE: This may throw lock exception if some
                                //	other connection is currently updating an
                                //	extent etc...
                                await LoadDistributionPage(distPage, distPageIndex).ConfigureAwait(false);

                                // Ask distribution page to allocate for object
                                var virtualId = await distPage.AllocatePageAsync(allocParams).ConfigureAwait(false);
                                if (virtualId.Value > 0)
                                {
                                    return virtualId;
                                }
                            }
                            catch (Exception)
                            {
                                // TODO: We should be catching explicit 
                                //	exception types here rather than ignoring
                                //	all... CODE SMELL!
                            }
                        }
                    }

                    // If we have already tried to expand the device or the
                    //	device cannot be automatically expanded then throw
                    if (isExpand || !rootPage.IsExpandable)
                    {
                        throw new DeviceFullException(DeviceId);
                    }
                }

                // Expand this device and attempt to retry operation
                // NOTE: We first release the root page to be sure the
                //	expand will succeed (although since it would be using
                //	the same transaction id it would have the same lock)
                await FileGroupDevice
                    .ExpandDataDeviceAsync(new ExpandDataDeviceParameters(DeviceId, 0))
                    .ConfigureAwait(false);

                // Signal we have expanded the device
                isExpand = true;
            }
        }

        /// <summary>
        /// Deallocates the data page.
        /// </summary>
        /// <param name="deallocParams">The dealloc parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task DeallocateDataPageAsync(DeallocateDataPageParameters deallocParams)
        {
            // Locate the distribution page that is tracking this page
            var distPageIndex =
                (deallocParams.VirtualPageId.PhysicalPageId - DistributionPageOffset) /
                DistributionPage.PageTrackingCount;
            using (var distPage = new DistributionPage())
            {
                // Load the distribution page (it will be under a SHARED lock)
                await LoadDistributionPage(distPage, distPageIndex).ConfigureAwait(false);

                // Determine the page index (relative to distribution page that tracks it)
                var pageOffset = deallocParams.VirtualPageId.PhysicalPageId - distPage.VirtualPageId.PhysicalPageId - 1;

                // Delegate deallocation request to the distribution page
                await distPage.DeallocatePageAsync(pageOffset).ConfigureAwait(false);
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Called when opening the device.
        /// </summary>
        /// <returns></returns>
        protected override async Task OnOpenAsync()
        {
            if (IsCreate)
            {
                using (var rootPage = (PrimaryFileGroupRootPage)
                    await InitRootPageAsync().ConfigureAwait(false))
                {
                    // Get the device size information from the device status msg
                    var bufferDevice = GetService<IMultipleBufferDevice>();
                    var deviceInfo = bufferDevice.GetDeviceInfo(DeviceId);
                    rootPage.AllocatedPages = deviceInfo.PageCount;
                    var pageCount = rootPage.AllocatedPages;
                    Logger.Debug(
                        "Preparing to create distribution pages to cover device {DeviceId} of {PageCount} pages",
                        DeviceId,
                        pageCount);
                    
                    var subTasks = new List<Task>();

                    // Calculate number of distribution pages to deal with
                    var strideLength = DistributionPage.PageTrackingCount + 1;
                    var distPageCount = ((pageCount - DistributionPageOffset) / strideLength) + 1;

                    var pages = new List<DistributionPage>();
                    try
                    {
                        for (uint distPageIndex = 0; distPageIndex < distPageCount; ++distPageIndex)
                        {
                            // Create distribution page and setup virtual id
                            var page = new DistributionPage();
                            // Ensure page has exclusive lock during init
                            await page.SetDistributionLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false);
                            subTasks.Add(InitDistributionPage(page, distPageIndex, pageCount));
                            pages.Add(page);
                        }

                        // Wait for all pages to init or load and import
                        Logger.Debug(
                            "Waiting for distribution page creation to complete for device {DeviceId}...",
                            DeviceId);

                        await TaskExtra
                            .WhenAllOrEmpty(subTasks.ToArray())
                            .ConfigureAwait(false);
                    }
                    finally
                    {
                        // Cleanup allocated pages
                        foreach (var page in pages)
                        {
                            page.Dispose();
                        }
                    }
                }
            }
            else
            {
                using (var rootPage = (PrimaryFileGroupRootPage)
                    await LoadRootPageAsync().ConfigureAwait(false))
                {
                    var pageCount = rootPage.AllocatedPages;
                    Logger.Debug(
                        "Preparing to load distribution pages to cover device {DeviceId} of {PageCount} pages",
                        DeviceId,
                        pageCount);
                    
                    var subTasks = new List<Task>();

                    // Calculate number of distribution pages to deal with
                    var strideLength = DistributionPage.PageTrackingCount + 1;
                    var distPageCount = ((pageCount - DistributionPageOffset) / strideLength) + 1;

                    var pages = new List<DistributionPage>();
                    try
                    {
                        for (uint distPageIndex = 0; distPageIndex < distPageCount; ++distPageIndex)
                        {
                            // Create distribution page and setup virtual id
                            var page = new DistributionPage();
                            // Load distribution pages from the underlying device
                            subTasks.Add(LoadDistributionPageAndImport(distPageIndex, page));
                            pages.Add(page);
                        }

                        // Wait for all pages to init or load and import
                        Logger.Debug(
                            "Waiting for distribution page loading to complete for device {DeviceId}...",
                            DeviceId);

                        await TaskExtra
                            .WhenAllOrEmpty(subTasks.ToArray())
                            .ConfigureAwait(false);
                    }
                    finally
                    {
                        // Cleanup allocated pages
                        foreach (var page in pages)
                        {
                            page.Dispose();
                        }
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        private RootPage CreateRootPageAndHookupSite()
        {
            var rootPage = FileGroupDevice.CreateRootPage();
            rootPage.VirtualPageId = new VirtualPageId(DeviceId, 0);
            HookupPageSite(rootPage);
            return rootPage;
        }

        private async Task InitDistributionPage(DistributionPage page, uint distributionPageIndex, uint devicePageCount)
        {
            using (LogContext.PushProperty("Method", nameof(InitDistributionPage)))
            {
                try
                {
                    // Create contained init request
                    HookupPageSite(page);

                    // Determine the virtual id for the page
                    var physicalId = ((distributionPageIndex * (DistributionPage.PageTrackingCount + 1)) +
                                      DistributionPageOffset);

                    // TODO: Sanity check physical page fits the confines of the underlying device

                    // Setup the page virtual id
                    var pageId = new VirtualPageId(DeviceId, physicalId);
                    page.VirtualPageId = pageId;
                    Logger.Debug(
                        "Distribution page at {PageId}",
                        pageId);
                    
                    // Issue the sub-ordinate request
                    await FileGroupDevice.InitDataPageAsync(
                        new InitDataPageParameters(page)).ConfigureAwait(false);

                    // Notify page as to the number of usable extents
                    await page.UpdateValidExtentsAsync(devicePageCount).ConfigureAwait(false);
                    page.Save();
                }
                catch (Exception error)
                {
                    Logger.Error(
                        error,
                        "Init distribution page failed {Message}",
                        error.Message);
                    throw;
                }
            }
        }

        private async Task LoadDistributionPage(DistributionPage page, uint distributionPageIndex)
        {
            using (LogContext.PushProperty("Method", nameof(LoadDistributionPage)))
            {
                try
                {
                    // Create contained load request
                    HookupPageSite(page);

                    // Determine the virtual id for the page
                    var physicalId = ((distributionPageIndex * (DistributionPage.PageTrackingCount + 1)) + DistributionPageOffset);
                    var pageId = new VirtualPageId(DeviceId, physicalId);
                    page.VirtualPageId = pageId;
                    Logger.Debug(
                        "Distribution page at {PageId}",
                        pageId);

                    // Issue the sub-ordinate request
                    await FileGroupDevice
                        .LoadDataPageAsync(new LoadDataPageParameters(page))
                        .ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    Logger.Error(
                        error,
                        "Load distribution page failed {Message}",
                        error.Message);
                    throw;
                }
            }
        }

        private async Task LoadDistributionPageAndImport(uint distPageIndex, DistributionPage page)
        {
            await LoadDistributionPage(page, distPageIndex).ConfigureAwait(false);
            await FileGroupDevice.ProcessDistributionPageAsync(page).ConfigureAwait(false);
        }
        #endregion
    }
}
