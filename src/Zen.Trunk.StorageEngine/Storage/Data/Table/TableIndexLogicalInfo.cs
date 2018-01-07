using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Storage.Data.Index;

namespace Zen.Trunk.Storage.Data.Table
{
    /// <summary>
    /// <c>TableIndexLogicalInfo</c> is used to describe root and intermediate
    /// index pages.
    /// </summary>
    public class TableIndexLogicalInfo : TableIndexInfo
    {
        #region Private Fields
        private readonly BufferFieldLogicalPageId _logicalId;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexInfo" /> class.
        /// </summary>
        /// <param name="keySize">Size of the key.</param>
        public TableIndexLogicalInfo(int keySize)
            : base(keySize)
        {
            _logicalId = new BufferFieldLogicalPageId(LogicalPageId.Zero);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexInfo" /> class.
        /// </summary>
        /// <param name="keys">The keys.</param>
        public TableIndexLogicalInfo(object[] keys)
            : base(keys)
        {
            _logicalId = new BufferFieldLogicalPageId(LogicalPageId.Zero);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexInfo" /> class.
        /// </summary>
        /// <param name="keySize">Size of the key.</param>
        /// <param name="logicalId">The logical id.</param>
        public TableIndexLogicalInfo(int keySize, LogicalPageId logicalId)
            : base(keySize)
        {
            _logicalId = new BufferFieldLogicalPageId(logicalId);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexInfo" /> class.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <param name="logicalId">The logical id.</param>
        public TableIndexLogicalInfo(object[] keys, LogicalPageId logicalId)
            : base(keys)
        {
            _logicalId = new BufferFieldLogicalPageId(logicalId);
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