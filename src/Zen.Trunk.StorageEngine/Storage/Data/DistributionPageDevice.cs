using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zen.Trunk.Logging;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data
{
	public abstract class DistributionPageDevice : PageDevice
	{
		#region Private Fields
        private static readonly ILog Logger = LogProvider.For<DistributionPageDevice>();

		private readonly DeviceId _deviceId;
		private FileGroupDevice _fileGroupDevice;
		private bool _firstCallForRootPage = true;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DistributionPageDevice"/> class.
		/// </summary>
		/// <param name="deviceId">The device id.</param>
		protected DistributionPageDevice(DeviceId deviceId)
		{
			_deviceId = deviceId;
		}
		#endregion

		#region Public Properties
		public DeviceId DeviceId => _deviceId;

	    public bool IsPrimary => DeviceId == DeviceId.Primary;

	    public FileGroupDevice FileGroupDevice
	    {
	        get
	        {
	            if (_fileGroupDevice == null)
	            {
	                _fileGroupDevice = ResolveDeviceService<FileGroupDevice>();
	            }
	            return _fileGroupDevice;
	        }   
	    }

	    public abstract uint DistributionPageOffset
		{
			get;
		}

		public string Name
		{
			get;
			set;
		}

		public string PathName
		{
			get;
			set;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Loads or create the device root page.
		/// </summary>
		/// <returns></returns>
		public virtual async Task<RootPage> LoadOrCreateRootPageAsync()
		{
            if (Logger.IsDebugEnabled())
            {
                Logger.Debug("Load/create root page");
            }

			// Load or initialise the file-group root page
			var rootPage = FileGroupDevice.CreateRootPage(IsPrimary);
			rootPage.VirtualId = new VirtualPageId(DeviceId, 0);
			HookupPageSite(rootPage);
			if (IsCreate && _firstCallForRootPage)
			{
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Init root page pending");
                }

                await FileGroupDevice
                    .InitDataPage(new InitDataPageParameters(rootPage, false, true))
                    .ConfigureAwait(false);

                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Init root page completed");
                }
			}
			else
			{
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Load root page pending");
                }

                await FileGroupDevice
                    .LoadDataPage(new LoadDataPageParameters(rootPage, true, IsPrimary, true))
                    .ConfigureAwait(false);

                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Load root page completed");
                }
			}

			_firstCallForRootPage = false;
			return rootPage;
		}

		/// <summary>
		/// Allocates the data page.
		/// </summary>
		/// <param name="allocParams">The alloc parameters.</param>
		/// <returns></returns>
		/// <exception cref="DeviceFullException"></exception>
		public async Task<VirtualPageId> AllocateDataPageAsync(AllocateDataPageParameters allocParams)
		{
			// Keep looping until we allocate
			var isExpand = false;
			while (true)
			{
				// Load device root page
				using (var rootPage = await LoadOrCreateRootPageAsync().ConfigureAwait(false))
				{
					await rootPage.SetRootLockAsync(RootLockType.Shared).ConfigureAwait(false);

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
								await distPage.SetDistributionLockAsync(ObjectLockType.Shared).ConfigureAwait(false);
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
				await FileGroupDevice.ExpandDataDevice(DeviceId, 0).ConfigureAwait(false);

				// Signal we have expanded the device
				isExpand = true;
			}
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Called when opening the device.
		/// </summary>
		/// <returns></returns>
		protected override async Task OnOpen()
		{
			using (var rootPage = (PrimaryFileGroupRootPage)
				await LoadOrCreateRootPageAsync().ConfigureAwait(false))
			{
				if (IsCreate)
				{
					// Get the device size information from the device status msg
					var bufferDevice = ResolveDeviceService<IMultipleBufferDevice>();
					var deviceInfo = bufferDevice.GetDeviceInfo(_deviceId);
					rootPage.AllocatedPages = deviceInfo.PageCount;
				}

				var pageCount = rootPage.AllocatedPages;
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug($"Preparing to process distribution pages to cover device {_deviceId} of {pageCount} pages");
                }

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
						if (!IsCreate)
						{
							// Load distribution pages from the underlying device
							subTasks.Add(LoadDistributionPageAndImport(distPageIndex, page));
						}
						else
						{
							// Ensure page has exclusive lock during init
							await page.SetDistributionLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false);
							subTasks.Add(InitDistributionPage(page, distPageIndex, pageCount));
						}
						pages.Add(page);
					}

                    // Wait for all pages to init or load and import
                    if (Logger.IsDebugEnabled())
                    {
                        Logger.Debug($"Waiting for distribution page processing to complete for device {_deviceId}...");
                    }
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

                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug($"Open completed for device {_deviceId}");
                }
			}
		}
		#endregion

		#region Private Methods
		private async Task InitDistributionPage(DistributionPage page, uint distributionPageIndex, uint devicePageCount)
		{
			try
			{
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Init distribution page");
                }

				// Create contained init request
				HookupPageSite(page);

				// Determine the virtual id for the page
				var physicalId = ((distributionPageIndex * (DistributionPage.PageTrackingCount + 1)) + DistributionPageOffset);

				// TODO: Sanity check physical page fits the confines of the underlying device

				// Setup the page virtual id
				var pageId = new VirtualPageId(DeviceId, physicalId);
				page.VirtualId = pageId;
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug($"Distribution page at {pageId}");
                }

				// Issue the sub-ordinate request
				await FileGroupDevice.InitDataPage(
					new InitDataPageParameters(page)).ConfigureAwait(false);

				// Notify page as to the number of usable extents
				await page.InitialiseValidExtentsAsync(devicePageCount).ConfigureAwait(false);
				page.Save();

                // Mark original request as complete
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Init distribution page complete");
                }
			}
			catch (Exception error)
			{
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug($"Init distribution page failed {error}");
                }
				throw;
			}
		}

		private async Task LoadDistributionPage(DistributionPage page, uint distributionPageIndex)
		{
			try
			{
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Load distribution page");
                }

				// Create contained load request
				HookupPageSite(page);

				// Determine the virtual id for the page
				var physicalId = ((distributionPageIndex * (DistributionPage.PageTrackingCount + 1)) + DistributionPageOffset);
				var pageId = new VirtualPageId(DeviceId, physicalId);
				page.VirtualId = pageId;
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug($"Distribution page at {pageId}");
                }

				// Issue the sub-ordinate request
				await FileGroupDevice
                    .LoadDataPage(new LoadDataPageParameters(page))
                    .ConfigureAwait(false);

                // Mark original request as complete
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Load distribution page complete");
                }
			}
			catch (Exception error)
			{
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug($"Load distribution page failed {error}");
                }
				throw;
			}
		}

		private async Task LoadDistributionPageAndImport(uint distPageIndex, DistributionPage page)
		{
			await LoadDistributionPage(page, distPageIndex).ConfigureAwait(false);
			await FileGroupDevice.ImportDistributionPage(page).ConfigureAwait(false);
		} 
		#endregion
	}
}
