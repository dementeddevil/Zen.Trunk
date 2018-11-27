namespace Zen.Trunk.Storage.Data.Table
{
    using Index;
    using System;

    /// <summary>
    /// Class containing the implementation for index page splitting for
    /// database tables.
    /// </summary>
    public class TableIndexPage : IndexPage<TableIndexInfo, RootTableIndexInfo>
    {
        #region Private Fields
        private DatabaseTable _ownerTable;
        private RootTableIndexInfo _rootIndex;
        #endregion

        #region Public Properties
        /*public RootTableIndexInfo RootIndex
		{
			get
			{
				return _rootIndex;
			}
			set
			{
				if (_rootIndex != value)
				{
					if (_rootIndex != null)
					{
						ObjectId = 0;
					}
					_rootIndex = value;
					if (_rootIndex != null)
					{
						ObjectId = _rootIndex.ObjectId;
					}
				}
			}
		}*/

        /// <summary>
        /// Overridden. Gets the index manager.
        /// </summary>
        /// <value>The index manager.</value>
        public override IndexManager IndexManager => GetService<TableIndexManager>();

        /// <summary>
        /// Gets the max index entries.
        /// </summary>
        /// <value>The max index entries.</value>
        public override ushort MaxIndexEntries => (ushort)(DataSize / KeySize);

        /// <summary>
        /// Gets the size of the key for this page.
        /// </summary>
        /// <value>The size of the key.</value>
        public ushort KeySize
        {
            get
            {
                if (_rootIndex == null)
                {
                    throw new InvalidOperationException("Attempt to get key size without assigning RootIndex object.");
                }

                if (IsLeafIndex)
                {
                    if (IsNormalOverClusteredIndex)
                    {
                        // Index Key Size + Clustered Key Size
                        return (ushort)(_rootIndex.KeySize + _ownerTable.ClusteredIndex.KeySize);
                    }
                    else
                    {
                        // Index Key Size + Logical Page Id + Page Row Id
                        return (ushort)(_rootIndex.KeySize + 10);
                    }
                }
                else
                {
                    // Index Key Size + Logical Page Id
                    return (ushort)(_rootIndex.KeySize + 8);
                }
            }
        }

        /// <summary>
        /// Gets the size of the min header.
        /// </summary>
        /// <value>The size of the min header.</value>
        public override uint MinHeaderSize => base.MinHeaderSize + 0;

        /// <summary>
        /// Gets a value indicating whether the index this page is attached to
        /// is a normal index operating over a clustered index
        /// </summary>
        /// <value>
        /// <c>true</c> if the associated index is a normal index operating
        /// over a clustered index; otherwise, <c>false</c>.
        /// </value>
        public bool IsNormalOverClusteredIndex
        {
            get
            {
                if (_ownerTable.IsHeap || (_rootIndex.IndexSubType & TableIndexSubType.Clustered) == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the owner table has a clustered index.
        /// </summary>
        public bool IsOwnerTableClustered => !_ownerTable.IsHeap;
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the context.
        /// </summary>
        /// <param name="def">The definition.</param>
        /// <param name="rootIndex">Index of the root.</param>
        public void SetContext(DatabaseTable def, RootTableIndexInfo rootIndex)
        {
            _ownerTable = def;
            _rootIndex = rootIndex;
            for (var index = 0; index < IndexCount; ++index)
            {
                IndexEntries[index].SetContext(def, rootIndex);
            }
        }

        /// <summary>
        /// Compares the indexed keys at the specified ordinal with the values
        /// given and returns an integer indicating whether the indexed keys
        /// appear before, at the same position or after the candidate values.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="keys">The keys.</param>
        /// <returns></returns>
        public int CompareIndex(int index, TableIndexInfo keys)
        {
            return index < IndexCount ? IndexEntries[index].CompareTo(keys) : -1;
        }

        /// <summary>
        /// Attempts to get the index leaf information for the index entry at
        /// the specified ordinal.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="leaf">An output parameter containing the leaf info.</param>
        /// <returns></returns>
        public bool TryGetIndexEntryLeafInfo(int index, out TableIndexLeafInfo leaf)
        {
            if (Depth == 0)
            {
                leaf = (TableIndexLeafInfo) IndexEntries[index];
                return true;
            }

            leaf = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the logical page identifier for the index entry at
        /// the specified ordinal.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="logicalPageId">An output parameter containing the
        /// logical page identifier.</param>
        /// <returns></returns>
        public bool TryGetIndexEntryLogicalPageId(int index, out LogicalPageId logicalPageId)
        {
            if (Depth > 0)
            {
                logicalPageId = ((TableIndexLogicalInfo) IndexEntries[index]).LogicalPageId;
                return true;
            }

            logicalPageId = LogicalPageId.Zero;
            return false;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Creates an index link to the first entry in this index page
        /// </summary>
        /// <param name="page"></param>
        protected override TableIndexInfo CreateLinkToPage(
            IndexPage<TableIndexInfo, RootTableIndexInfo> page)
        {
            var keys = page.IndexEntries[0].Keys;
            return new TableIndexLogicalInfo(keys, page.LogicalPageId);
        }

        /// <summary>
        /// Adds an intermediate link to the given page.
        /// </summary>
        /// <returns></returns>
        protected override TableIndexInfo CreateIndexEntry()
        {
            if (IsLeafIndex)
            {
                if (IsNormalOverClusteredIndex)
                {
                    // Normal index over table with clustered key have something different
                    // clustered key length is based on the number of fields in the clustered index
                    //	we need a pointer to that information in the owner table
                    return new TableIndexNormalOverClusteredLeafInfo(
                        IndexEntries[0].KeyLength, _ownerTable.ClusteredIndex.ColumnIDs.Length);
                }
                else if (IsOwnerTableClustered)
                {
                    // Clustered index leaf info just uses logical id to point to data page
                    return new TableIndexClusteredLeafInfo(IndexEntries[0].KeyLength);
                }
                else
                {
                    // Heaps have leaf info pointing to row index 
                    return new TableIndexNormalLeafInfo(IndexEntries[0].KeyLength);
                }
            }
            else
            {
                // Root and intermediate pages use logical info
                return new TableIndexLogicalInfo(IndexEntries[0].KeyLength);
            }
        }
        #endregion
    }
}
