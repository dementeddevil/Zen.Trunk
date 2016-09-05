namespace Zen.Trunk.Storage.Data.Index
{
	using System;
	using System.Collections.Generic;
	using IO;
	using Locking;

	/// <summary>
	/// Abstract class containing the generic implementation for index page
	/// splitting.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <remarks>
	/// Obviously this class will also contain the generic index page merging
	/// code too...
	/// </remarks>
	public abstract class IndexPage<IndexClass, IndexRootClass> : IndexPage
		where IndexClass : IndexInfo
		where IndexRootClass : RootIndexInfo
	{
		#region Private Fields
		private readonly BufferFieldUInt16 _indexEntryCount;
		private readonly List<IndexClass> _indexEntries;
		private bool _lastInternalLockWritable;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexPage&lt;IndexClass, IndexRootClass&gt;"/> class.
		/// </summary>
		public IndexPage()
		{
			_indexEntryCount = new BufferFieldUInt16(base.LastHeaderField);
			_indexEntries = new List<IndexClass>(MaxIndexEntries);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the count of index entries in this page.
		/// </summary>
		/// <value>The index count.</value>
		public ushort IndexCount => (ushort)_indexEntries.Count;

	    /// <summary>
		/// Overridden. Gets the minimum required header space.
		/// </summary>
		/// <value></value>
		public override uint MinHeaderSize => base.MinHeaderSize + 2;
	    #endregion

		#region Internal Properties
		/// <summary>
		/// Gets the index entries.
		/// </summary>
		/// <value>The index entries.</value>
		internal List<IndexClass> IndexEntries => _indexEntries;
	    #endregion

		#region Protected Properties
		/// <summary>
		/// Overridden. Gets the last header field.
		/// </summary>
		/// <value>The last header field.</value>
		protected override BufferField LastHeaderField => _indexEntryCount;

	    #endregion

		#region Public Methods
		/// <summary>
		/// Adds the given (child) page to this page's index entry list.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="updateParentPage">
		/// if set to <c>true</c> then the caller must update the parent page.
		/// </param>
		public void AddLinkToPage(IndexPage<IndexClass, IndexRootClass> page, out bool updateParentPage)
		{
			var info = CreateLinkToPage(page);
			AddLinkToPage(info, out updateParentPage);
		}

		/// <summary>
		/// Adds the given link (to a child page) to this page's index entry list.
		/// </summary>
		/// <param name="link">The link.</param>
		/// <param name="updateParentPage">
		/// if set to <c>true</c> then the caller must update the parent page.
		/// </param>
		public void AddLinkToPage(IndexClass link, out bool updateParentPage)
		{
			// Special case for first entry in this page.
			updateParentPage = false;
			if (IndexCount == 0)
			{
				IndexEntries.Add(link);
				updateParentPage = true;
				return;
			}

			// Everything else must use positioned insert.
			var isAdded = false;
			for (ushort index = 0; !isAdded && index < (IndexCount + 1); ++index)
			{
				var sort = 0;
				var isFinal = false;
				if (index < IndexCount)
				{
					sort = link.CompareTo(IndexEntries[index]);
				}
				else
				{
					isFinal = true;
				}

				if (sort <= 0 || isFinal)
				{
					if (isFinal)
					{
						IndexEntries.Add(link);
					}
					else
					{
						IndexEntries.Insert(index, link);
					}

					if (index == 0)
					{
						updateParentPage = true;
					}
					isAdded = true;
				}
			}
			if (!isAdded)
			{
				throw new InvalidOperationException("Failed to add page link!");
			}
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Adds an intermediate link to the given page.
		/// </summary>
		/// <param name="page"></param>
		protected abstract IndexClass CreateIndexEntry();

		/// <summary>
		/// Overridden in derived classes to provide the intermediate index
		/// information that is needed to store a link to the specified page
		/// within an intermediate index page.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		protected abstract IndexClass CreateLinkToPage(IndexPage<IndexClass, IndexRootClass> page);

		/// <summary>
		/// Overridden. Writes the page header block to the specified buffer writer.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected override void WriteHeader(BufferReaderWriter streamManager)
		{
			// Save the current index entry count into the header field.
			_indexEntryCount.Value = (ushort)_indexEntries.Count;

			// Write headers via base class
			base.WriteHeader(streamManager);
		}

		/// <summary>
		/// Overridden. Reads the page data block from the specified buffer
		/// reader.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected override void ReadData(BufferReaderWriter streamManager)
		{
			_indexEntries.Clear();
			for (ushort index = 0; index < _indexEntryCount.Value; ++index)
			{
				IndexInfo info = CreateIndexEntry();
				info.Read(streamManager);
				_indexEntries.Add((IndexClass)info);
			}
		}

		/// <summary>
		/// Overridden. Writes the page data block to the specified buffer 
		/// writer.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected override void WriteData(BufferReaderWriter streamManager)
		{
			for (ushort index = 0; index < _indexEntryCount.Value; ++index)
			{
				IndexInfo info = IndexEntries[index];
				info.Write(streamManager);
			}
		}

		/// <summary>
		/// Called when [lock page].
		/// </summary>
		/// <param name="lm">The lm.</param>
		protected override void OnLockPage(IDatabaseLockManager lm)
		{
			// Perform base class locking first
			switch (IndexType)
			{
				case IndexType.Root:
					_lastInternalLockWritable = false;
					if (PageLock == DataLockType.Exclusive)
					{
						_lastInternalLockWritable = true;
					}
					lm.LockRootIndex(ObjectId, IndexId, _lastInternalLockWritable, LockTimeout);
					break;

				case IndexType.Intermediate:
					_lastInternalLockWritable = false;
					if (PageLock == DataLockType.Exclusive)
					{
						_lastInternalLockWritable = true;
					}
					lm.LockInternalIndex(ObjectId, IndexId, LogicalId, _lastInternalLockWritable, LockTimeout);
					break;

				case IndexType.Leaf:
					lm.LockData(ObjectId, LogicalId, PageLock, LockTimeout);
					break;
			}
		}

		/// <summary>
		/// Called when [unlock page].
		/// </summary>
		/// <param name="lm">The lm.</param>
		protected override void OnUnlockPage(IDatabaseLockManager lm)
		{
			switch (IndexType)
			{
				case IndexType.Root:
					lm.UnlockRootIndex(ObjectId, IndexId, _lastInternalLockWritable);
					break;

				case IndexType.Intermediate:
					lm.UnlockInternalIndex(ObjectId, IndexId, LogicalId, _lastInternalLockWritable);
					break;

				case IndexType.Leaf:
					lm.UnlockData(ObjectId, LogicalId);
					break;
			}
		}
		#endregion
	}
}
