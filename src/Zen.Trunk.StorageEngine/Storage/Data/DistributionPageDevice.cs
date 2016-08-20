namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Zen.Trunk.Storage.Locking;

	public abstract class DistributionPageDevice : PageDevice
	{
		#region Private Fields
		private FileGroupDevice _fileGroupDevice;
		private ushort _deviceId;
		private bool _firstCallForRootPage = true;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DistributionPageDevice"/> class.
		/// </summary>
		/// <param name="fileGroupDevice">The file group device.</param>
		/// <param name="deviceId">The device id.</param>
		protected DistributionPageDevice(FileGroupDevice fileGroupDevice, ushort deviceId)
			: base(fileGroupDevice)
		{
			_fileGroupDevice = fileGroupDevice;
			_deviceId = deviceId;
		}
		#endregion

		#region Public Properties
		public ushort DeviceId
		{
			get
			{
				return _deviceId;
			}
		}

		public bool IsPrimary
		{
			get
			{
				return DeviceId == 1;
			}
		}

		public FileGroupDevice FileGroupDevice
		{
			get
			{
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
		/// <param name="fileGroupDeviceState">State of the file group device.</param>
		/// <returns></returns>
		public virtual async Task<RootPage> LoadOrCreateRootPage()
		{
			Tracer.WriteVerboseLine("Load/create root page");

			// Load or initialise the file-group root page
			RootPage rootPage = FileGroupDevice.CreateRootPage(IsPrimary);
			rootPage.VirtualId = new DevicePageId(DeviceId, 0).VirtualPageId;
			HookupPageSite(rootPage);
			if (IsCreate && _firstCallForRootPage)
			{
				Tracer.WriteVerboseLine("Init root page pending");
				await FileGroupDevice.InitDataPage(
					new InitDataPageParameters(rootPage, false, true)).ConfigureAwait(false);
				Tracer.WriteVerboseLine("Init root page completed");
			}
			else
			{
				Tracer.WriteVerboseLine("Load root page pending");
				await FileGroupDevice.LoadDataPage(
					new LoadDataPageParameters(rootPage, true, IsPrimary, true)).ConfigureAwait(false);
				Tracer.WriteVerboseLine("Load root page completed");
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
		public async Task<DevicePageId> AllocateDataPage(AllocateDataPageParameters allocParams)
		{
			// Keep looping until we allocate
			bool isExpand = false;
			while (true)
			{
				// Load device root page
				using (RootPage rootPage = await LoadOrCreateRootPage())
				{
					rootPage.RootLock = RootLockType.Shared;

					// On this device loop through all distribution pages
					uint maxDistPage =
						(
						(rootPage.AllocatedPages - DistributionPageOffset) /
						DistributionPage.PageTrackingCount
						) + 1;
					for (uint distPageIndex = 0; distPageIndex < maxDistPage; ++distPageIndex)
					{
						// Walk the distribution pages on this device
						using (DistributionPage distPage = new DistributionPage())
						{
							try
							{
								// Load the distribution page
								// Deal with lock timeout by skipping this page
								// NOTE: This may throw lock exception if some
								//	other connection is currently updating an
								//	extent etc...
								distPage.DistributionLock = ObjectLockType.Shared;
								await LoadDistributionPage(distPage, distPageIndex).ConfigureAwait(false);

								// Ask distribution page to allocate for object
								ulong virtualId = distPage.AllocatePage(allocParams);
								if (virtualId > 0)
								{
									return new DevicePageId(virtualId);
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
				await FileGroupDevice.ExpandDataDevice(0, DeviceId).ConfigureAwait(false);

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
			using (PrimaryFileGroupRootPage rootPage = (PrimaryFileGroupRootPage)
				await LoadOrCreateRootPage().ConfigureAwait(false))
			{
				if (IsCreate)
				{
					// Get the device size information from the device status msg
					var bufferDevice = GetService<IMultipleBufferDevice>();
					var deviceInfo = bufferDevice.GetDeviceInfo(_deviceId);
					rootPage.AllocatedPages = deviceInfo.PageCount;
				}

				var pageCount = rootPage.AllocatedPages;
				Tracer.WriteVerboseLine("Preparing to process distribution pages to cover device {0} of {1} pages", _deviceId, pageCount);

				List<Task> subTasks = new List<Task>();

				// Calculate number of distribution pages to deal with
				uint strideLength = DistributionPage.PageTrackingCount + 1;
				uint distPageCount = ((pageCount - DistributionPageOffset) / strideLength) + 1;

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
							page.DistributionLock = ObjectLockType.Exclusive;
							subTasks.Add(InitDistributionPage(page, distPageIndex, pageCount));
						}
						pages.Add(page);
					}

					// Wait for all pages to init or load and import
					Tracer.WriteVerboseLine("Waiting for distribution page processing to complete for device {0}...", _deviceId);
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

				Tracer.WriteVerboseLine("Open completed for device {0}", _deviceId);
			}
		}

		/// <summary>
		/// Gets the service object of the specified type.
		/// </summary>
		/// <param name="serviceType">
		/// An object that specifies the type of service object to get.
		/// </param>
		/// <returns>
		/// A service object of type <paramref name="serviceType"/>.
		/// -or-
		/// null if there is no service object of type <paramref name="serviceType"/>.
		/// </returns>
		protected override object GetService(Type serviceType)
		{
			if (serviceType == typeof(DistributionPageDevice))
			{
				return this;
			}
			return base.GetService(serviceType);
		}
		#endregion

		#region Private Methods
		private async Task InitDistributionPage(DistributionPage page, uint distributionPageIndex, uint devicePageCount)
		{
			try
			{
				Tracer.WriteVerboseLine("Init distribution page");

				// Create contained init request
				HookupPageSite(page);

				// Determine the virtual id for the page
				uint physicalId = ((distributionPageIndex * (DistributionPage.PageTrackingCount + 1)) + DistributionPageOffset);

				// TODO: Sanity check physical page fits the confines of the underlying device

				// Setup the page virtual id
				DevicePageId pageId = new DevicePageId(DeviceId, physicalId);
				page.VirtualId = pageId.VirtualPageId;
				Tracer.WriteVerboseLine("\tPage id {0}", pageId);

				// Issue the sub-ordinate request
				await _fileGroupDevice.InitDataPage(
					new InitDataPageParameters(page)).ConfigureAwait(false);

				// Notify page as to the number of usable extents
				page.InitialiseValidExtents(devicePageCount);
				page.Save();

				// Mark original request as complete
				Tracer.WriteVerboseLine("Init distribution page complete");
			}
			catch (Exception error)
			{
				Tracer.WriteVerboseLine("Init distribution page failed {0}", error);
				throw;
			}
		}

		private async Task LoadDistributionPage(DistributionPage page, uint distributionPageIndex)
		{
			try
			{
				Tracer.WriteVerboseLine("Load distribution page");

				// Create contained load request
				HookupPageSite(page);

				// Determine the virtual id for the page
				uint physicalId = ((distributionPageIndex * (DistributionPage.PageTrackingCount + 1)) + DistributionPageOffset);
				DevicePageId pageId = new DevicePageId(DeviceId, physicalId);
				page.VirtualId = pageId.VirtualPageId;
				Tracer.WriteVerboseLine("\tPage id {0}", pageId);

				// Issue the sub-ordinate request
				await _fileGroupDevice.LoadDataPage(
					new LoadDataPageParameters(page)).ConfigureAwait(false);

				// Mark original request as complete
				Tracer.WriteVerboseLine("Load distribution page complete");
			}
			catch (Exception error)
			{
				Tracer.WriteVerboseLine("Load distribution page failed {0}", error);
				throw;
			}
		}

		private async Task LoadDistributionPageAndImport(uint distPageIndex, DistributionPage page)
		{
			await LoadDistributionPage(page, distPageIndex).ConfigureAwait(false);
			await _fileGroupDevice.ImportDistributionPage(page).ConfigureAwait(false);
		} 
		#endregion
	}

	public class PrimaryDistributionPageDevice : DistributionPageDevice
	{
		public PrimaryDistributionPageDevice(FileGroupDevice provider, ushort deviceId)
			: base(provider, deviceId)
		{
		}

		public override uint DistributionPageOffset
		{
			get
			{
				return 1;
			}
		}
	}

	public class SecondaryDistributionPageDevice : DistributionPageDevice
	{
		public SecondaryDistributionPageDevice(FileGroupDevice provider, ushort deviceId)
			: base(provider, deviceId)
		{
			if (deviceId < 2)
			{
				throw new ArgumentException("deviceId");
			}
		}

		public override uint DistributionPageOffset
		{
			get
			{
				return 1;
			}
		}
	}
}
