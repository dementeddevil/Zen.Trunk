using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Data.Table
{
    /// <summary>
    /// TableSchemaRootPage extends <see cref="TableSchemaPage"/> for holding
    /// extra information needed for recording table extends.
    /// </summary>
    /// <remarks>
    /// The table schema root page object maintains two additional properties;
    /// 1. The first data page
    /// 2. The last data page
    /// </remarks>
    public class TableSchemaRootPage : TableSchemaPage
    {
        #region Private Fields
        private readonly BufferFieldUInt64 _dataFirstLogicalPageId;
        private readonly BufferFieldUInt64 _dataLastLogicalPageId;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TableSchemaRootPage"/> class.
        /// </summary>
        public TableSchemaRootPage()
        {
            _dataFirstLogicalPageId = new BufferFieldUInt64(base.LastHeaderField);
            _dataLastLogicalPageId = new BufferFieldUInt64(_dataFirstLogicalPageId);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the minimum size of the header.
        /// </summary>
        /// <value>
        /// The minimum size of the header.
        /// </value>
        public override uint MinHeaderSize => base.MinHeaderSize + 16;

        /// <summary>
        /// Gets or sets the logical page identifier of the first data page for the table.
        /// </summary>
        /// <value>
        /// The logical page identifier.
        /// </value>
        public LogicalPageId DataFirstLogicalPageId
        {
            get => new LogicalPageId(_dataFirstLogicalPageId.Value);
            set
            {
                if (_dataFirstLogicalPageId.Value != value.Value)
                {
                    _dataFirstLogicalPageId.Value = value.Value;
                    SetHeaderDirty();
                }
            }
        }

        /// <summary>
        /// Gets or sets the logical page identifier of the last data page for the table.
        /// </summary>
        /// <value>
        /// The logical page identifier.
        /// </value>
        public LogicalPageId DataLastLogicalPageId
        {
            get => new LogicalPageId(_dataLastLogicalPageId.Value);
            set
            {
                if (_dataLastLogicalPageId.Value != value.Value)
                {
                    _dataLastLogicalPageId.Value = value.Value;
                    SetHeaderDirty();
                }
            }
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the last header field.
        /// </summary>
        /// <value>
        /// The last header field.
        /// </value>
        protected override BufferField LastHeaderField => _dataLastLogicalPageId;

        #endregion
    }
}