using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Data.Index
{
    using System;
    using System.Collections.Specialized;

    /// <summary>
    /// <c>IndexPage</c> is a base class for pages needing indexing capability.
    /// </summary>
    /// <remarks>
    /// Index page handles a binary tree spread across multiple logical pages. 
    /// </remarks>
    public abstract class IndexPage : ObjectDataPage
    {
        #region Private Fields
        private readonly BufferFieldIndexId _indexId;
        private readonly BufferFieldLogicalPageId _leftLogicalPageId;
        private readonly BufferFieldLogicalPageId _rightLogicalPageId;
        private readonly BufferFieldLogicalPageId _parentLogicalPageId;
        private readonly BufferFieldByte _depth;

        private BitVector32 _status;
        private BitVector32.Section _indexType;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexPage"/> class.
        /// </summary>
        protected IndexPage()
        {
            _indexId = new BufferFieldIndexId(base.LastHeaderField);
            _leftLogicalPageId = new BufferFieldLogicalPageId(_indexId);
            _rightLogicalPageId = new BufferFieldLogicalPageId(_leftLogicalPageId);
            _parentLogicalPageId = new BufferFieldLogicalPageId(_rightLogicalPageId);
            _depth = new BufferFieldByte(_parentLogicalPageId);

            PageType = PageType.Index;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the minimum number of bytes required for the header block.
        /// </summary>
        /// <value></value>
        public override uint MinHeaderSize => base.MinHeaderSize + 27;

        /// <summary>
        /// Gets the index manager.
        /// </summary>
        /// <value>The index manager.</value>
        public abstract IndexManager IndexManager
        {
            get;
        }

        /// <summary>
        /// Gets the max index entries.
        /// </summary>
        /// <value>The max index entries.</value>
        public abstract ushort MaxIndexEntries
        {
            get;
        }

        /// <summary>
        /// Gets or sets the type of the index.
        /// </summary>
        /// <value>The type of the index.</value>
        public IndexType IndexType
        {
            get => (IndexType)_status[_indexType];
            set
            {
                CheckReadOnly();
                if (IndexType != value)
                {
                    _status[_indexType] = (int)value;
                    SetHeaderDirty();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a root
        /// index page.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if this instance is root index; otherwise,
        /// <see langword="false"/>.
        /// </value>
        public bool IsRootIndex
        {
            get => (IndexType & IndexType.Root) != 0;
            set
            {
                if (value)
                {
                    IndexType |= IndexType.Root;
                }
                else
                {
                    IndexType &= ~IndexType.Root;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is an 
        /// intermediate index page.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if this instance is root index; otherwise,
        /// <see langword="false"/>.
        /// </value>
        public bool IsIntermediateIndex
        {
            get => (IndexType & IndexType.Intermediate) != 0;
            set
            {
                if (value)
                {
                    IndexType |= IndexType.Intermediate;
                }
                else
                {
                    IndexType &= ~IndexType.Intermediate;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a root index page.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if this instance is root index; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsLeafIndex
        {
            get => (IndexType & IndexType.Leaf) != 0;
            set
            {
                if (value)
                {
                    IndexType |= IndexType.Leaf;
                }
                else
                {
                    IndexType &= ~IndexType.Leaf;
                }
            }
        }

        /// <summary>
        /// Gets or sets the index identifier.
        /// </summary>
        public IndexId IndexId
        {
            get => _indexId.Value;
            set
            {
                CheckReadOnly();
                if (_indexId.Value != value)
                {
                    _indexId.Value = value;
                    SetHeaderDirty();
                }
            }
        }

        /// <summary>
        /// Gets or sets the left logical page id.
        /// </summary>
        /// <value>The left logical page id.</value>
        public LogicalPageId LeftLogicalPageId
        {
            get => _leftLogicalPageId.Value;
            set
            {
                CheckReadOnly();
                if (_leftLogicalPageId.Value != value)
                {
                    _leftLogicalPageId.Value = value;
                    SetHeaderDirty();
                }
            }
        }

        /// <summary>
        /// Gets or sets the right logical page id.
        /// </summary>
        /// <value>The right logical page id.</value>
        public LogicalPageId RightLogicalPageId
        {
            get => _rightLogicalPageId.Value;
            set
            {
                CheckReadOnly();
                if (_rightLogicalPageId.Value != value)
                {
                    _rightLogicalPageId.Value = value;
                    SetHeaderDirty();
                }
            }
        }

        /// <summary>
        /// Gets or sets the parent logical page id.
        /// </summary>
        /// <value>The parent logical page id.</value>
        public LogicalPageId ParentLogicalPageId
        {
            get => _parentLogicalPageId.Value;
            set
            {
                CheckReadOnly();
                if (_parentLogicalPageId.Value != value)
                {
                    _parentLogicalPageId.Value = value;
                    SetHeaderDirty();
                }
            }
        }

        /// <summary>
        /// Gets or sets the index page depth.
        /// </summary>
        /// <value>The depth.</value>
        /// <remarks>
        /// Index pages with a depth of zero are index leaf index pages.
        /// Everything else is an intermediate index index page with the 
        /// exception of the root index page (which also has the highest depth
        /// value)
        /// Once an index page is created, it's depth value will never change.
        /// </remarks>
        public byte Depth
        {
            get => _depth.Value;
            set
            {
                CheckReadOnly();
                if (_depth.Value != value)
                {
                    _depth.Value = value;
                    SetHeaderDirty();
                }
            }
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the last header field.
        /// </summary>
        /// <value>The last header field.</value>
        protected override BufferField LastHeaderField => _depth;
        #endregion

        #region Protected Methods
        /// <summary>
        /// Creates the status sections.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="previousSection">The previous section.</param>
        /// <returns></returns>
        protected override BitVector32.Section CreateStatusSections(BitVector32 status, BitVector32.Section previousSection)
        {
            previousSection = base.CreateStatusSections(status, previousSection);

            _status = status;
            _indexType = BitVector32.CreateSection((short)IndexType.Leaf, previousSection);

            return _indexType;
        }
        #endregion
    }
}
