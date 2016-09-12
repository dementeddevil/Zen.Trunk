using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;
using Zen.Trunk.Extensions;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Storage.Log
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
    /// </remarks>
    public class LogPageDevice : MountableDevice, ILogPageDevice
    {
        #region Private Fields
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
        public bool IsReadOnly
        {
            get
            {
                //var parent = ResolveDeviceService<DatabaseDevice>();
                //return parent.IsReadOnly;
                // ReSharper disable once ConvertPropertyToExpressionBody
                return false;
            }
        }

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
                var master = GetService<MasterLogPageDevice>();
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
        /// <exception cref="ArgumentException"></exception>
        public virtual VirtualLogFileInfo GetVirtualFileById(LogFileId fileId)
        {
            if (fileId.DeviceId != DeviceId)
            {
                throw new ArgumentException();
            }

            var rootPage = GetRootPage<LogRootPage>();
            return rootPage.GetLogFile(fileId.Index);
        }

        /// <summary>
        /// Gets the virtual file stream.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns></returns>
        public VirtualLogFileStream GetVirtualFileStream(VirtualLogFileInfo info)
        {
            VirtualLogFileStream stream;
            if (!_fileStreams.TryGetValue(info.FileId, out stream))
            {
                // Create virtual log file stream on top of backing store
                stream = new VirtualLogFileStream(
                    this, new NonClosingStream(_deviceStream), info);

                // Add file stream to the cache
                _fileStreams.Add(info.FileId, stream);
            }
            return stream;
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
                    8192,
                    true);
                _deviceStream.SetLength(8192 * _rootPage.AllocatedPages);
                _rootPage.BackingStore = _deviceStream;

                // TODO: Initialise root page information
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
                    8192,
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
        #endregion

        #region Private Methods
        #endregion
    }
}