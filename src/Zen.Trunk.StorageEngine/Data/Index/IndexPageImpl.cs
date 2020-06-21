using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data.Index
{
    /// <summary>
    /// Abstract class containing the generic implementation for index page
    /// splitting and merging.
    /// </summary>
    /// <typeparam name="TIndexClass"></typeparam>
    /// <typeparam name="TIndexRootClass"></typeparam>
    public abstract class IndexPage<TIndexClass, TIndexRootClass> : IndexPage
		where TIndexClass : IndexInfo
		where TIndexRootClass : RootIndexInfo
	{
		#region Private Fields
		private readonly BufferFieldUInt16 _indexEntryCount;
		private readonly List<TIndexClass> _indexEntries;
		private bool _lastInternalLockWritable;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexPage&lt;IndexClass, IndexRootClass&gt;"/> class.
		/// </summary>
		protected IndexPage()
		{
			_indexEntryCount = new BufferFieldUInt16(base.LastHeaderField);
			_indexEntries = new List<TIndexClass>();
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
		internal List<TIndexClass> IndexEntries => _indexEntries;
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
		/// <returns>
		/// <c>true</c> if the caller must update the parent page; otherwise, <c>false</c>.
		/// </returns>
		public bool AddLinkToPage(IndexPage<TIndexClass, TIndexRootClass> page)
		{
			var info = CreateLinkToPage(page);
			return AddLinkToPage(info);
		}

		/// <summary>
		/// Adds the given link (to a child page) to this page's index entry list.
		/// </summary>
		/// <param name="link">The link.</param>
		/// <returns>
		/// <c>true</c> if the caller must update the parent page; otherwise, <c>false</c>.
		/// </returns>
		public bool AddLinkToPage(TIndexClass link)
		{
			// Special case for first entry in this page.
			if (IndexCount == 0)
			{
				IndexEntries.Add(link);
				return true;
			}

			// Everything else must use positioned insert.
			var isAdded = false;
			var updateParentPage = false;
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

			return updateParentPage;
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Adds an intermediate link to the given page.
		/// </summary>
		protected abstract TIndexClass CreateIndexEntry();

		/// <summary>
		/// Overridden in derived classes to provide the intermediate index
		/// information that is needed to store a link to the specified page
		/// within an intermediate index page.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		protected abstract TIndexClass CreateLinkToPage(IndexPage<TIndexClass, TIndexRootClass> page);

		/// <summary>
		/// Overridden. Writes the page header block to the specified buffer writer.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected override void WriteHeader(SwitchingBinaryWriter streamManager)
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
		protected override void ReadData(SwitchingBinaryReader streamManager)
		{
			_indexEntries.Clear();
			for (ushort index = 0; index < _indexEntryCount.Value; ++index)
			{
				var info = CreateIndexEntry();
				info.Read(streamManager);
				_indexEntries.Add(info);
			}
		}

		/// <summary>
		/// Overridden. Writes the page data block to the specified buffer 
		/// writer.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected override void WriteData(SwitchingBinaryWriter streamManager)
		{
			for (ushort index = 0; index < _indexEntryCount.Value; ++index)
			{
				var info = IndexEntries[index];
				info.Write(streamManager);
			}
		}

		/// <summary>
		/// Called when [lock page].
		/// </summary>
		/// <param name="lockManager">The lockManager.</param>
		protected override async Task OnLockPageAsync(IDatabaseLockManager lockManager)
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
					lockManager.LockRootIndex(ObjectId, IndexId, _lastInternalLockWritable, LockTimeout);
					break;

				case IndexType.Intermediate:
					_lastInternalLockWritable = false;
					if (PageLock == DataLockType.Exclusive)
					{
						_lastInternalLockWritable = true;
					}
					lockManager.LockInternalIndex(ObjectId, IndexId, LogicalPageId, _lastInternalLockWritable, LockTimeout);
					break;

				case IndexType.Leaf:
					await lockManager.LockDataAsync(ObjectId, LogicalPageId, PageLock, LockTimeout).ConfigureAwait(false);
					break;
			}
		}

		/// <summary>
		/// Called when [unlock page].
		/// </summary>
		/// <param name="lockManager">The lockManager.</param>
		protected override async Task OnUnlockPageAsync(IDatabaseLockManager lockManager)
		{
			switch (IndexType)
			{
				case IndexType.Root:
					lockManager.UnlockRootIndex(ObjectId, IndexId, _lastInternalLockWritable);
					break;

				case IndexType.Intermediate:
					lockManager.UnlockInternalIndex(ObjectId, IndexId, LogicalPageId, _lastInternalLockWritable);
					break;

				case IndexType.Leaf:
					await lockManager.UnlockDataAsync(ObjectId, LogicalPageId).ConfigureAwait(false);
					break;
			}
		}
		#endregion
	}
}
