// -----------------------------------------------------------------------
// <copyright file="MasterLogPage.cs" company="Zen Design Software">
// © Zen Design Software 2009 - 2016
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// <c>MasterLogRootPage</c> tracks essential information about the logging
    /// infrastructure.
    /// </summary>
    public class MasterLogRootPage : LogRootPage
	{
		#region Private Fields
		private readonly Dictionary<DeviceId, DeviceInfo> _deviceById;
		private readonly List<DeviceInfo> _devicesByIndex;

		private readonly BufferFieldUInt16 _deviceCount;
		private readonly BufferFieldLogFileId _lastLogFileId;
		private readonly BufferFieldLogFileId _startLogFileId;
		private readonly BufferFieldUInt32 _startLogOffset;
		private readonly BufferFieldLogFileId _endLogFileId;
		private readonly BufferFieldUInt32 _endLogOffset;
		private readonly BufferFieldByte _checkPointHistoryCount;

		private CheckPointInfo[] _lastCheckPoint;
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
			_lastLogFileId = new BufferFieldLogFileId(_deviceCount);
			_startLogFileId = new BufferFieldLogFileId(_lastLogFileId);
			_startLogOffset = new BufferFieldUInt32(_startLogFileId);
			_endLogFileId = new BufferFieldLogFileId(_startLogOffset);
			_endLogOffset = new BufferFieldUInt32(_endLogFileId);
			_checkPointHistoryCount = new BufferFieldByte(_endLogOffset);
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the last log file identifier created.
        /// </summary>
        /// <value>
        /// The last log file identifier.
        /// </value>
		/// <remarks>
		/// This value is used to complete linked-list chaining during
		/// the creation of new virtual log files.
		/// </remarks>
        public LogFileId LastLogFileId
		{
			get
			{
				return _lastLogFileId.Value;
			}
			set
			{
				CheckReadOnly();
				if (_lastLogFileId.Value != value)
				{
					_lastLogFileId.Value = value;
					SetHeaderDirty();
				}
			}
		}

        /// <summary>
        /// Gets or sets the start log file identifier.
        /// </summary>
        /// <value>
        /// The start log file identifier.
        /// </value>
		/// <remarks>
		/// This is the first file which must be kept in order to successfully
		/// recover the database. It is adjusted during recovery and checkpoint
		/// operations.
		/// </remarks>
        public LogFileId StartLogFileId
		{
			get
			{
				return _startLogFileId.Value;
			}
			set
			{
				CheckReadOnly();
				if (_startLogFileId.Value != value)
				{
					_startLogFileId.Value = value;
					SetHeaderDirty();
				}
			}
		}

        /// <summary>
        /// Gets or sets the start log offset.
        /// </summary>
        /// <value>
        /// The start log offset.
        /// </value>
        /// <remarks>
		/// Tracks the offset within the start file where log records must be
		/// preserved.
        /// </remarks>
        public uint StartLogOffset
		{
			get
			{
				return _startLogOffset.Value;
			}
			set
			{
				CheckReadOnly();
				if (_startLogOffset.Value != value)
				{
					_startLogOffset.Value = value;
					SetHeaderDirty();
				}
			}
		}

        /// <summary>
        /// Gets or sets the end log file identifier.
        /// </summary>
        /// <value>
        /// The end log file identifier.
        /// </value>
        /// <remarks>
        /// Tracks the last written log file.
        /// </remarks>
        public LogFileId EndLogFileId
		{
			get
			{
				return _endLogFileId.Value;
			}
			set
			{
				CheckReadOnly();
				if (_endLogFileId.Value != value)
				{
					_endLogFileId.Value = value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets or sets the log end offset.
		/// </summary>
		/// <value>The log end offset.</value>
		/// <remarks>
		/// Tracks the next free insert location for writes to the log file.
		/// </remarks>
		public uint EndLogOffset
		{
			get
			{
				return _endLogOffset.Value;
			}
			set
			{
				CheckReadOnly();
				if (_endLogOffset.Value != value)
				{
					_endLogOffset.Value = value;
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
		/// <param name="deviceInfo"></param>
		public void AddDevice(DeviceInfo deviceInfo)
		{
			_deviceById.Add(deviceInfo.Id, deviceInfo);
			_devicesByIndex.Add(deviceInfo);

			// We are dirty...
			SetHeaderDirty();
			SetDataDirty();
		}

        /// <summary>
        /// Gets the device by identifier.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns></returns>
        public DeviceInfo GetDeviceById(DeviceId deviceId)
		{
			return _deviceById[deviceId];
		}

        /// <summary>
        /// Gets the device information associated with the given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// A <see cref="DeviceInfo"/> object containing device information.
        /// </returns>
        public DeviceInfo GetDeviceByIndex(int index)
		{
			return _devicesByIndex[index];
		}

        /// <summary>
        /// Gets the check point history.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">Index out of range</exception>
        public CheckPointInfo GetCheckPointHistory(int index)
		{
			if (index < 0 || index >= _checkPointHistoryCount.Value)
			{
				throw new ArgumentOutOfRangeException(nameof(index), index, "Index out of range");
			}

		    return _lastCheckPoint?[index];
		}

        /// <summary>
        /// Adds the check point.
        /// </summary>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="start">if set to <c>true</c> [start].</param>
        public void AddCheckPoint(LogFileId fileId, uint offset, bool start)
		{
			if (_lastCheckPoint == null)
			{
				_lastCheckPoint = new CheckPointInfo[3];
			}
			if (_checkPointHistoryCount.Value < 3)
			{
				CheckPointInfo cpi;
				if (start)
				{
					cpi = new CheckPointInfo();
					_lastCheckPoint[_checkPointHistoryCount.Value] = cpi;
					cpi.BeginLogFileId = fileId;
					cpi.BeginOffset = offset;
				}
				else
				{
					cpi = _lastCheckPoint[_checkPointHistoryCount.Value++];
					cpi.EndLogFileId = fileId;
					cpi.EndOffset = offset;
					cpi.IsValid = true;
				}
			}
			else
			{
				_lastCheckPoint[0] = _lastCheckPoint[1];
				_lastCheckPoint[1] = _lastCheckPoint[2];
				CheckPointInfo cpi;
				if (start)
				{
					cpi = new CheckPointInfo();
					_lastCheckPoint[2] = cpi;
					cpi.BeginLogFileId = fileId;
					cpi.BeginOffset = offset;
				}
				else
				{
					cpi = _lastCheckPoint[2];
					cpi.EndLogFileId = fileId;
					cpi.EndOffset = offset;
					cpi.IsValid = true;
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
        /// <summary>
        /// Writes the page header block to the specified buffer writer.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void WriteHeader(SwitchingBinaryWriter streamManager)
		{
			_deviceCount.Value = (ushort)_devicesByIndex.Count;
			base.WriteHeader(streamManager);
		}

        /// <summary>
        /// Writes the page data block to the specified buffer writer.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void WriteData(SwitchingBinaryWriter streamManager)
		{
			base.WriteData(streamManager);

			// Write checkpoint history
			System.Diagnostics.Debug.Assert(_checkPointHistoryCount.Value < 4);
			for (var index = 0; index < _checkPointHistoryCount.Value; ++index)
			{
				if (_lastCheckPoint[index] == null)
				{
					_lastCheckPoint[index] = new CheckPointInfo();
				}

                _lastCheckPoint[index].Write(streamManager);
			}

			// then device list
			foreach (var info in _devicesByIndex)
			{
				info.Write(streamManager);
			}
		}

        /// <summary>
        /// Reads the page data block from the specified buffer reader.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void ReadData(SwitchingBinaryReader streamManager)
		{
			base.ReadData(streamManager);

			if (_checkPointHistoryCount.Value > 0)
			{
				_lastCheckPoint = new CheckPointInfo[3];
				System.Diagnostics.Debug.Assert(_checkPointHistoryCount.Value < 4);
				for (var index = 0; index < _checkPointHistoryCount.Value; ++index)
				{
					_lastCheckPoint[index] = new CheckPointInfo();
					_lastCheckPoint[index].Read(streamManager);
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
