using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Logging
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="BufferFieldWrapper" />
    public class CheckPointInfo : BufferFieldWrapper
    {
        private readonly BufferFieldLogFileId _beginLogFileId;
        private readonly BufferFieldUInt32 _beginOffset;
        private readonly BufferFieldLogFileId _endLogFileId;
        private readonly BufferFieldUInt32 _endOffset;
        private readonly BufferFieldBitVector8 _status;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckPointInfo"/> class.
        /// </summary>
        public CheckPointInfo()
        {
            _beginLogFileId = new BufferFieldLogFileId();
            _beginOffset = new BufferFieldUInt32(_beginLogFileId);
            _endLogFileId = new BufferFieldLogFileId(_beginOffset);
            _endOffset = new BufferFieldUInt32(_endLogFileId);
            _status = new BufferFieldBitVector8(_endOffset);
        }

        /// <summary>
        /// Gets the first buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField FirstField => _beginLogFileId;

        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _status;

        /// <summary>
        /// Gets or sets the begin log file identifier.
        /// </summary>
        /// <value>
        /// The begin log file identifier.
        /// </value>
        public LogFileId BeginLogFileId
        {
            get => _beginLogFileId.Value;
            set => _beginLogFileId.Value = value;
        }

        /// <summary>
        /// Gets or sets the begin offset.
        /// </summary>
        /// <value>
        /// The begin offset.
        /// </value>
        public uint BeginOffset
        {
            get => _beginOffset.Value;
            set => _beginOffset.Value = value;
        }

        /// <summary>
        /// Gets or sets the end log file identifier.
        /// </summary>
        /// <value>
        /// The end log file identifier.
        /// </value>
        public LogFileId EndLogFileId
        {
            get => _endLogFileId.Value;
            set => _endLogFileId.Value = value;
        }

        /// <summary>
        /// Gets or sets the end offset.
        /// </summary>
        /// <value>
        /// The end offset.
        /// </value>
        public uint EndOffset
        {
            get => _endOffset.Value;
            set => _endOffset.Value = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CheckPointInfo"/> is valid.
        /// </summary>
        /// <value>
        /// <c>true</c> if valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid
        {
            get => _status.GetBit(1);
            set => _status.SetBit(1, value);
        }
    }
}
