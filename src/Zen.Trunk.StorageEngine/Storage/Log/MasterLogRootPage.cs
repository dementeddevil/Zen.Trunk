// -----------------------------------------------------------------------
// <copyright file="MasterLogPage.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.Storage.Log
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using IO;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class MasterLogRootPage : LogRootPage
	{
		#region Private Fields
		private readonly Dictionary<DeviceId, DeviceInfo> _deviceById;
		private readonly List<DeviceInfo> _devicesByIndex;

		private readonly BufferFieldUInt16 _deviceCount;

		/// <summary>
		/// Last virtual log file Id created.
		/// </summary>
		/// <remarks>
		/// This value is used to complete linked-list chaining during
		/// the creation of new virtual log files.
		/// </remarks>
		private readonly BufferFieldUInt32 _logLastFileId;

		/// <summary>
		/// Tracks the start of the log file.
		/// </summary>
		/// <remarks>
		/// This is the first file which must be kept in order to successfully
		/// recover the database. It is adjusted during recovery and checkpoint
		/// operations.
		/// </remarks>
		private readonly BufferFieldUInt32 _logStartFileId;

		/// <summary>
		/// Tracks the offset within the start file where log records must be
		/// preserved.
		/// </summary>
		private readonly BufferFieldUInt32 _logStartOffset;

		/// <summary>
		/// Tracks the last written log file.
		/// </summary>
		private readonly BufferFieldUInt32 _logEndFileId;

		/// <summary>
		/// Tracks the next free insert location for writes to the log file.
		/// </summary>
		private readonly BufferFieldUInt32 _logEndOffset;

		/// <summary>
		/// Tracks the number of checkpoint history records
		/// </summary>
		private readonly BufferFieldByte _checkPointHistoryCount;
		private CheckPointInfo[] lastCheckPoint;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MasterLogRootPage"/> class.
		/// </summary>
		public MasterLogRootPage()
		{
			_deviceById = new Dictionary<DeviceId, DeviceInfo>();
			_devicesByIndex = new List<DeviceInfo>();

			_deviceCount = new BufferFieldUInt16(base.LastHeaderField);
			_logLastFileId = new BufferFieldUInt32(_deviceCount);
			_logStartFileId = new BufferFieldUInt32(_logLastFileId);
			_logStartOffset = new BufferFieldUInt32(_logStartFileId);
			_logEndFileId = new BufferFieldUInt32(_logStartOffset);
			_logEndOffset = new BufferFieldUInt32(_logEndFileId);
			_checkPointHistoryCount = new BufferFieldByte(_logEndOffset);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the log last file id.
		/// </summary>
		/// <value>The log last file id.</value>
		public uint LogLastFileId
		{
			get
			{
				return _logLastFileId.Value;
			}
			set
			{
				CheckReadOnly();
				if (_logLastFileId.Value != value)
				{
					_logLastFileId.Value = value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets or sets the log start file id.
		/// </summary>
		/// <value>The log start file id.</value>
		public uint LogStartFileId
		{
			get
			{
				return _logStartFileId.Value;
			}
			set
			{
				CheckReadOnly();
				if (_logStartFileId.Value != value)
				{
					_logStartFileId.Value = value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets or sets the log start offset.
		/// </summary>
		/// <value>The log start offset.</value>
		public uint LogStartOffset
		{
			get
			{
				return _logStartOffset.Value;
			}
			set
			{
				CheckReadOnly();
				if (_logStartOffset.Value != value)
				{
					_logStartOffset.Value = value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets or sets the log end file id.
		/// </summary>
		/// <value>The log end file id.</value>
		public uint LogEndFileId
		{
			get
			{
				return _logEndFileId.Value;
			}
			set
			{
				CheckReadOnly();
				if (_logEndFileId.Value != value)
				{
					_logEndFileId.Value = value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets or sets the log end offset.
		/// </summary>
		/// <value>The log end offset.</value>
		public uint LogEndOffset
		{
			get
			{
				return _logEndOffset.Value;
			}
			set
			{
				CheckReadOnly();
				if (_logEndOffset.Value != value)
				{
					_logEndOffset.Value = value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets the check point history count.
		/// </summary>
		/// <value>The check point history count.</value>
		public byte CheckPointHistoryCount => _checkPointHistoryCount.Value;

	    /// <summary>
		/// Overridden. Returns the minimum header size for this page object.
		/// </summary>
		public override uint MinHeaderSize => base.MinHeaderSize + 23;

	    /// <summary>
		/// Gets a value indicating the number of registered devices.
		/// </summary>
		public int DeviceCount
		{
			get
			{
				System.Diagnostics.Debug.Assert(_deviceById.Count ==
					_devicesByIndex.Count, "Device container size mismatch!");
				return _deviceById.Count;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Adds the device to the root page record.
		/// </summary>
		/// <param name="device"></param>
		internal void AddDevice(DeviceInfo deviceInfo)
		{
			_deviceById.Add(deviceInfo.Id, deviceInfo);
			_devicesByIndex.Add(deviceInfo);

			// We are dirty...
			SetHeaderDirty();
			SetDataDirty();
		}

		internal DeviceInfo GetDeviceById(DeviceId deviceId)
		{
			return _deviceById[deviceId];
		}

		internal DeviceInfo GetDeviceByIndex(int index)
		{
			return _devicesByIndex[index];
		}

		internal CheckPointInfo GetCheckPointHistory(int index)
		{
			if (index < 0 || index >= _checkPointHistoryCount.Value)
			{
				throw new ArgumentOutOfRangeException(nameof(index), index, "Index out of range");
			}
			if (lastCheckPoint == null)
				return null;
			return lastCheckPoint[index];
		}

		internal void AddCheckPoint(uint fileId, uint offset, bool start)
		{
			if (lastCheckPoint == null)
			{
				lastCheckPoint = new CheckPointInfo[3];
			}
			if (_checkPointHistoryCount.Value < 3)
			{
				CheckPointInfo cpi = null;
				if (start)
				{
					cpi = new CheckPointInfo();
					lastCheckPoint[_checkPointHistoryCount.Value] = cpi;
					cpi.BeginFileId = fileId;
					cpi.BeginOffset = offset;
				}
				else
				{
					cpi = lastCheckPoint[_checkPointHistoryCount.Value++];
					cpi.EndFileId = fileId;
					cpi.EndOffset = offset;
					cpi.Valid = true;
				}
			}
			else
			{
				lastCheckPoint[0] = lastCheckPoint[1];
				lastCheckPoint[1] = lastCheckPoint[2];
				CheckPointInfo cpi = null;
				if (start)
				{
					cpi = new CheckPointInfo();
					lastCheckPoint[2] = cpi;
					cpi.BeginFileId = fileId;
					cpi.BeginOffset = offset;
				}
				else
				{
					cpi = lastCheckPoint[2];
					cpi.EndFileId = fileId;
					cpi.EndOffset = offset;
					cpi.Valid = true;
				}
			}

			// Do not record root page changes for begin checkpoint
			if (!start)
			{
				SetHeaderDirty();
				SetDataDirty();
			}
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the last header field.
		/// </summary>
		/// <value>The last header field.</value>
		protected override BufferField LastHeaderField => _checkPointHistoryCount;

	    #endregion

		#region Protected Methods
		protected override void WriteHeader(BufferReaderWriter streamManager)
		{
			_deviceCount.Value = (ushort)_devicesByIndex.Count;
			base.WriteHeader(streamManager);
		}

		protected override void WriteData(BufferReaderWriter streamManager)
		{
			base.WriteData(streamManager);

			// Write checkpoint history
			System.Diagnostics.Debug.Assert(_checkPointHistoryCount.Value < 4);
			for (var index = 0; index < _checkPointHistoryCount.Value; ++index)
			{
				if (lastCheckPoint[index] == null)
				{
					lastCheckPoint[index] = new CheckPointInfo();
				}
				lastCheckPoint[index].Read(streamManager);
			}

			// then device list
			foreach (var info in _devicesByIndex)
			{
				info.Write(streamManager);
			}
		}

		protected override void ReadData(BufferReaderWriter streamManager)
		{
			base.ReadData(streamManager);

			if (_checkPointHistoryCount.Value > 0)
			{
				lastCheckPoint = new CheckPointInfo[3];
				System.Diagnostics.Debug.Assert(_checkPointHistoryCount.Value < 4);
				for (var index = 0; index < _checkPointHistoryCount.Value; ++index)
				{
					lastCheckPoint[index] = new CheckPointInfo();
					lastCheckPoint[index].Read(streamManager);
				}
			}

			// then device list
			_deviceById.Clear();
			_devicesByIndex.Clear();
			if (_deviceCount.Value > 0)
			{
				for (byte index = 0; index < _deviceCount.Value; ++index)
				{
					var info = new DeviceInfo();
					info.Read(streamManager);

					_deviceById.Add(info.Id, info);
					_devicesByIndex.Add(info);
				}
			}
		}
		#endregion
	}
}
