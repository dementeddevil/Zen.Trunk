using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// <c>VirtualLogFileHeader</c> carries important information about a
    /// virtual log file.
    /// </summary>
    /// <seealso cref="BufferFieldWrapper" />
    public class VirtualLogFileHeader : BufferFieldWrapper
    {
        #region Private Fields
        private readonly BufferFieldInt64 _timestamp;
        private readonly BufferFieldUInt32 _lastCursor;
        private readonly BufferFieldUInt32 _cursor;
        private readonly BufferFieldLogFileId _previousLogFileId;
        private readonly BufferFieldLogFileId _nextLogFileId;
        private readonly BufferFieldInt32 _hash;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualLogFileHeader" /> class.
        /// </summary>
        public VirtualLogFileHeader()
        {
            _timestamp = new BufferFieldInt64();
            _lastCursor = new BufferFieldUInt32(_timestamp);
            _cursor = new BufferFieldUInt32(_lastCursor);
            _previousLogFileId = new BufferFieldLogFileId(_cursor);
            _nextLogFileId = new BufferFieldLogFileId(_previousLogFileId);
            _hash = new BufferFieldInt32(_nextLogFileId);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets/sets the header timestamp.
        /// </summary>
        /// <value>
        /// The timestamp.
        /// </value>
        public long Timestamp
        {
            get => _timestamp.Value;
            set => _timestamp.Value = value;
        }

        /// <summary>
        /// Gets/sets the previous log file Id.
        /// </summary>
        /// <value>
        /// The previous log file identifier.
        /// </value>
        public LogFileId PreviousLogFileId
        {
            get => _previousLogFileId.Value;
            set => _previousLogFileId.Value = value;
        }

        /// <summary>
        /// Gets/sets the next log file Id.
        /// </summary>
        /// <value>
        /// The next log file identifier.
        /// </value>
        public LogFileId NextLogFileId
        {
            get => _nextLogFileId.Value;
            set => _nextLogFileId.Value = value;
        }

        /// <summary>
        /// Gets/sets the last cursor position.
        /// </summary>
        /// <value>
        /// The last cursor position.
        /// </value>
        public uint LastCursor
        {
            get => _lastCursor.Value;
            set => _lastCursor.Value = value;
        }

        /// <summary>
        /// Gets/sets the cursor position.
        /// </summary>
        /// <value>
        /// The cursor position.
        /// </value>
        public uint Cursor
        {
            get => _cursor.Value;
            set => _cursor.Value = value;
        }

        /// <summary>
        /// Gets/sets the header hash.
        /// </summary>
        /// <value>
        /// The hash value is simply a hash of the <see cref="Timestamp"/> field.
        /// </value>
        public int Hash
        {
            get => _hash.Value;
            set => _hash.Value = value;
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the first buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField FirstField => _timestamp;

        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _hash;
        #endregion
    }
}