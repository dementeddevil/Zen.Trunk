namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// <c>VirtualLogFileInfo</c> defines information about a virtual log file.
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.BufferFieldWrapper" />
    public class VirtualLogFileInfo : BufferFieldWrapper
	{
		#region Private Fields
		private readonly BufferFieldBitVector8 _status;
		private readonly BufferFieldLogFileId _id;
		private readonly BufferFieldInt64 _startOffset;
		private readonly BufferFieldUInt32 _length;
	    #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualLogFileInfo"/> class.
        /// </summary>
        public VirtualLogFileInfo()
		{
			_status = new BufferFieldBitVector8();
			_id = new BufferFieldLogFileId(_status);
			_startOffset = new BufferFieldInt64(_id);
			_length = new BufferFieldUInt32(_startOffset);

			CurrentHeader = new VirtualLogFileHeader();
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets/sets device Id.
		/// </summary>
		public DeviceId DeviceId
		{
			get
			{
				return _id.Value.DeviceId;
			}
			set
			{
			    if (_id.Value.DeviceId != value)
			    {
			        _id.Value = new LogFileId(value, _id.Value.Index);
			    }
			}
		}

		/// <summary>
		/// Gets/sets the virtual log index into the device.
		/// </summary>
		public ushort Index
		{
			get
			{
				return _id.Value.Index;
			}
			set
			{
			    if (_id.Value.Index != value)
			    {
			        _id.Value = new LogFileId(_id.Value.DeviceId, value);
			    }
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
		/// Gets the log file Id.
		/// </summary>
		public LogFileId FileId => _id.Value;

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

        /// <summary>
        /// Gets or sets a value indicating whether this instance is allocated.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is allocated; otherwise, <c>false</c>.
        /// </value>
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

        /// <summary>
        /// Gets or sets a value indicating whether this instance is full.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is full; otherwise, <c>false</c>.
        /// </value>
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

        /// <summary>
        /// Gets or sets the current header.
        /// </summary>
        /// <value>
        /// The current header.
        /// </value>
        public VirtualLogFileHeader CurrentHeader { get; set; }
	    #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the first buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField FirstField => _status;

        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _length;
	    #endregion
	}
}
