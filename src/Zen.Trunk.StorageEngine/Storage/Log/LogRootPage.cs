namespace Zen.Trunk.Storage.Log
{
	using System;
	using System.Collections.Generic;
	using Zen.Trunk.Storage.IO;

	public class LogRootPage : LogPage
	{
		#region Public Constants
		public const byte StatusIsExpandable = 1;
		public const byte StatusIsExpandablePercent = 2;
		#endregion

		#region Private Fields
		private readonly BufferFieldBitVector8 _status;
		private readonly BufferFieldUInt32 _allocatedPages;
		private readonly BufferFieldUInt32 _maximumPages;
		private readonly BufferFieldUInt32 _growthPages;
		private readonly BufferFieldDouble _growthPercent;

		private readonly BufferFieldUInt16 _logFileCount;
		private readonly List<VirtualLogFileInfo> _logFiles =
			new List<VirtualLogFileInfo>();
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="LogRootPage"/> class.
		/// </summary>
		public LogRootPage()
		{
			_status = new BufferFieldBitVector8(base.LastHeaderField);
			_allocatedPages = new BufferFieldUInt32(_status);
			_maximumPages = new BufferFieldUInt32(_allocatedPages);
			_growthPages = new BufferFieldUInt32(_maximumPages);
			_growthPercent = new BufferFieldDouble(_growthPages);
			_logFileCount = new BufferFieldUInt16(_growthPercent);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the status.
		/// </summary>
		/// <value>The status.</value>
		public byte Status
		{
			get
			{
				return _status.Value;
			}
			set
			{
				_status.Value = value;
			}
		}

		/// <summary>
		/// Gets or sets the allocated pages.
		/// </summary>
		/// <value>The allocated pages.</value>
		public uint AllocatedPages
		{
			get
			{
				return _allocatedPages.Value;
			}
			set
			{
				_allocatedPages.Value = value;
			}
		}

		/// <summary>
		/// Gets or sets the maximum pages.
		/// </summary>
		/// <value>The maximum pages.</value>
		public uint MaximumPages
		{
			get
			{
				return _maximumPages.Value;
			}
			set
			{
				_maximumPages.Value = value;
			}
		}

		/// <summary>
		/// Gets or sets the growth pages.
		/// </summary>
		/// <value>The growth pages.</value>
		public uint GrowthPages
		{
			get
			{
				return _growthPages.Value;
			}
			set
			{
				_growthPages.Value = value;
				if (value > 0)
				{
					_growthPercent.Value = 0.0;
					IsExpandable = true;
					IsExpandableByPercent = false;
				}
				else if (_growthPercent.Value == 0.0)
				{
					IsExpandable = IsExpandableByPercent = false;
				}
			}
		}

		/// <summary>
		/// Gets or sets the growth percent.
		/// </summary>
		/// <value>The growth percent.</value>
		public double GrowthPercent
		{
			get
			{
				return _growthPercent.Value;
			}
			set
			{
				_growthPercent.Value = value;
				if (value > 0.0)
				{
					_growthPages.Value = 0;
					IsExpandable = true;
					IsExpandableByPercent = true;
				}
				else if (_growthPages.Value == 0)
				{
					IsExpandable = IsExpandableByPercent = false;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is expandable.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is expandable; otherwise, <c>false</c>.
		/// </value>
		public bool IsExpandable
		{
			get
			{
				return _status.GetBit(StatusIsExpandable);
			}
			private set
			{
				_status.SetBit(StatusIsExpandable, value);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is expandable by percent.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is expandable by percent; otherwise, <c>false</c>.
		/// </value>
		public bool IsExpandableByPercent
		{
			get
			{
				return _status.GetBit(StatusIsExpandablePercent);
			}
			private set
			{
				_status.SetBit(StatusIsExpandablePercent, value);
			}
		}

		/// <summary>
		/// Overridden. Returns boolean true indicating this is a root page object.
		/// </summary>
		public override bool IsRootPage => true;

	    /// <summary>
		/// Overridden. Returns the minimum header size for this page object.
		/// </summary>
		public override uint MinHeaderSize => base.MinHeaderSize + 23;

	    /// <summary>
		/// Gets the number of virtual log files present on this device.
		/// </summary>
		/// <value>The log file count.</value>
		public ushort LogFileCount => (ushort)_logFiles.Count;

	    #endregion

		#region Protected Properties
		/// <summary>
		/// Gets the last header field.
		/// </summary>
		/// <value>The last header field.</value>
		protected override BufferField LastHeaderField => _logFileCount;

	    #endregion

		#region Public Methods
		public VirtualLogFileInfo AddLogFile(
			ushort deviceId, uint length, uint lastLogFile)
		{
			// Check whether we can fit another log file on this device.
			if (LogFileCount == ushort.MaxValue)
			{
				throw new DeviceFullException(deviceId);
			}

			// Create log file and assign Id
			var info = new VirtualLogFileInfo();
			info.DeviceId = deviceId;
			info.IndexId = LogFileCount;

			// Chain the log file if we can
			var lastLogFileId = new LogFileId(lastLogFile);
			if (lastLogFile != 0)
			{
				// If last log file is on a different device then the we can
				//	only perform half of the linked-list fixup.
				if (lastLogFileId.DeviceId != deviceId)
				{
					info.CurrentHeader.PrevFileId = lastLogFile;
				}

				// Otherwise check the last file hasn't already been chained
				else if (_logFiles[lastLogFileId.Index].CurrentHeader.NextFileId != 0)
				{
					throw new ArgumentException("LastFileId is invalid - already pointing to different FileId!");
				}

				// Last file can be chained.
				else
				{
					_logFiles[lastLogFileId.Index].CurrentHeader.NextFileId = info.FileId;
					info.CurrentHeader.PrevFileId = lastLogFile;
				}
			}

			// Handle default case of chaining to last file on device
			else if (LogFileCount > 0)
			{
				var lastIndexOnDevice = _logFiles.Count - 1;
				if (_logFiles[lastIndexOnDevice].CurrentHeader.NextFileId == 0)
				{
					_logFiles[lastIndexOnDevice].CurrentHeader.NextFileId = info.FileId;
					info.CurrentHeader.PrevFileId = _logFiles[lastIndexOnDevice].FileId;
				}
			}

			// Determine start offset
			if (_logFiles.Count == 0)
			{
				info.StartOffset = 0;
			}
			else
			{
				info.StartOffset =
					_logFiles[_logFiles.Count - 1].StartOffset +
					_logFiles[_logFiles.Count - 1].Length;
			}

			// Cache length
			info.Length = length;

			// Add file to list and return
			_logFiles.Add(info);
			return info;
		}

		public VirtualLogFileInfo GetLogFile(LogFileId id)
		{
			return GetLogFile(id.Index);
		}

		public VirtualLogFileInfo GetLogFile(ushort index)
		{
			// Check within bounds...
			if (index >= LogFileCount)
			{
				throw new ArgumentOutOfRangeException("index");
			}

			return _logFiles[(int)index];
		}
		#endregion

		#region Protected Methods
		protected override void OnPreSave(EventArgs e)
		{
			_logFileCount.Value = LogFileCount;
			base.OnPreSave(e);
		}

		protected override void ReadData(BufferReaderWriter streamManager)
		{
			base.ReadData(streamManager);
			if (_logFileCount.Value > 0)
			{
				for (byte index = 0; index < _logFileCount.Value; ++index)
				{
					var logFile = new VirtualLogFileInfo();
					logFile.Read(streamManager);
					_logFiles.Add(logFile);
				}
			}
		}

		protected override void WriteData(BufferReaderWriter streamManager)
		{
			base.WriteData(streamManager);
			for (var index = 0; index < _logFiles.Count; ++index)
			{
				_logFiles[index].Write(streamManager);
			}
		}
		#endregion
	}
}
