using Zen.Trunk.Storage.Data.Index;

namespace Zen.Trunk.Storage.Data.Audio
{
    /// <summary>
    /// Class containing the implementation for index page splitting for
    /// database audios.
    /// </summary>
    public class AudioIndexPage : IndexPage<AudioIndexInfo, RootAudioIndexInfo>
    {
        #region Private Fields
        private DatabaseAudio _ownerTable;
        private RootAudioIndexInfo _rootIndex;
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
        public override IndexManager IndexManager => GetService<AudioIndexManager>();

        /// <summary>
        /// Gets the max index entries.
        /// </summary>
        /// <value>The max index entries.</value>
        public override ushort MaxIndexEntries => (ushort)(DataSize / 8);

        /// <summary>
        /// Gets the size of the min header.
        /// </summary>
        /// <value>The size of the min header.</value>
        public override uint MinHeaderSize => base.MinHeaderSize + 0;
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the context.
        /// </summary>
        /// <param name="def">The definition.</param>
        /// <param name="rootIndex">Index of the root.</param>
        public void SetContext(DatabaseAudio def, RootAudioIndexInfo rootIndex)
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
        public int CompareIndex(int index, AudioIndexInfo keys)
        {
            return index < IndexCount ? IndexEntries[index].CompareTo(keys) : 1;
        }

        /// <summary>
        /// Attempts to get the index leaf information for the index entry at
        /// the specified ordinal.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="leaf">An output parameter containing the leaf info.</param>
        /// <returns></returns>
        public bool TryGetIndexEntryLeafInfo(int index, out AudioIndexLeafInfo leaf)
        {
            if (Depth == 0)
            {
                leaf = (AudioIndexLeafInfo) IndexEntries[index];
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
                logicalPageId = ((AudioIndexLogicalInfo) IndexEntries[index]).LogicalPageId;
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
        protected override AudioIndexInfo CreateLinkToPage(
            IndexPage<AudioIndexInfo, RootAudioIndexInfo> page)
        {
            var sample = page.IndexEntries[0].SampleIndex;
            return new AudioIndexLogicalInfo(sample, page.LogicalPageId);
        }

        /// <summary>
        /// Adds an intermediate link to the given page.
        /// </summary>
        /// <returns></returns>
        protected override AudioIndexInfo CreateIndexEntry()
        {
            if (IsLeafIndex)
            {
                return new AudioIndexLeafInfo();
            }
            else
            {
                // Root and intermediate pages use logical info
                return new AudioIndexLogicalInfo();
            }
        }
        #endregion
    }
}
