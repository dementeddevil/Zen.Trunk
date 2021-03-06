using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;
using Zen.Trunk.IO;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Logging
{
    /// <summary>
    /// The <b>LogPageDevice</b> is page device designed to contain physical
    /// physical buffer devices that are used in concert to provide the 
    /// transaction log page space.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The log page space is split into chunks that are represented by
    /// Virtual Log File Streams. Each stream stores complete log records
    /// together with redundant header information as part of the storage
    /// policy.
    /// </para>
    /// <para>
    /// All reads and writes to the underlying file-system streams is
    /// performed via asynchronous I/O.
    /// </para>
    /// <para>
    /// Calls to this device object are not multi-thread safe.
    /// </para>
    /// </remarks>
    public class LogPageDevice : MountableDevice, ILogPageDevice
    {
        #region Private Fields
        private const uint MinimumPagesPerVirtualFile = 128;
        private const uint MaximumPagesPerVirtualFile = 16384;

        private FileStream _deviceStream;
        private readonly Dictionary<LogFileId, VirtualLogFileStream> _fileStreams =
            new Dictionary<LogFileId, VirtualLogFileStream>();
        private LogRootPage _rootPage;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LogPageDevice"/> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="pathName">The log file pathname.</param>
        public LogPageDevice(DeviceId deviceId, string pathName)
        {
            DeviceId = deviceId;
            PathName = pathName;
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
        /// Gets or sets the name of the path.
        /// </summary>
        /// <value>
        /// The name of the path.
        /// </value>
        public string PathName { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets a value indicating whether this instance is in recovery.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is in recovery; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsInRecovery
        {
            get
            {
                var master = GetService<IMasterLogPageDevice>();
                return master.IsInRecovery;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialises the virtual file table for a newly added log device.
        /// </summary>
        /// <param name="masterRootPage">The master root page.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method will chain the new file table onto the current table
        /// by examining the logLastFileId and passing the related file to
        /// the Init routine.
        /// </remarks>
        public VirtualLogFileInfo InitVirtualFileForDevice(
            MasterLogRootPage masterRootPage)
        {
            // Retrieve last known log file info
            var fileInfo = GetVirtualFileById(masterRootPage.LastLogFileId);

            // Chain file table for new device onto last file
            return InitVirtualFileForDevice(masterRootPage, fileInfo);
        }

        /// <summary>
        /// Initialises the virtual file table for a log device.
        /// </summary>
        /// <param name="masterRootPage"></param>
        /// <param name="lastFileInfo"></param>
        /// <returns></returns>
        public VirtualLogFileInfo InitVirtualFileForDevice(
            MasterLogRootPage masterRootPage,
            VirtualLogFileInfo lastFileInfo)
        {
            var rootPage = GetRootPage<LogRootPage>();

            var pageCount = rootPage.AllocatedPages;
            var filePageCount = Math.Max(1, pageCount / 4);
            var fileLength = filePageCount * rootPage.PageSize;

            while (pageCount > 0)
            {
                // Determine last file Id and create virtual log file information
                var lastFileId = LogFileId.Zero;
                if (lastFileInfo != null)
                {
                    lastFileId = lastFileInfo.FileId;
                }

                // Determine file length if proposed size is larger than
                //	remaining pages. In all cases adjust pages remaining
                if (pageCount < filePageCount)
                {
                    fileLength = pageCount * rootPage.PageSize;
                    pageCount = 0;
                }
                else
                {
                    pageCount -= filePageCount;
                }

                // Create log file information for next block
                var info = rootPage.AddLogFile(DeviceId, fileLength, lastFileId);

                // Update next/prev fileId pointers
                if (lastFileInfo != null)
                {
                    // Fixup forward connection when switching devices
                    if (lastFileInfo.DeviceId != info.DeviceId)
                    {
                        lastFileInfo.CurrentHeader.NextLogFileId = info.FileId;
                    }
                }
                else
                {
                    // Check whether the root information is valid
                    if (masterRootPage.StartLogFileId == LogFileId.Zero)
                    {
                        // Setup log start file Id and offset
                        masterRootPage.StartLogFileId = info.FileId;
                        masterRootPage.StartLogOffset = 0;

                        // Setup log end file Id and offset
                        masterRootPage.EndLogFileId = info.FileId;
                        masterRootPage.EndLogOffset = 0;

                        masterRootPage.SetDirty();
                    }
                }

                // Update cache of last known Id and object
                lastFileInfo = info;
                masterRootPage.LastLogFileId = info.FileId;
            }

            rootPage.SetDirty();

            // Return last file info setup
            return lastFileInfo;
        }

        /// <summary>
        /// Gets the virtual file by identifier.
        /// </summary>
        /// <param name="fileId">The file identifier.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Device identifier of this device does not match identifier in file id.
        /// </exception>
        public virtual VirtualLogFileInfo GetVirtualFileById(LogFileId fileId)
        {
            if (fileId.DeviceId != DeviceId)
            {
                throw new ArgumentException("Device identifier mismatch", nameof(fileId));
            }

            var rootPage = GetRootPage<LogRootPage>();
            return rootPage.GetLogFile(fileId);
        }

        /// <summary>
        /// Gets the virtual file stream.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns></returns>
        public VirtualLogFileStream GetVirtualFileStream(VirtualLogFileInfo info)
        {
            if (!_fileStreams.TryGetValue(info.FileId, out var stream))
            {
                // Create virtual log file stream on top of backing store
                stream = new VirtualLogFileStream(
                    this, new NonClosingStream(_deviceStream), info);

                // Add file stream to the cache
                _fileStreams.Add(info.FileId, stream);
            }
            return stream;
        }

        public IEnumerable<VirtualLogFileInfo> ExpandDeviceCore(
            MasterLogRootPage masterRootPage, uint growthPageCount)
        {
            var rootPage = GetRootPage<LogRootPage>();

            // Limit number of pages by any defined maximum
            if (rootPage.MaximumPages > 0)
            {
                var maximumGrowthPages = rootPage.MaximumPages - rootPage.AllocatedPages;
                growthPageCount = Math.Min(maximumGrowthPages, growthPageCount);
            }

            // We need at least the minimum pages per virtual file
            growthPageCount = Math.Max(growthPageCount, MinimumPagesPerVirtualFile);

            // If we haven't changed the allocation size then return immediately
            var newAllocatedPageCount = rootPage.AllocatedPages + growthPageCount;
            if (newAllocatedPageCount <= rootPage.AllocatedPages)
            {
                return new VirtualLogFileInfo[0];
            }

            // Determine number of virtual log files we need to create
            var virtualFileCount = growthPageCount / MaximumPagesPerVirtualFile;
            if (growthPageCount % MaximumPagesPerVirtualFile != 0)
            {
                ++virtualFileCount;
            }

            // Setup virtual file info and chain into master root page
            var result = new List<VirtualLogFileInfo>();
            for (var fileIndex = 0; fileIndex < virtualFileCount; ++fileIndex)
            {
                var pageCount = Math.Min(growthPageCount, MaximumPagesPerVirtualFile);
                var info = rootPage.AddLogFile(
                    DeviceId, pageCount * rootPage.PageSize, masterRootPage.LastLogFileId);
                result.Add(info);
                growthPageCount -= pageCount;
                masterRootPage.LastLogFileId = info.FileId;
            }

            // Save the root page
            SaveRootPage();

            return result;
        }

        /// <summary>
        /// Gets the root page.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetRootPage<T>()
            where T : LogRootPage
        {
            if (_rootPage == null)
            {
                _rootPage = CreateRootPage();
            }
            return (T)_rootPage;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Called when opening the device.
        /// </summary>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected override Task OnOpenAsync()
        {
            const int streamBufferSize = StorageConstants.PageBufferSize * 4;

            if (_rootPage == null)
            {
                _rootPage = CreateRootPage();
            }

            if (IsCreate)
            {
                _deviceStream = new FileStream(
                    PathName,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    streamBufferSize,
                    true);
                _deviceStream.SetLength(streamBufferSize * _rootPage.AllocatedPages);
                _rootPage.PreInitInternal();
                _rootPage.BackingStore = _deviceStream;
                _rootPage.OnInitInternal();
                _rootPage.ReadOnly = false;

                // Save root page
                SaveRootPage();
            }
            else
            {
                _deviceStream = new FileStream(
                    PathName,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    streamBufferSize,
                    true);
                _rootPage.PreLoadInternal();
                _rootPage.BackingStore = _deviceStream;
                _rootPage.PostLoadInternal();
            }

            return CompletedTask.Default;
        }

        /// <summary>
        /// Called when closing the device.
        /// </summary>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected override Task OnCloseAsync()
        {
            // Flush the device stream and close
            // NOTE: Assume virtual log file streams have been flushed
            if (_deviceStream != null)
            {
                _deviceStream.Flush();
                _deviceStream.Close();
                _deviceStream = null;
            }

            return base.OnCloseAsync();
        }

        /// <summary>
        /// Creates the root page.
        /// </summary>
        /// <returns></returns>
        protected virtual LogRootPage CreateRootPage()
        {
            return new LogRootPage();
        }

        /// <summary>
        /// Saves the root page.
        /// </summary>
        protected void SaveRootPage()
        {
            _rootPage.Save();
        }

        protected uint CalculateGrowthPageCount()
        {
            var rootPage = GetRootPage<LogRootPage>();
            if (!rootPage.IsExpandable && !rootPage.IsExpandableByPercent ||
                rootPage.MaximumPages > 0 && rootPage.MaximumPages == rootPage.AllocatedPages)
            {
                return 0;
            }

            uint growthPageCount = 0;

            if (rootPage.IsExpandable)
            {
                growthPageCount = rootPage.GrowthPages;
            }
            else if (rootPage.IsExpandableByPercent && rootPage.GrowthPercent > 0.0)
            {
                var growthPages = (uint)(rootPage.AllocatedPages * rootPage.GrowthPercent / 100);
                growthPageCount = growthPages;
            }

            return growthPageCount;
        }
        #endregion
    }
}