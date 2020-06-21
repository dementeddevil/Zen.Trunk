using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Data.Audio
{
    public class AudioIndexLogicalInfo : AudioIndexInfo
    {
        #region Private Fields
        private readonly BufferFieldLogicalPageId _logicalId;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioIndexLogicalInfo"/> class.
        /// </summary>
        public AudioIndexLogicalInfo()
        {
            _logicalId = new BufferFieldLogicalPageId(LogicalPageId.Zero);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioIndexLogicalInfo"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public AudioIndexLogicalInfo(long value) : base(value)
        {
            _logicalId = new BufferFieldLogicalPageId(LogicalPageId.Zero);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioIndexLogicalInfo"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="logicalPageId">The logical page identifier.</param>
        public AudioIndexLogicalInfo(long value, LogicalPageId logicalPageId) : base(value)
        {
            _logicalId = new BufferFieldLogicalPageId(logicalPageId);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Represents the logical ID of the child index page (root and
        /// intermediate index pages only) or the logical ID of the
        /// data page (leaf pages only).
        /// </summary>
        public LogicalPageId LogicalPageId
        {
            get => _logicalId.Value;
            set => _logicalId.Value = value;
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the first buffer field object.
        /// </summary>
        /// <value>A <see cref="T:BufferField"/> object.</value>
        protected override BufferField FirstField => _logicalId;

        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>A <see cref="T:BufferField"/> object.</value>
        protected override BufferField LastField => _logicalId;
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _logicalId.Value.GetHashCode();
        }
        #endregion
    }
}

