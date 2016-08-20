namespace Zen.Trunk.Storage.Log
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;
	using System.Threading.Tasks.Schedulers;
	using Zen.Trunk.Storage.Data;
	using Zen.Trunk.Storage.IO;

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
	public class LogPageDevice : MountableDevice
	{
		#region Private Fields
		private FileStream _deviceStream;
		private Dictionary<uint, VirtualLogFileStream> _fileStreams =
			new Dictionary<uint, VirtualLogFileStream>();
		private LogRootPage _rootPage;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="LogPageDevice"/> class.
		/// </summary>
		public LogPageDevice(ushort deviceId, string pathName)
		{
			DeviceId = deviceId;
			PathName = pathName;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LogPageDevice"/> class.
		/// </summary>
		/// <param name="parentServiceProvider">The parent service provider.</param>
		public LogPageDevice(ushort deviceId, string pathName, IServiceProvider parentServiceProvider)
			: base(parentServiceProvider)
		{
		}
		#endregion

		#region Public Properties
		public ushort DeviceId
		{
			get;
			private set;
		}

		public string PathName
		{
			get;
			protected set;
		}

		public bool IsReadOnly
		{
			get
			{
				DatabaseDevice parent = (DatabaseDevice)GetService(
					typeof(DatabaseDevice));
				//return parent.IsReadOnly;
				return false;
			}
		}

		public virtual bool IsInRecovery
		{
			get
			{
				MasterLogPageDevice master = GetService<MasterLogPageDevice>();
				return master.IsInRecovery;
			}
		}
		#endregion

		#region Protected Properties
		#endregion

		#region Public Methods
		#endregion

		#region Internal Methods
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
		internal VirtualLogFileInfo InitVirtualFileForDevice(
			MasterLogRootPage masterRootPage)
		{
			// Retrieve last known log file info
			VirtualLogFileInfo fileInfo = GetVirtualFileById(
				masterRootPage.LogLastFileId);

			// Chain file table for new device onto last file
			return InitVirtualFileForDevice(masterRootPage, fileInfo);
		}

		/// <summary>
		/// Initialises the virtual file table for a log device.
		/// </summary>
		/// <param name="devInfo"></param>
		/// <param name="lastFileInfo"></param>
		/// <returns></returns>
		internal VirtualLogFileInfo InitVirtualFileForDevice(
			MasterLogRootPage masterRootPage,
			VirtualLogFileInfo lastFileInfo)
		{
			LogRootPage rootPage = GetRootPage<LogRootPage>();

			uint pageCount = rootPage.AllocatedPages;
			uint filePageCount = Math.Max(1, pageCount / 4);
			uint fileLength = filePageCount * rootPage.PageSize;

			for (byte fileIndex = 0; pageCount > 0; ++fileIndex)
			{
				// Determine last file Id and create virtual log file information
				uint lastFileId = 0;
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
				VirtualLogFileInfo info = rootPage.AddLogFile(DeviceId, fileLength, lastFileId);

				// Update next/prev fileId pointers
				if (lastFileInfo != null)
				{
					// Fixup forward connection when switching devices
					if (lastFileInfo.DeviceId != info.DeviceId)
					{
						lastFileInfo.CurrentHeader.NextFileId = info.FileId;
					}
				}
				else
				{
					// Check whether the root information is valid
					if (masterRootPage.LogStartFileId == 0)
					{
						// Setup log start file Id and offset
						masterRootPage.LogStartFileId = info.FileId;
						masterRootPage.LogStartOffset = 0;

						// Setup log end file Id and offset
						masterRootPage.LogEndFileId = info.FileId;
						masterRootPage.LogEndOffset = 0;

						masterRootPage.SetDirty();
					}
				}

				// Update cache of last known Id and object
				lastFileInfo = info;
				masterRootPage.LogLastFileId = info.FileId;
			}

			rootPage.SetDirty();

			// Return last file info setup
			return lastFileInfo;
		}

		internal virtual VirtualLogFileInfo GetVirtualFileById(uint fileId)
		{
			LogFileId file = new LogFileId(fileId);
			if (file.DeviceId != DeviceId)
			{
				throw new ArgumentException();
			}

			LogRootPage rootPage = GetRootPage<LogRootPage>();
			return rootPage.GetLogFile(file.Index);
		}

		internal VirtualLogFileStream GetVirtualFileStream(VirtualLogFileInfo info)
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

		internal T GetRootPage<T>()
			where T : LogRootPage
		{
			if (_rootPage == null)
			{
				_rootPage = CreateRootPage();
			}
			return (T)_rootPage;
		}

		internal protected void SaveRootPage()
		{
			_rootPage.Save();
		}
		#endregion

		#region Protected Methods
		protected override object GetService(Type serviceType)
		{
			if (serviceType == typeof(LogPageDevice))
			{
				return this;
			}

			// Delegate everything else
			return base.GetService(serviceType);
		}

		protected override Task OnOpen()
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

		protected override Task OnClose()
		{
			// Flush the device stream and close
			// NOTE: Assume virtual log file streams have been flushed
			if (_deviceStream != null)
			{
				_deviceStream.Flush();
				_deviceStream.Close();
				_deviceStream = null;
			}

			return base.OnClose();
		}

		protected virtual LogRootPage CreateRootPage()
		{
			return new LogRootPage();
		}
		#endregion

		#region Private Methods
		#endregion
	}
}