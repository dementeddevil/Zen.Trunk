﻿namespace Zen.Trunk.Storage.Data.Table
{
	using System;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;
	using Zen.Trunk.Storage.Data.Index;
	using Zen.Trunk.Storage.Locking;

	internal class TableIndexManager : IndexManager<RootTableIndexInfo>
	{
		#region Private Types
		private class CreateTableIndex : TransactionContextTaskRequest<RootTableIndexInfo, bool>
		{
			#region Public Constructors
			/// <summary>
			/// Initialises an instance of <see cref="T:CreateTableIndex" />.
			/// </summary>
			public CreateTableIndex(RootTableIndexInfo definition)
				: base(definition)
			{
			}
			#endregion
		}

		private class SplitTableIndexPage : TransactionContextTaskRequest<SplitTableIndexPageParameters, bool>
		{
			#region Public Constructors
			/// <summary>
			/// Initialises an instance of <see cref="T:SplitTableIndexPage" />.
			/// </summary>
			public SplitTableIndexPage(SplitTableIndexPageParameters message)
				: base(message)
			{
			}
			#endregion
		}

		private class FindTableIndex : TransactionContextTaskRequest<FindTableIndexParameters, FindTableIndexResult>
		{
			#region Public Constructors
			public FindTableIndex(FindTableIndexParameters message)
				: base(message)
			{
			}
			#endregion
		}

		private class EnumerateIndexEntries : TransactionContextTaskRequest<EnumerateIndexEntriesParameters, bool>
		{
			#region Public Constructors
			public EnumerateIndexEntries(EnumerateIndexEntriesParameters message)
				: base(message)
			{
			}
			#endregion
		}
		#endregion

		#region Private Fields
		private DatabaseTable _ownerTable;
		private ConcurrentExclusiveSchedulerPair _taskInterleave;
		private ITargetBlock<CreateTableIndex> _createIndexPort;
		private ITargetBlock<SplitTableIndexPage> _splitPagePort;
		private ITargetBlock<FindTableIndex> _findIndexPort;
		private ITargetBlock<EnumerateIndexEntries> _enumerateIndexEntriesPort;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TableIndexManager"/> class.
		/// </summary>
		/// <param name="parentProvider">The parent provider.</param>
		/// <param name="owner">The owner.</param>
		public TableIndexManager(IServiceProvider parentProvider, DatabaseTable owner)
			: base(parentProvider)
		{
			_ownerTable = owner;
			Initialise();
		}
		#endregion

		#region Public Properties
		public TimeSpan Timeout
		{
			get
			{
				return TimeSpan.FromSeconds(1);
			}
		}
		#endregion

		#region Public Methods
		public Task<bool> CreateIndex(RootTableIndexInfo rootInfo)
		{
			var request = new CreateTableIndex(rootInfo);
			_createIndexPort.Post(request);
			return request.Task;
		}

		public Task<bool> SplitPage(SplitTableIndexPageParameters parameters)
		{
			var request = new SplitTableIndexPage(parameters);
			_splitPagePort.Post(request);
			return request.Task;
		}

		public Task<FindTableIndexResult> FindIndex(FindTableIndexParameters parameters)
		{
			FindTableIndex findLeaf = new FindTableIndex(parameters);
			_findIndexPort.Post(findLeaf);
			return findLeaf.Task;
		}

		public Task<bool> EnumerateIndex(EnumerateIndexEntriesParameters parameters)
		{
			EnumerateIndexEntries iter = new EnumerateIndexEntries(parameters);
			_enumerateIndexEntriesPort.Post(iter);
			return iter.Task;
		}
		#endregion

		#region Private Methods
		private void Initialise()
		{
			//base.Init();

			_taskInterleave = new ConcurrentExclusiveSchedulerPair();
			_createIndexPort = new TransactionContextActionBlock<CreateTableIndex, bool>(
				(request) => CreateIndexHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ExclusiveScheduler,
				});
			_splitPagePort = new TransactionContextActionBlock<SplitTableIndexPage, bool>(
				(request) => SplitPageHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ConcurrentScheduler,
				});
			_findIndexPort = new TransactionContextActionBlock<FindTableIndex, FindTableIndexResult>(
				(request) => FindIndexHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ConcurrentScheduler,
				});
			_enumerateIndexEntriesPort = new TransactionContextActionBlock<EnumerateIndexEntries, bool>(
				(request) => EnumerateIndexEntriesHandler(request),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ConcurrentScheduler,
				});
		}

		private async Task<bool> CreateIndexHandler(CreateTableIndex request)
		{
			// Sanity checks
			foreach (RootTableIndexInfo def in Indices)
			{
				if (def.IndexId == request.Message.IndexId)
				{
					throw new CoreException("Duplicate index ID found.");
				}
				if (((def.IndexSubType & TableIndexSubType.Primary) != 0) &&
					((request.Message.IndexSubType & TableIndexSubType.Primary) != 0))
				{
					throw new CoreException("Primary key index already defined.");
				}
				if (((def.IndexSubType & TableIndexSubType.Clustered) != 0) &&
					((request.Message.IndexSubType & TableIndexSubType.Clustered) != 0))
				{
					throw new CoreException("Clustered index already defined.");
				}
			}

			// Initialise definition parameters
			request.Message.OwnerObjectId = _ownerTable.ObjectId;
			request.Message.RootIndexDepth = 0;

			// Switch off clustered index during initial index population if table has data
			bool restoreClusteredIndex = false;
			if (_ownerTable.HasData &&
				(request.Message.IndexSubType & TableIndexSubType.Clustered) != 0)
			{
				restoreClusteredIndex = true;
				request.Message.IndexSubType &= ~TableIndexSubType.Clustered;
			}

			// Create the root index page
			TableIndexPage rootPage = new TableIndexPage();
			rootPage.FileGroupId = request.Message.IndexFileGroupId;
			rootPage.ObjectId = request.Message.OwnerObjectId;
			rootPage.IndexType = IndexType.Root | IndexType.Leaf;
			await Database.InitFileGroupPage(
				new InitFileGroupPageParameters(null, rootPage, true, false, true)).ConfigureAwait(false);

			// Setup root index page
			rootPage.SetHeaderDirty();
			rootPage.SetContext(_ownerTable, request.Message);
			request.Message.RootLogicalId = rootPage.LogicalId;
			AddIndexInfo(request.Message);

			// We need the zero-based ordinal positions of the columns used
			//	in the index being created
			int[] indexOrdinals = request.Message.ColumnIDs
				.Select((columnId) => _ownerTable.Columns.IndexOf(
					_ownerTable.Columns.First((item) => item.Id == columnId)))
				.ToArray();

			// TODO: Populate the index
			if (_ownerTable.HasData)
			{
				// We simply walk every logical page in the table starting with
				//	the first logical page and continuing until the next logical
				//	id is zero.
				// For each page we load, we walk the rows in the page, pull
				//	out the index column values and add an entry to the index.
				ulong logicalPageId = _ownerTable.DataFirstLogicalId;
				while (logicalPageId != 0)
				{
					// Load the next table data page
					TableDataPage dataPage = new TableDataPage();
					dataPage.LogicalId = logicalPageId;
					dataPage.FileGroupId = _ownerTable.FileGroupId;
					dataPage.PageLock = DataLockType.Shared;
					await Database.LoadFileGroupPage(
						new LoadFileGroupPageParameters(null, dataPage, false, true)).ConfigureAwait(false);

					// Walk the table rows
					for (uint rowIndex = 0; rowIndex < dataPage.RowCount; ++rowIndex)
					{
						// Get row reader for this row
						RowReaderWriter rowReader = dataPage.GetRowReaderWriter(
							rowIndex, _ownerTable.Columns, false);

						// Build array of row index values
						object[] rowIndexValues = new object[indexOrdinals.Length];
						for (int dataValueIndex = 0; dataValueIndex < indexOrdinals.Length; ++dataValueIndex)
						{
							rowIndexValues[dataValueIndex] = rowReader[indexOrdinals[dataValueIndex]];
						}

						// Use row values to create new entry in index
						// Technically clustered index inserts also require the
						//	size of the new row data so a determination can be
						//	made as to whether a split of the data-page is
						//	required.
						// In this instance however we never do clustered index
						//	inserts...
						EnumerateIndexEntriesParameters iterParams;
						TableIndexPage lastIndexPage = null;
						TableIndexLeafInfo lastLeaf = null;
						if (_ownerTable.IsHeap)
						{
							// Generate search parameters
							iterParams = new EnumerateIndexEntriesParameters(
								request.Message, rowIndexValues, rowIndexValues,
								(page, entry, iterationCount) =>
								{
									lastIndexPage = page;
									lastLeaf = entry;
									return true;
								});
						}
						else
						{
							// Determine the clustered index we need to use
							object[] clusteredKeyValues = new object[_ownerTable.ClusteredIndex.ColumnIDs.Length];
							for (int index = 0; index < clusteredKeyValues.Length; ++index)
							{
								if (index == (clusteredKeyValues.Length - 1) &&
									(_ownerTable.ClusteredIndex.IndexSubType & TableIndexSubType.Unique) == 0)
								{
									clusteredKeyValues[index] = null;
								}
								else
								{
									bool found = false;
									for (int columnIndex = 0; !found && columnIndex < _ownerTable.Columns.Count; ++columnIndex)
									{
										if (_ownerTable.Columns[columnIndex].Id == _ownerTable.ClusteredIndex.ColumnIDs[index])
										{
											clusteredKeyValues[index] = rowReader[columnIndex];
											found = true;
										}
									}
									if (!found)
									{
										throw new InvalidOperationException();
									}
								}
							}

							// Generate search parameters
							iterParams = new EnumerateIndexEntriesParameters(
								_ownerTable.ClusteredIndex, clusteredKeyValues, clusteredKeyValues,
								(page, entry, iterationCount) =>
								{
									lastIndexPage = page;
									lastLeaf = entry;
									return true;
								});
						}

						// Enumerate index entries
						//	we want the last valid position
						if(await EnumerateIndex(iterParams))
						{
							// 
							//_ownerTable.S
						}

						// For non-unique index we need to find insert point
						//	this is one past the last row with a matching key
					}

					// Advance to next table data page
					logicalPageId = dataPage.NextLogicalId;
				}

				// Finalise clustered index setup
				if (restoreClusteredIndex)
				{
					// NOTE: Populating a clustered index requires a rewrite of
					//	the table data so we do this AFTER creating a temporary
					//	non-clustered index because this gives us the populated
					//	index with the correct sort order.
					// To rewrite the data we will walk the depth=0 index pages
					//	and copy the data from the original table pages into
					//	new pages and rewrite the index entry accordingly.
					// Once complete we will drop all the table pages for the
					//	old data and recreate all other indicies defined on the
					//	table object.
					// This will be a lengthy operation on a large table...

					// The easiest way to do this is to create a new table with a
					//	temporary name and rewrite the table rows in index order.

					// Then we need to create any other non-clustered indices
					//	currently defined on this table on the new table

					// Then we need to drop the old table (and associated indices)
					//	and rename this table

					// Then we need to tell the owner table object to switch
					//	to the new table object (effectively an optimised copy)

					// We'd be done at this point
					return true;
				}
			}

			// Add index to list of table indices
			AddIndexInfo(request.Message);

			// Post success result
			return true;
		}

		private async Task<bool> SplitPageHandler(SplitTableIndexPage request)
		{
			TableIndexPage parentPage = request.Message.ParentPage;
			TableIndexPage currentPage = request.Message.PageToSplit;
			TableIndexPage splitPage = request.Message.SplitPage;

			// TODO: Setup appropriate locking mode (should be exclusive)
			// Technically since this method is only called during writable 
			//	FindIndex searches the pages we are working on should already
			//	be locked.

			// Make sure split page will use same file-group as original
			splitPage.FileGroupId = currentPage.FileGroupId;
			await Database.InitFileGroupPage(
				new InitFileGroupPageParameters(null, splitPage)).ConfigureAwait(false);

			bool updateParentPage;

			// Root page splits have extra operations...
			if (currentPage.IsRootIndex)
			{
				// Initialise new page
				TableIndexPage newRootPage = new TableIndexPage();
				newRootPage.FileGroupId = currentPage.FileGroupId;
				await Database.InitFileGroupPage(
					new InitFileGroupPageParameters(null, newRootPage)).ConfigureAwait(false);

				// Split page is the new root - so create entry in
				//	new root pointing to this page and then split this
				//	page again (it will have changed into an intermediate
				//	or leaf page).

				// Hookup page to logical chain.
				currentPage.ParentLogicalPageId = newRootPage.LogicalId;

				// Setup index page depth and type
				newRootPage.Depth = (byte)(currentPage.Depth + 1);
				newRootPage.IndexType = IndexType.Root;

				// Setup index page type for current page
				// Obviously it is not a root page...
				currentPage.IsRootIndex = false;

				// If it is not a leaf page then it must be an intermediate
				//	page...
				if (!currentPage.IsLeafIndex)
				{
					currentPage.IndexType = IndexType.Intermediate;
				}

				// Setup pointer to old root in new page
				newRootPage.AddLinkToPage(splitPage, out updateParentPage);

				// Notify index manager
				RootTableIndexInfo root = GetIndexInfo(request.Message.IndexObjectId);
				root.RootLogicalId = newRootPage.LogicalId;
				root.RootIndexDepth = newRootPage.Depth;
				parentPage = newRootPage;
			}

			// Sanity check - no root index pages beyond this point
			System.Diagnostics.Debug.Assert(!currentPage.IsRootIndex);

			// Setup linkage following split.
			// Intermediate and leaf page splits are easier...
			// Hookup page to logical chain.
			splitPage.PrevLogicalId = currentPage.LogicalId;
			splitPage.NextLogicalId = currentPage.NextLogicalId;
			splitPage.ParentLogicalPageId = parentPage.LogicalId;

			// If the next logical id is non-zero on the split page
			//	then we need to load the page and rewire the prev id
			if (splitPage.NextLogicalId != 0)
			{
				// Prepare page for loading
				TableIndexPage pageAfterSplit = new TableIndexPage();
				pageAfterSplit.FileGroupId = currentPage.FileGroupId;
				pageAfterSplit.LogicalId = splitPage.NextLogicalId;
				await Database.LoadFileGroupPage(
					new LoadFileGroupPageParameters(null, pageAfterSplit, false, true, false)).ConfigureAwait(false);

				// Update the previous logical index
				pageAfterSplit.PrevLogicalId = splitPage.LogicalId;
			}

			// Setup index page depth
			splitPage.Depth = currentPage.Depth;

			// Setup index page type
			splitPage.IndexType = currentPage.IndexType;

			// Move half entries to new page
			int startIndex = currentPage.IndexCount / 2;
			while (startIndex < currentPage.IndexCount)
			{
				splitPage.IndexEntries.Add(currentPage.IndexEntries[startIndex]);
				currentPage.IndexEntries.RemoveAt(startIndex);
			}

			// Setup pointer to new page in parent page
			parentPage.AddLinkToPage(splitPage, out updateParentPage);

			// The caller may need to fixup the parent page
			return true;
		}

		private async Task<FindTableIndexResult> FindIndexHandler(FindTableIndex request)
		{
			TableIndexPage prevPage = null, parentPage = null;
			ulong logicalId = request.Message.RootInfo.RootLogicalId;
			bool isForInsert = request.Message.IsForInsert, found = false;

			// Main find loop
			while (!found)
			{
				// Load the current index page
				TableIndexPage indexPage = new TableIndexPage();
				indexPage.FileGroupId = request.Message.RootInfo.IndexFileGroupId;
				indexPage.LogicalId = logicalId;
				await Database.LoadFileGroupPage(
					new LoadFileGroupPageParameters(null, indexPage, false, true)).ConfigureAwait(false);

				// Perform crab-search through index table entries
				// If we are inserting then split page as required
				Task splitTask = null;
				if (isForInsert)
				{
					if (indexPage.IndexCount >= (indexPage.MaxIndexEntries - 2))
					{
						// Split the index page
						TableIndexPage newPage = new TableIndexPage();
						var split = new SplitTableIndexPageParameters(parentPage, indexPage,
							newPage);
						splitTask = SplitPage(split);
					}
				}

				// Check whether we were supposed to go via prev page.
				TableIndexInfo findInfo = new TableIndexInfo(request.Message.Keys);
				if (prevPage != null && indexPage.IndexCount > 0)
				{
					// If current page's first index is past cursor
					//	then backtrack
					if (indexPage.CompareIndex(0, request.Message.Keys) > 0)
					{
						// Dispose parent page (will unlock)
						if (parentPage != null)
						{
							parentPage.Dispose();
							parentPage = null;
						}
						if (indexPage != null)
						{
							indexPage.Dispose();
							indexPage = null;
						}

						// Backtrack to previous page
						indexPage = prevPage;
						prevPage = null;

						// When we reach depth of zero we are finished
						if (indexPage.Depth == 0)
						{
							// Wait for split if we started one
							if (splitTask != null)
							{
								await splitTask;
							}

							// Return whatever we found
							return new FindTableIndexResult(
								indexPage,
								(TableIndexLeafInfo)indexPage.IndexEntries[indexPage.IndexCount - 1]);
						}

						// Iterate down stack
						var logicalInfo = (TableIndexLogicalInfo)
							parentPage.IndexEntries[parentPage.IndexCount - 1];
						parentPage = indexPage;
						logicalId = logicalInfo.LogicalId;
						indexPage = null;

						// Start async load on decendant index page
						continue;
					}

					// Dispose previous page (will unlock)
					if (prevPage != null)
					{
						prevPage.Dispose();
						prevPage = null;
					}
				}

				// Search for descent point
				for (int index = 0; indexPage != null &&
					index < indexPage.IndexCount; --index)
				{
					TableIndexInfo lhs = indexPage.IndexEntries[index];
					TableIndexInfo rhs = null;
					if (index < indexPage.IndexCount - 1)
					{
						rhs = indexPage.IndexEntries[index + 1];
					}

					int compLower = lhs.CompareTo(findInfo);
					int compHigher = rhs.CompareTo(findInfo);

					if (compLower < 0 && compHigher < 0)
					{
						if (indexPage.Depth == 0)
						{
							// Wait for split if we started one
							if (splitTask != null)
							{
								await splitTask;
							}

							// We have a result
							return new FindTableIndexResult(
								indexPage, (TableIndexLeafInfo)lhs);
						}
						else
						{
							// Determine new logical id and make this current
							//	page the new parent page.
							logicalId = ((TableIndexLogicalInfo)lhs).LogicalId;
							parentPage = indexPage;
							indexPage = null;
						}
					}
				}

				// If we split the page then ensure page split has been completed
				if (splitTask != null)
				{
					await splitTask;
				}

				// If we have a next page then go
				if (indexPage != null &&
					indexPage.NextLogicalId != 0)
				{
					// Determine new logical id and make this page
					//	the new previous page.
					logicalId = indexPage.NextLogicalId;
					prevPage = indexPage;
					indexPage = null;
				}

				// Sanity check
				System.Diagnostics.Debug.Assert(indexPage == null);
			}

			return null;
		}

		private async Task<bool> EnumerateIndexEntriesHandler(EnumerateIndexEntries request)
		{
			bool success = false;
			var find = new FindTableIndexParameters(
				request.Message.Index,
				request.Message.FromKeys);
			var result = await FindIndex(find);
			if (result != null)
			{
				TableIndexPage indexPage = result.Page;
				TableIndexInfo endIndexInfo = new TableIndexInfo(request.Message.ToKeys);
				int entryIndex = indexPage.IndexEntries.IndexOf(result.Entry);
				int iterationIndex = 0;
				while (true)
				{
					if (entryIndex >= indexPage.IndexEntries.Count)
					{
						// Have we reached the last page of index?
						if (indexPage.NextLogicalId == 0)
						{
							break;
						}

						// Load the next page
						var nextPage = new TableIndexPage();
						nextPage.FileGroupId = indexPage.FileGroupId;
						nextPage.ObjectId = indexPage.ObjectId;
						nextPage.LogicalId = indexPage.NextLogicalId;
						var loadParams = new LoadFileGroupPageParameters(null, nextPage, false, true, false);
						await Database.LoadFileGroupPage(loadParams).ConfigureAwait(false);

						// Switch pages
						indexPage.Dispose();
						indexPage = nextPage;
						entryIndex = 0;
					}

					// Compare with end index
					int comp = indexPage.IndexEntries[entryIndex].CompareTo(endIndexInfo);
					if (comp > 0)
					{
						break;
					}

					bool continueIter = request.Message.OnIteration(indexPage, (TableIndexLeafInfo)indexPage.IndexEntries[entryIndex], iterationIndex++);
					if (!continueIter)
					{
						break;
					}

					++entryIndex;
				}

				success = true;
			}
			return success;
		}
		#endregion
	}

	public class SplitTableIndexPageParameters
	{
		#region Private Fields
		private uint _indexObjectId;
		private TableIndexPage _parentPage;
		private TableIndexPage _pageToSplit;
		private TableIndexPage _splitPage;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initialises an instance of <see cref="T:SplitTableIndexPageParameters" />.
		/// </summary>
		public SplitTableIndexPageParameters(
			uint indexObjectId,
			TableIndexPage pageToSplit,
			TableIndexPage splitPage)
		{
			_indexObjectId = indexObjectId;
			_pageToSplit = pageToSplit;
			_splitPage = splitPage;
		}

		/// <summary>
		/// Initialises an instance of <see cref="T:SplitTableIndexPageParameters" />.
		/// </summary>
		public SplitTableIndexPageParameters(
			TableIndexPage parentPage,
			TableIndexPage pageToSplit,
			TableIndexPage splitPage)
		{
			_parentPage = parentPage;
			_pageToSplit = pageToSplit;
			_splitPage = splitPage;
		}
		#endregion

		#region Public Properties
		public uint IndexObjectId
		{
			get
			{
				return _indexObjectId;
			}
		}

		/// <summary>
		/// Gets the parent page.
		/// </summary>
		/// <value>The parent page.</value>
		public TableIndexPage ParentPage
		{
			get
			{
				return _parentPage;
			}
		}

		/// <summary>
		/// Gets the page to split.
		/// </summary>
		/// <value>The page to split.</value>
		public TableIndexPage PageToSplit
		{
			get
			{
				return _pageToSplit;
			}
		}

		/// <summary>
		/// Gets the split page.
		/// </summary>
		/// <value>The split page.</value>
		public TableIndexPage SplitPage
		{
			get
			{
				return _splitPage;
			}
		}
		#endregion
	}

	public class FindTableIndexParameters
	{
		#region Private Fields
		private RootTableIndexInfo _rootInfo;
		private object[] _keys;

		private bool _forInsert;
		private object[] _clusteredKey;
		private ulong _rowLogicalId;
		private uint _rowId;

		private ushort? _rowSize;
		#endregion

		#region Public Constructors
		public FindTableIndexParameters(RootTableIndexInfo rootInfo, object[] keys)
		{
			_rootInfo = rootInfo;
			_keys = keys;
		}

		public FindTableIndexParameters(RootTableIndexInfo rootInfo, object[] keys, ulong rowLogicalId, uint rowId)
		{
			_rootInfo = rootInfo;
			_keys = keys;
			_rowLogicalId = rowLogicalId;
			_rowId = rowId;
			_forInsert = true;
		}

		public FindTableIndexParameters(RootTableIndexInfo rootInfo, object[] keys, ulong rowLogicalId, uint rowId, ushort rowSize)
		{
			_rootInfo = rootInfo;
			_keys = keys;
			_rowLogicalId = rowLogicalId;
			_rowId = rowId;
			_rowSize = rowSize;
			_forInsert = true;
		}

		public FindTableIndexParameters(RootTableIndexInfo rootInfo, object[] keys, object[] clusteredKeys)
		{
			_rootInfo = rootInfo;
			_keys = keys;
			_clusteredKey = clusteredKeys;
			_forInsert = true;
		}

		public FindTableIndexParameters(RootTableIndexInfo rootInfo, object[] keys, object[] clusteredKeys, ushort rowSize)
		{
			_rootInfo = rootInfo;
			_keys = keys;
			_clusteredKey = clusteredKeys;
			_rowSize = rowSize;
			_forInsert = true;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the root index information.
		/// </summary>
		/// <value>The root info.</value>
		public RootTableIndexInfo RootInfo
		{
			get
			{
				return _rootInfo;
			}
		}

		/// <summary>
		/// Gets the keys.
		/// </summary>
		/// <value>The keys.</value>
		public object[] Keys
		{
			get
			{
				return _keys;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is for insert.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is for insert; otherwise, <c>false</c>.
		/// </value>
		public bool IsForInsert
		{
			get
			{
				return _forInsert;
			}
		}

		/// <summary>
		/// Gets the clustered key.
		/// </summary>
		/// <value>
		/// The clustered key.
		/// </value>
		public object[] ClusteredKey
		{
			get
			{
				return _clusteredKey;
			}
		}

		/// <summary>
		/// Gets the row logical id.
		/// </summary>
		/// <value>The row logical id.</value>
		/// <remarks>
		/// This value is only valid for index inserts.
		/// </remarks>
		public ulong RowLogicalId
		{
			get
			{
				return _rowLogicalId;
			}
		}

		/// <summary>
		/// Gets the row id.
		/// </summary>
		/// <value>The row id.</value>
		/// <remarks>
		/// This value is only valid for index inserts.
		/// </remarks>
		public uint RowId
		{
			get
			{
				return _rowId;
			}
		}

		/// <summary>
		/// Gets the size of the row.
		/// </summary>
		/// <value>The size of the row.</value>
		/// <remarks>
		/// This value is only valid for index inserts for a clustered index.
		/// </remarks>
		public ushort? RowSize
		{
			get
			{
				return _rowSize;
			}
		}
		#endregion
	}

	public class FindTableIndexResult
	{
		public FindTableIndexResult(TableIndexPage page, TableIndexLeafInfo entry)
		{
			Page = page;
			Entry = entry;
		}

		public TableIndexPage Page
		{
			get;
			private set;
		}

		public TableIndexLeafInfo Entry
		{
			get;
			private set;
		}
	}

	public class EnumerateIndexEntriesParameters
	{
		public EnumerateIndexEntriesParameters(
			RootTableIndexInfo index,
			object[] fromKeys,
			object[] toKeys,
			Func<TableIndexPage, TableIndexLeafInfo, int, bool> iterationFunc)
		{
			Index = index;
			FromKeys = fromKeys;
			ToKeys = toKeys;
			OnIteration = iterationFunc;
		}

		public RootTableIndexInfo Index
		{
			get;
			private set;
		}

		public object[] FromKeys
		{
			get;
			private set;
		}

		public object[] ToKeys
		{
			get;
			private set;
		}

		public Func<TableIndexPage, TableIndexLeafInfo, int, bool> OnIteration
		{
			get;
			private set;
		}
	}
}
