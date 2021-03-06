﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Autofac;
using Zen.Trunk.Storage.Data.Index;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data.Table
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="IndexManager{RootTableIndexInfo}" />
    internal class TableIndexManager : IndexManager<RootTableIndexInfo>
    {
        #region Private Types
        private class CreateTableIndexRequest : TransactionContextTaskRequest<CreateTableIndexParameters, IndexId>
        {
            #region Public Constructors
            /// <summary>
            /// Initialises an instance of <see cref="T:CreateTableIndex" />.
            /// </summary>
            /// <param name="parameters">The parameters.</param>
            public CreateTableIndexRequest(CreateTableIndexParameters parameters)
                : base(parameters)
            {
            }
            #endregion
        }

        private class SplitTableIndexPageRequest : TransactionContextTaskRequest<SplitTableIndexPageParameters, bool>
        {
            #region Public Constructors
            /// <summary>
            /// Initialises an instance of <see cref="T:SplitTableIndexPage" />.
            /// </summary>
            public SplitTableIndexPageRequest(SplitTableIndexPageParameters message)
                : base(message)
            {
            }
            #endregion
        }

        private class MergeTableIndexPagesRequest : TransactionContextTaskRequest<MergeTableIndexPageParameters, bool>
        {
            #region Public Constructors
            public MergeTableIndexPagesRequest(MergeTableIndexPageParameters parameters)
                : base(parameters)
            {
            }
            #endregion
        }

        private class FindTableIndexRequest : TransactionContextTaskRequest<FindTableIndexParameters, FindTableIndexResult>
        {
            #region Public Constructors
            public FindTableIndexRequest(FindTableIndexParameters message)
                : base(message)
            {
            }
            #endregion
        }

        private class EnumerateIndexEntriesRequest : TransactionContextTaskRequest<EnumerateIndexEntriesParameters, bool>
        {
            #region Public Constructors
            public EnumerateIndexEntriesRequest(EnumerateIndexEntriesParameters message)
                : base(message)
            {
            }
            #endregion
        }
        #endregion

        #region Private Fields
        private readonly DatabaseTable _ownerTable;
        private readonly ITargetBlock<CreateTableIndexRequest> _createIndexPort;
        private readonly ITargetBlock<SplitTableIndexPageRequest> _splitPagePort;
        private readonly ITargetBlock<MergeTableIndexPagesRequest> _mergePagesPort;
        private readonly ITargetBlock<FindTableIndexRequest> _findIndexPort;
        private readonly ITargetBlock<EnumerateIndexEntriesRequest> _enumerateIndexEntriesPort;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TableIndexManager"/> class.
        /// </summary>
        /// <param name="parentLifetimeScope">The parent provider.</param>
        public TableIndexManager(ILifetimeScope parentLifetimeScope)
            : base(parentLifetimeScope)
        {
            _ownerTable = parentLifetimeScope.Resolve<DatabaseTable>();
            var taskInterleave = new ConcurrentExclusiveSchedulerPair();
            _createIndexPort = new TransactionContextActionBlock<CreateTableIndexRequest, IndexId>(
                request => CreateIndexHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler,
                });
            _splitPagePort = new TransactionContextActionBlock<SplitTableIndexPageRequest, bool>(
                request => SplitPageHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler,
                });
            _mergePagesPort = new TransactionContextActionBlock<MergeTableIndexPagesRequest, bool>(
                request => MergePagesHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler,
                });
            _findIndexPort = new TransactionContextActionBlock<FindTableIndexRequest, FindTableIndexResult>(
                request => FindIndexHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler,
                });
            _enumerateIndexEntriesPort = new TransactionContextActionBlock<EnumerateIndexEntriesRequest, bool>(
                request => EnumerateIndexEntriesHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler,
                });
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the timeout.
        /// </summary>
        /// <value>
        /// The timeout.
        /// </value>
        public TimeSpan Timeout => TimeSpan.FromSeconds(1);
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates a table index.
        /// </summary>
        /// <param name="parameters">The table index parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task<IndexId> CreateIndexAsync(CreateTableIndexParameters parameters)
        {
            var request = new CreateTableIndexRequest(parameters);
            _createIndexPort.Post(request);
            return request.Task;
        }

        /// <summary>
        /// Splits an index page into two pages.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task<bool> SplitPageAsync(SplitTableIndexPageParameters parameters)
        {
            var request = new SplitTableIndexPageRequest(parameters);
            _splitPagePort.Post(request);
            return request.Task;
        }

        /// <summary>
        /// Merges the two index pages together.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task<bool> MergePagesAsync(MergeTableIndexPageParameters parameters)
        {
            var request = new MergeTableIndexPagesRequest(parameters);
            _mergePagesPort.Post(request);
            return request.Task;
        }

        /// <summary>
        /// Finds the index matching the find parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task<FindTableIndexResult> FindIndexAsync(FindTableIndexParameters parameters)
        {
            var findLeaf = new FindTableIndexRequest(parameters);
            _findIndexPort.Post(findLeaf);
            return findLeaf.Task;
        }

        /// <summary>
        /// Enumerates the index entries.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task<bool> EnumerateIndexAsync(EnumerateIndexEntriesParameters parameters)
        {
            var iter = new EnumerateIndexEntriesRequest(parameters);
            _enumerateIndexEntriesPort.Post(iter);
            return iter.Task;
        }
        #endregion

        #region Private Methods
        private async Task<IndexId> CreateIndexHandlerAsync(CreateTableIndexRequest request)
        {
            // Perform sanity checks and determine index id
            var indexId = new IndexId(1);
            foreach (var def in Indices)
            {
                // Set index id to high-water mark of index identifiers
                indexId = new IndexId(Math.Max(indexId.Value, def.IndexId.Value + 1));

                // Cannot have more than one primary index
                if (((def.IndexSubType & TableIndexSubType.Primary) != 0) &&
                    ((request.Message.IndexSubType & TableIndexSubType.Primary) != 0))
                {
                    throw new CoreException("Primary key index already defined.");
                }

                // Cannot have more than one clustered index
                if (((def.IndexSubType & TableIndexSubType.Clustered) != 0) &&
                    ((request.Message.IndexSubType & TableIndexSubType.Clustered) != 0))
                {
                    throw new CoreException("Clustered index already defined.");
                }
            }

            // Create root table index information
            var rootTableIndexInfo =
                new RootTableIndexInfo(indexId)
                {
                    Name = request.Message.Name,
                    IndexFileGroupId = request.Message.IndexFileGroupId,
                    IndexSubType = request.Message.IndexSubType,
                    ObjectId = _ownerTable.ObjectId,
                    ColumnIDs = request.Message.Members.Select(t => (byte)t.Item1).ToArray(),
                    ColumnDirections = request.Message.Members.Select(t => t.Item2).ToArray()
                };

            // Switch off clustered index during initial index population if table has data
            var restoreClusteredIndex = false;
            if (_ownerTable.HasData &&
                (request.Message.IndexSubType & TableIndexSubType.Clustered) != 0)
            {
                restoreClusteredIndex = true;
                rootTableIndexInfo.IndexSubType &= ~TableIndexSubType.Clustered;
            }

            // Create the root index page
            using (var rootPage =
                new TableIndexPage
                {
                    FileGroupId = rootTableIndexInfo.IndexFileGroupId,
                    ObjectId = rootTableIndexInfo.ObjectId,
                    IndexId = indexId,
                    IndexType = IndexType.Root | IndexType.Leaf
                })
            {
                await Database
                    .InitFileGroupPageAsync(
                        new InitFileGroupPageParameters(
                            null, rootPage, true, false, true))
                    .ConfigureAwait(false);

                // Setup root index page
                rootPage.SetHeaderDirty();
                rootPage.SetContext(_ownerTable, rootTableIndexInfo);
                rootTableIndexInfo.RootLogicalPageId = rootPage.LogicalPageId;
                AddIndexInfo(rootTableIndexInfo);

                // We need the zero-based ordinal positions of the columns used
                //	in the index being created
                var indexOrdinals = rootTableIndexInfo.ColumnIDs
                    .Select(columnId => _ownerTable.Columns.IndexOf(
                        _ownerTable.Columns.First(item => item.Id == columnId)))
                    .ToArray();

                // TODO: Populate the index
                if (_ownerTable.HasData)
                {
                    // We simply walk every logical page in the table starting with
                    //	the first logical page and continuing until the next logical
                    //	id is zero.
                    // For each page we load, we walk the rows in the page, pull
                    //	out the index column values and add an entry to the index.
                    var logicalPageId = _ownerTable.DataFirstLogicalPageId;
                    while (logicalPageId != LogicalPageId.Zero)
                    {
                        // Load the next table data page
                        using (var dataPage =
                            new TableDataPage
                            {
                                LogicalPageId = logicalPageId,
                                FileGroupId = _ownerTable.FileGroupId,
                            })
                        {
                            await dataPage.SetPageLockAsync(DataLockType.Shared).ConfigureAwait(false);

                            await Database
                                .LoadFileGroupPageAsync(
                                    new LoadFileGroupPageParameters(
                                        null, dataPage, false, true))
                                .ConfigureAwait(false);

                            // Walk the table rows
                            for (uint rowIndex = 0; rowIndex < dataPage.RowCount; ++rowIndex)
                            {
                                // Get row reader for this row
                                var rowReader = dataPage.GetRowReader(
                                    rowIndex, _ownerTable.Columns);

                                // Build array of row index values
                                var rowIndexValues = new object[indexOrdinals.Length];
                                for (var dataValueIndex = 0; dataValueIndex < indexOrdinals.Length; ++dataValueIndex)
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
                                        rootTableIndexInfo, rowIndexValues, rowIndexValues,
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
                                    var clusteredKeyValues = new object[_ownerTable.ClusteredIndex.ColumnIDs.Length];
                                    for (var index = 0; index < clusteredKeyValues.Length; ++index)
                                    {
                                        if (index == (clusteredKeyValues.Length - 1) &&
                                            (_ownerTable.ClusteredIndex.IndexSubType & TableIndexSubType.Unique) == 0)
                                        {
                                            clusteredKeyValues[index] = null;
                                        }
                                        else
                                        {
                                            var found = false;
                                            for (var columnIndex = 0; !found && columnIndex < _ownerTable.Columns.Count; ++columnIndex)
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
                                if (await EnumerateIndexAsync(iterParams))
                                {
                                    // 
                                    //_ownerTable.S
                                }

                                // For non-unique index we need to find insert point
                                //	this is one past the last row with a matching key
                            }

                            // Advance to next table data page
                            logicalPageId = dataPage.NextLogicalPageId;
                        }
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
                        return indexId;
                    }
                }
            }

            // Add index to list of table indices
            //AddIndexInfo(rootTableIndexInfo);

            // Post success result
            return indexId;
        }

        private async Task<bool> SplitPageHandlerAsync(SplitTableIndexPageRequest request)
        {
            var currentPage = request.Message.PageToSplit;
            var splitPage = request.Message.SplitPage;

            // TODO: Setup appropriate locking mode (should be exclusive)
            // Technically since this method is only called during writable 
            //	FindIndex searches the pages we are working on should already
            //	be locked.

            // Make sure split page will use same identifiers as original
            splitPage.FileGroupId = currentPage.FileGroupId;
            splitPage.ObjectId = currentPage.ObjectId;
            splitPage.IndexId = currentPage.IndexId;
            await Database
                .InitFileGroupPageAsync(
                    new InitFileGroupPageParameters(null, splitPage))
                .ConfigureAwait(false);

            // Root page splits have extra operations...
            var parentPage = await SplitRootIfNecessaryAndReturnParentAsync(currentPage)
                .ConfigureAwait(false);

            // Sanity check - no root index pages beyond this point
            //Debug.Assert(!currentPage.IsRootIndex);
            //Debug.Assert(parentPage != null);

            // Setup linkage following split (double linked list)
            splitPage.PrevLogicalPageId = currentPage.LogicalPageId;
            splitPage.NextLogicalPageId = currentPage.NextLogicalPageId;
            currentPage.NextLogicalPageId = splitPage.LogicalPageId;
            splitPage.ParentLogicalPageId = parentPage.LogicalPageId;

            // Copy across page state information
            splitPage.IndexType = currentPage.IndexType;
            splitPage.Depth = currentPage.Depth;

            // If the next logical id is non-zero on the split page
            //	then we need to load the page and rewire the prev id
            if (splitPage.NextLogicalPageId != LogicalPageId.Zero)
            {
                // Prepare page for loading
                var pageAfterSplit =
                    new TableIndexPage
                    {
                        FileGroupId = currentPage.FileGroupId,
                        LogicalPageId = splitPage.NextLogicalPageId
                    };
                await Database
                    .LoadFileGroupPageAsync(
                        new LoadFileGroupPageParameters(
                            null, pageAfterSplit, false, true))
                    .ConfigureAwait(false);

                // Update the previous logical index
                pageAfterSplit.PrevLogicalPageId = splitPage.LogicalPageId;
            }

            // Move half entries to new page
            var startIndex = currentPage.IndexCount / 2;
            while (startIndex < currentPage.IndexCount)
            {
                splitPage.IndexEntries.Add(currentPage.IndexEntries[startIndex]);
                currentPage.IndexEntries.RemoveAt(startIndex);
            }

            // Setup pointer to new page in parent page
            parentPage.AddLinkToPage(splitPage);
            return true;
        }

        private async Task<TableIndexPage> SplitRootIfNecessaryAndReturnParentAsync(TableIndexPage currentPage)
        {
            TableIndexPage parentPage;

            if (!currentPage.IsRootIndex)
            {
                // Current page is non-root page; just load the parent page
                parentPage =
                    new TableIndexPage
                    {
                        FileGroupId = currentPage.FileGroupId,
                        ObjectId = currentPage.ObjectId,
                        IndexId = currentPage.IndexId,
                        LogicalPageId = currentPage.LogicalPageId
                    };
                await Database
                    .LoadFileGroupPageAsync(new LoadFileGroupPageParameters(null, parentPage, false, true))
                    .ConfigureAwait(false);
            }
            else
            {
                // Initialise new page (it will become the new root)
                var newRootPage =
                    new TableIndexPage
                    {
                        FileGroupId = currentPage.FileGroupId,
                        ObjectId = currentPage.ObjectId,
                        IndexId = currentPage.IndexId
                    };
                await Database
                    .InitFileGroupPageAsync(new InitFileGroupPageParameters(null, newRootPage))
                    .ConfigureAwait(false);

                // Update parent/child relationship and update state of pages
                currentPage.ParentLogicalPageId = newRootPage.LogicalPageId;
                currentPage.IsRootIndex = false;
                newRootPage.Depth = (byte) (currentPage.Depth + 1);
                newRootPage.IndexType = IndexType.Root;
                if (!currentPage.IsLeafIndex)
                {
                    currentPage.IndexType = IndexType.Intermediate;
                }

                // Ensure new root page has linkage to current page
                newRootPage.AddLinkToPage(currentPage);

                // Notify index manager
                var root = GetIndexInfo(currentPage.IndexId);
                root.RootLogicalPageId = newRootPage.LogicalPageId;
                root.RootIndexDepth = newRootPage.Depth;
                parentPage = newRootPage;
            }

            return parentPage;
        }

        private async Task<bool> MergePagesHandlerAsync(MergeTableIndexPagesRequest request)
        {
            var parentPage = request.Message.ParentPage;
            var primaryPage = request.Message.PrimaryPage;
            var mergePage = request.Message.PageToBeMerged;

            // TODO: Setup appropriate locking mode (should be exclusive)

            // Move all index from merge page to primary page
            primaryPage.IndexEntries.AddRange(mergePage.IndexEntries);

            // Update page linkage
            primaryPage.NextLogicalPageId = mergePage.NextLogicalPageId;

            // If we have a parent page then update all references to merge page
            if (parentPage != null)
            {
                var entriesToUpdate = parentPage
                    .IndexEntries
                    .Cast<TableIndexLogicalInfo>()
                    .Where(i => i.LogicalPageId == mergePage.LogicalPageId);
                foreach (var entry in entriesToUpdate)
                {
                    entry.LogicalPageId = primaryPage.LogicalPageId;
                }
            }

            // Free the merged page
            await Database
                .DeallocateFileGroupPageAsync(
                    new DeallocateFileGroupDataPageParameters(
                        string.Empty, mergePage))
                .ConfigureAwait(false);

            // TODO: If the parent page is the root page and this is the last
            //  child page then this page becomes the new root page
            if (parentPage != null &&
                parentPage.IndexType == IndexType.Root &&
                primaryPage.PrevLogicalPageId == LogicalPageId.Zero &&
                primaryPage.NextLogicalPageId == LogicalPageId.Zero)
            {
                // Setup the index type (add root to primary page)
                primaryPage.IndexType |= IndexType.Root;

                // TODO: Update the root index information with logical id
                //  of the primary index page.
            }

            return true;
        }

        private async Task<FindTableIndexResult> FindIndexHandlerAsync(FindTableIndexRequest request)
        {
            TableIndexPage prevPage = null, parentPage = null;
            var logicalId = request.Message.RootInfo.RootLogicalPageId;
            var isForInsert = request.Message.IsForInsert;
            var findInfo = new TableIndexInfo(request.Message.Keys);

            // Main find loop
            while (true)
            {
                // Load the current index page
                var indexPage = 
                    new TableIndexPage
                    {
                        FileGroupId = request.Message.RootInfo.IndexFileGroupId,
                        LogicalPageId = logicalId
                    };
                await Database
                    .LoadFileGroupPageAsync(
                        new LoadFileGroupPageParameters(
                            null, indexPage, false, true))
                    .ConfigureAwait(false);

                // Perform crab-search through index table entries and split
                //  as necessary
                if (isForInsert && indexPage.IndexCount >= (indexPage.MaxIndexEntries - 2))
                {
                    var newPage = new TableIndexPage();
                    var split = new SplitTableIndexPageParameters(
                        parentPage, indexPage, newPage);
                    var updateParentPageAfterSplit = await SplitPageAsync(split)
                        .ConfigureAwait(false);
                    if (updateParentPageAfterSplit && parentPage != null)
                    {

                    }
                }

                // Check whether we were supposed to go via prev page.
                if (prevPage != null && indexPage.IndexCount > 0)
                {
                    // If current page's first index is past cursor
                    //	then backtrack
                    if (indexPage.CompareIndex(0, findInfo) > 0)
                    {
                        // Dispose parent page (will unlock)
                        if (parentPage != null)
                        {
                            parentPage.Dispose();
                            parentPage = null;
                        }

                        // Dispose of index page (will unlock)
                        indexPage.Dispose();

                        // Backtrack to previous page
                        indexPage = prevPage;
                        prevPage = null;

                        // When we reach depth of zero we are finished
                        if (indexPage.Depth == 0)
                        {
                            // Return whatever we found
                            return new FindTableIndexResult(
                                indexPage,
                                (TableIndexLeafInfo)indexPage.IndexEntries[indexPage.IndexCount - 1]);
                        }

                        // Iterate down stack
                        var logicalInfo = (TableIndexLogicalInfo)
                            indexPage.IndexEntries[indexPage.IndexCount - 1];
                        parentPage = indexPage;
                        logicalId = logicalInfo.LogicalPageId;
                        continue;
                    }

                    // Dispose previous page (will unlock)
                    prevPage.Dispose();
                    prevPage = null;
                }

                // Search for descent point
                for (var index = 0;
                    indexPage != null && index < indexPage.IndexCount;
                    ++index)
                {
                    // Is this the descent point for the current index page
                    var compLower = indexPage.CompareIndex(index, findInfo);
                    var compHigher = indexPage.CompareIndex(index + 1, findInfo);
                    if (compLower >= 0 || compHigher >= 0) continue;

                    // If this is a leaf index page then we are finished
                    if (indexPage.TryGetIndexEntryLeafInfo(index, out var leaf))
                    {
                        return new FindTableIndexResult(indexPage, leaf);
                    }

                    // Determine new logical id of child page and descend
                    indexPage.TryGetIndexEntryLogicalPageId(index, out logicalId);
                    parentPage = indexPage;

                    // Cause index scan of this page to terminate
                    indexPage = null;
                }

                // If current index page is still valid and we have a next page then go
                // NOTE: Typically this should never occur but due to page splits we
                //  cannot fully discount this as a possibility...
                if (indexPage != null &&
                    indexPage.NextLogicalPageId != LogicalPageId.Zero)
                {
                    // Determine new logical id and make this page
                    //	the new previous page.
                    logicalId = indexPage.NextLogicalPageId;
                    prevPage = indexPage;
                    indexPage = null;
                }

                // Sanity check
                System.Diagnostics.Debug.Assert(indexPage == null);
            }
        }

        private async Task<bool> EnumerateIndexEntriesHandlerAsync(EnumerateIndexEntriesRequest request)
        {
            var find = new FindTableIndexParameters(
                request.Message.Index,
                request.Message.FromKeys);
            var result = await FindIndexAsync(find).ConfigureAwait(false);
            if (result == null) return false;

            var indexPage = result.Page;
            var endIndexInfo = new TableIndexInfo(request.Message.ToKeys);
            var entryIndex = indexPage.IndexEntries.IndexOf(result.Entry);
            var iterationIndex = 0;
            while (true)
            {
                // Check for reaching end of index entry list
                if (entryIndex >= indexPage.IndexEntries.Count)
                {
                    // Have we reached the last page of index?
                    if (indexPage.NextLogicalPageId == LogicalPageId.Zero)
                    {
                        break;
                    }

                    // Load the next page and switch
                    var nextPage =
                        new TableIndexPage
                        {
                            FileGroupId = indexPage.FileGroupId,
                            LogicalPageId = indexPage.NextLogicalPageId
                        };
                    var loadParams = new LoadFileGroupPageParameters(
                        null, nextPage, false, true);
                    await Database
                        .LoadFileGroupPageAsync(loadParams)
                        .ConfigureAwait(false);

                    indexPage.Dispose();
                    indexPage = nextPage;
                    entryIndex = 0;
                }

                // Compare with end index
                var comp = indexPage.IndexEntries[entryIndex].CompareTo(endIndexInfo);
                if (comp > 0 ||
                    !request.Message.OnIteration(
                        indexPage,
                        (TableIndexLeafInfo)indexPage.IndexEntries[entryIndex],
                        iterationIndex++))
                {
                    break;
                }

                ++entryIndex;
            }

            return true;
        }
        #endregion
    }
}
