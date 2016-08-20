namespace Zen.Trunk.Storage.Log
{
	using System;

	public class VirtualLogFileInfo : BufferFieldWrapper
	{
		#region Private Fields
		private BufferFieldBitVector8 _status;
		private BufferFieldLogFileId _id;
		private BufferFieldInt64 _startOffset;
		private BufferFieldUInt32 _length;

		private VirtualLogFileHeader _currentHeader;
		#endregion

		#region Public Constructors
		public VirtualLogFileInfo()
		{
			_status = new BufferFieldBitVector8();
			_id = new BufferFieldLogFileId(_status);
			_startOffset = new BufferFieldInt64(_id);
			_length = new BufferFieldUInt32(_startOffset);

			_currentHeader = new VirtualLogFileHeader();
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets/sets device Id.
		/// </summary>
		public ushort DeviceId
		{
			get
			{
				return _id.Value.DeviceId;
			}
			set
			{
				_id.Value.DeviceId = value;
			}
		}

		/// <summary>
		/// Gets/sets the virtual log index into the device.
		/// </summary>
		public ushort IndexId
		{
			get
			{
				return _id.Value.Index;
			}
			set
			{
				_id.Value.Index = value;
			}
		}

		/// <summary>
		/// Gets/sets the start offset.
		/// </summary>
		public long StartOffset
		{
			get
			{
				return _startOffset.Value;
			}
			set
			{
				_startOffset.Value = value;
			}
		}

		/// <summary>
		/// Gets/sets the log stream length.
		/// </summary>
		public uint Length
		{
			get
			{
				return _length.Value;
			}
			set
			{
				_length.Value = value;
			}
		}

		/// <summary>
		/// Gets/sets the log file Id.
		/// </summary>
		public uint FileId
		{
			get
			{
				return _id.Value.FileId;
			}
			set
			{
				_id.Value.FileId = value;
			}
		}

		/// <summary>
		/// Gets/sets the header status
		/// </summary>
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

		public bool IsAllocated
		{
			get
			{
				return _status.GetBit(0x01);
			}
			set
			{
				_status.SetBit(0x01, value);
			}
		}

		public bool IsFull
		{
			get
			{
				return _status.GetBit(0x02);
			}
			set
			{
				_status.SetBit(0x02, value);
			}
		}

		public VirtualLogFileHeader CurrentHeader
		{
			get
			{
				return _currentHeader;
			}
			set
			{
				_currentHeader = value;
			}
		}
		#endregion

		#region Protected Properties
		protected override BufferField FirstField
		{
			get
			{
				return _status;
			}
		}

		protected override BufferField LastField
		{
			get
			{
				return _length;
			}
		}
		#endregion
	}

	public class VirtualLogFileHeader : BufferFieldWrapper
	{
		#region Private Fields
		private BufferFieldInt64 _timestamp;
		private BufferFieldUInt32 _lastCursor;
		private BufferFieldUInt32 _cursor;
		private BufferFieldUInt32 _prevFileId;
		private BufferFieldUInt32 _nextFileId;
		private BufferFieldInt32 _hash;
		#endregion

		#region Public Constructors
		public VirtualLogFileHeader()
		{
			_timestamp = new BufferFieldInt64();
			_lastCursor = new BufferFieldUInt32(_timestamp);
			_cursor = new BufferFieldUInt32(_lastCursor);
			_prevFileId = new BufferFieldUInt32(_cursor);
			_nextFileId = new BufferFieldUInt32(_prevFileId);
			_hash = new BufferFieldInt32(_nextFileId);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets/sets the header timestamp.
		/// </summary>
		public long Timestamp
		{
			get
			{
				return _timestamp.Value;
			}
			set
			{
				_timestamp.Value = value;
			}
		}

		/// <summary>
		/// Gets/sets the previous file Id.
		/// </summary>
		public uint PrevFileId
		{
			get
			{
				return _prevFileId.Value;
			}
			set
			{
				_prevFileId.Value = value;
			}
		}

		/// <summary>
		/// Gets/sets the next file Id.
		/// </summary>
		public uint NextFileId
		{
			get
			{
				return _nextFileId.Value;
			}
			set
			{
				_nextFileId.Value = value;
			}
		}

		/// <summary>
		/// Gets/sets the last cursor position.
		/// </summary>
		public uint LastCursor
		{
			get
			{
				return _lastCursor.Value;
			}
			set
			{
				_lastCursor.Value = value;
			}
		}

		/// <summary>
		/// Gets/sets the cursor position.
		/// </summary>
		public uint Cursor
		{
			get
			{
				return _cursor.Value;
			}
			set
			{
				_cursor.Value = value;
			}
		}

		/// <summary>
		/// Gets/sets the header hash.
		/// </summary>
		public int Hash
		{
			get
			{
				return _hash.Value;
			}
			set
			{
				_hash.Value = value;
			}
		}
		#endregion

		#region Protected Properties
		protected override BufferField FirstField
		{
			get
			{
				return _timestamp;
			}
		}

		protected override BufferField LastField
		{
			get
			{
				return _hash;
			}
		}
		#endregion
	}
}
