using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Autofac;
using Serilog;
using Zen.Trunk.Storage.Data.Index;

namespace Zen.Trunk.Storage.Data.Audio
{
    public class AudioIndexManager : IndexManager<RootAudioIndexInfo>
    {
        #region Private Types
        private class CreateAudioIndexRequest : TransactionContextTaskRequest<CreateAudioIndexParameters, IndexId>
        {
            #region Public Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="CreateAudioIndexRequest"/> class.
            /// </summary>
            /// <param name="parameters">The parameters.</param>
            public CreateAudioIndexRequest(CreateAudioIndexParameters parameters) : base(parameters)
            {
            }
            #endregion
        }

        private class SplitAudioIndexPageRequest : TransactionContextTaskRequest<SplitAudioIndexPageParameters, bool>
        {
            #region Public Constructors
            /// <summary>
            /// Initialises an instance of <see cref="T:SplitAudioIndexPage" />.
            /// </summary>
            public SplitAudioIndexPageRequest(SplitAudioIndexPageParameters message)
                : base(message)
            {
            }
            #endregion
        }

        private class MergeAudioIndexPagesRequest : TransactionContextTaskRequest<MergeAudioIndexPageParameters, bool>
        {
            #region Public Constructors
            public MergeAudioIndexPagesRequest(MergeAudioIndexPageParameters parameters)
                : base(parameters)
            {
            }
            #endregion
        }

        private class FindAudioIndexRequest : TransactionContextTaskRequest<FindAudioIndexParameters, FindAudioIndexResult>
        {
            #region Public Constructors
            public FindAudioIndexRequest(FindAudioIndexParameters message)
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

        private class RebuildAudioIndexRequest : TransactionContextTaskRequest<RebuildAudioIndexParameters, bool>
        {
            public RebuildAudioIndexRequest(RebuildAudioIndexParameters message)
                : base(message)
            {
            }
        }
        #endregion

        #region Private Fields
        private readonly ILogger _logger = Log.ForContext<AudioIndexManager>();
        private DatabaseAudio _ownerAudio;
        private readonly ITargetBlock<CreateAudioIndexRequest> _createIndexPort;
        private readonly ITargetBlock<RebuildAudioIndexRequest> _rebuildIndexPort;
        private readonly ITargetBlock<SplitAudioIndexPageRequest> _splitPagePort;
        private readonly ITargetBlock<MergeAudioIndexPagesRequest> _mergePagesPort;
        private readonly ITargetBlock<FindAudioIndexRequest> _findIndexPort;
        private readonly ITargetBlock<EnumerateIndexEntriesRequest> _enumerateIndexEntriesPort;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioIndexManager"/> class.
        /// </summary>
        /// <param name="parentLifetimeScope">The parent provider.</param>
        public AudioIndexManager(ILifetimeScope parentLifetimeScope) : base(parentLifetimeScope)
        {
            _ownerAudio = parentLifetimeScope.Resolve<DatabaseAudio>();
            var taskInterleave = new ConcurrentExclusiveSchedulerPair();
            _createIndexPort = new TransactionContextActionBlock<CreateAudioIndexRequest, IndexId>(
                request => CreateIndexHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            _rebuildIndexPort = new TransactionContextActionBlock<RebuildAudioIndexRequest, bool>(
                request => RebuildIndexHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            _splitPagePort = new TransactionContextActionBlock<SplitAudioIndexPageRequest, bool>(
                request => SplitPageHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler
                });
            _mergePagesPort = new TransactionContextActionBlock<MergeAudioIndexPagesRequest, bool>(
                request => MergePagesHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler
                });
            _findIndexPort = new TransactionContextActionBlock<FindAudioIndexRequest, FindAudioIndexResult>(
                request => FindIndexHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler
                });
            _enumerateIndexEntriesPort = new TransactionContextActionBlock<EnumerateIndexEntriesRequest, bool>(
                request => EnumerateIndexEntriesHandlerAsync(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ConcurrentScheduler
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
        /// Creates an audio index.
        /// </summary>
        /// <param name="parameters">The audio index creation parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task<IndexId> CreateIndexAsync(CreateAudioIndexParameters parameters)
        {
            var request = new CreateAudioIndexRequest(parameters);
            _createIndexPort.Post(request);
            return request.Task;
        }

        /// <summary>
        /// Rebuilds an audio index.
        /// </summary>
        /// <param name="parameters">The audio index rebuild parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task RebuildIndexAsync(RebuildAudioIndexParameters parameters)
        {
            var request = new RebuildAudioIndexRequest(parameters);
            _rebuildIndexPort.Post(request);
            return request.Task;
        }

        /// <summary>
        /// Splits an index page into two pages.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task SplitPageAsync(SplitAudioIndexPageParameters parameters)
        {
            var request = new SplitAudioIndexPageRequest(parameters);
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
        public Task<bool> MergePagesAsync(MergeAudioIndexPageParameters parameters)
        {
            var request = new MergeAudioIndexPagesRequest(parameters);
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
        public Task<FindAudioIndexResult> FindIndexAsync(FindAudioIndexParameters parameters)
        {
            var findLeaf = new FindAudioIndexRequest(parameters);
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
        private async Task<IndexId> CreateIndexHandlerAsync(CreateAudioIndexRequest request)
        {
            // Determine index identifier and validate index sub-type
            var indexId = new IndexId(1);
            foreach (var def in Indices)
            {
                indexId = new IndexId(Math.Max(indexId.Value, def.IndexId.Value + 1));

                // Canot have more than one of a given index sub-type
                if (def.IndexSubType == request.Message.IndexSubType)
                {
                    throw new CoreException($"Index of sub-type {request.Message.IndexSubType} is already defined.");
                }
            }

            // Create root audio index information
            var rootAudioIndexInfo =
                new RootAudioIndexInfo(indexId)
                {
                    Name = request.Message.Name,
                    IndexFileGroupId = request.Message.IndexFileGroupId,
                    IndexSubType = request.Message.IndexSubType,
                    ObjectId = _ownerAudio.ObjectId
                };

            // Create the root index page
            using (var rootPage =
                new AudioIndexPage
                {
                    FileGroupId = rootAudioIndexInfo.IndexFileGroupId,
                    ObjectId = rootAudioIndexInfo.ObjectId,
                    IndexId = indexId,
                    IndexType = IndexType.Root | IndexType.Leaf
                })
            {
                // Initialise root index page
                await Database
                    .InitFileGroupPageAsync(
                        new InitFileGroupPageParameters(
                            null, rootPage, true, false, true, true))
                    .ConfigureAwait(false);

                // Setup root index page
                rootPage.SetHeaderDirty();
                rootPage.SetContext(_ownerAudio, rootAudioIndexInfo);
                rootAudioIndexInfo.RootLogicalPageId = rootPage.LogicalPageId;
                AddIndexInfo(rootAudioIndexInfo);
            }

            // While we are here; force a rebuild of the index
            if (_ownerAudio.HasData)
            {
                // Don't bother posting a message to the queue; do it directly
                var rebuildRequest =
                    new RebuildAudioIndexRequest(
                        new RebuildAudioIndexParameters(
                            request.Message.IndexFileGroupId,
                            _ownerAudio.ObjectId,
                            indexId));
                await RebuildIndexHandlerAsync(rebuildRequest).ConfigureAwait(false);
            }

            return indexId;
        }

        private async Task<bool> RebuildIndexHandlerAsync(RebuildAudioIndexRequest request)
        {
            TryGetIndexInfo(request.Message.IndexId, out var rootInfo);
            var indexPageHierarchy = new List<AudioIndexPage>();

            var currentIndexPage =
                await CreateIndexPageAsync(
                    rootInfo,
                    request.Message.FileGroupId,
                    request.Message.ObjectId,
                    request.Message.IndexId)
                .ConfigureAwait(false);
            indexPageHierarchy.Add(currentIndexPage);

            var dataLogicalPageId = _ownerAudio.SchemaRootPage.DataFirstLogicalPageId;
            var sampleOrdinalValue = 0L;
            while (true)
            {
                var currentPage =
                    new AudioDataPage
                    {
                        FileGroupId = request.Message.FileGroupId,
                        LogicalPageId = dataLogicalPageId
                    };
                await Database
                    .LoadFileGroupPageAsync(
                        new LoadFileGroupPageParameters(
                            null, currentPage, false, true))
                    .ConfigureAwait(false);

                if (currentIndexPage.IndexCount < currentIndexPage.MaxIndexEntries)
                {
                    currentIndexPage.AddLinkToPage(
                        new AudioIndexLeafInfo(
                            sampleOrdinalValue,
                            currentPage.LogicalPageId));
                }
                else
                {
                    // Save current index page and setup state machine
                    currentIndexPage.Save();
                    var activeIndexPage = currentIndexPage;
                    currentIndexPage = null;

                    for (byte depth = 0; depth < indexPageHierarchy.Count; ++depth)
                    {
                        // Create new index page
                        var newIndexPage =
                            await CreateIndexPageAsync(
                                rootInfo,
                                request.Message.FileGroupId,
                                request.Message.ObjectId,
                                request.Message.IndexId,
                                depth)
                            .ConfigureAwait(false);
                        
                        // First new index page created must be new current index page
                        if (currentIndexPage == null)
                        {
                            currentIndexPage = newIndexPage;
                        }

                        // At depth == 0 we need to add a leaf entry to the new page
                        if (depth == 0)
                        {
                            activeIndexPage.AddLinkToPage(
                                new AudioIndexLeafInfo(
                                    sampleOrdinalValue,
                                    currentPage.LogicalPageId));
                        }
                        else
                        {
                            newIndexPage.AddLinkToPage(
                                new AudioIndexLogicalInfo(
                                    activeIndexPage.IndexEntries[0].SampleIndex,
                                    activeIndexPage.LogicalPageId));
                        }

                        // Link prior index page with the newly created page and swap in hierarchy
                        var priorIndexPage = indexPageHierarchy[depth];
                        priorIndexPage.NextLogicalPageId = newIndexPage.LogicalPageId;
                        newIndexPage.PrevLogicalPageId = priorIndexPage.LogicalPageId;
                        newIndexPage.IndexType = priorIndexPage.IndexType;
                        indexPageHierarchy[depth] = newIndexPage;

                        // Handle wire-up of parent logical page id
                        if (priorIndexPage.ParentLogicalPageId == LogicalPageId.Zero)
                        {
                            // Prior page had no parent (so must have been a root)

                            // Create new parent page (as root)
                            var newParentPage =
                                await CreateIndexPageAsync(
                                    rootInfo,
                                    request.Message.FileGroupId,
                                    request.Message.ObjectId,
                                    request.Message.IndexId,
                                    (byte)(depth + 1))
                                .ConfigureAwait(false);
                            newParentPage.IndexType = IndexType.Root;

                            // Add links to both index pages to new root
                            newParentPage.AddLinkToPage(
                                new AudioIndexLogicalInfo(
                                    priorIndexPage.IndexEntries[0].SampleIndex,
                                    priorIndexPage.LogicalPageId));
                            newParentPage.AddLinkToPage(
                                new AudioIndexLogicalInfo(
                                    newIndexPage.IndexEntries[0].SampleIndex,
                                    newIndexPage.LogicalPageId));

                            // Remove root status from prior index page and correctly
                            //  setup intermediate index type as needed
                            priorIndexPage.IsRootIndex = false;
                            priorIndexPage.IsIntermediateIndex = (!priorIndexPage.IsRootIndex && !priorIndexPage.IsLeafIndex);
                            newIndexPage.IndexType = priorIndexPage.IndexType;

                            // Add new parent page to hierarchy
                            indexPageHierarchy.Add(newParentPage);

                            // We're done with the lock hierarchy updates
                            break;
                        }
                        else
                        {
                            // Prior page has a parent (so we need to see if we can update it to point to this page)

                            // Check whether the parent page has space for new index (it must be in the hierarchy)
                            var parentPage = indexPageHierarchy[depth + 1];
                            if (parentPage.IndexCount < parentPage.MaxIndexEntries)
                            {
                                parentPage.AddLinkToPage(
                                    new AudioIndexLogicalInfo(
                                        newIndexPage.IndexEntries[0].SampleIndex,
                                        newIndexPage.LogicalPageId));

                                // We're done with the lock hierarchy updates
                                break;
                            }
                            else
                            {
                                // Update active index page
                                activeIndexPage = newIndexPage;
                            }
                        }
                    }
                }

                // Advance to next audio page
                if (currentPage.NextLogicalPageId != LogicalPageId.Zero)
                {
                    dataLogicalPageId = currentPage.NextLogicalPageId;
                    sampleOrdinalValue += _ownerAudio.SchemaRootPage.SamplesPerPage;
                }
                else
                {
                    break;
                }
            }

            // Save current index page
            currentIndexPage.Save();

            // Update the root index information with tip of hierarchy
            var rootPage = indexPageHierarchy.Last();
            if (rootPage.LogicalPageId != rootInfo.RootLogicalPageId)
            {
                rootInfo.RootLogicalPageId = rootPage.LogicalPageId;
                _ownerAudio.SchemaRootPage.SetDataDirty();
                _ownerAudio.SchemaRootPage.Save();
            }

            return true;
        }

        private async Task<bool> SplitPageHandlerAsync(SplitAudioIndexPageRequest request)
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
                    new AudioIndexPage
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
            // TODO: Implement special case so we can do append mode of split
            //  In this case we will only move a single entry to the new page
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

        private async Task<AudioIndexPage> SplitRootIfNecessaryAndReturnParentAsync(AudioIndexPage currentPage)
        {
            AudioIndexPage parentPage;

            if (!currentPage.IsRootIndex)
            {
                // Current page is non-root page; just load the parent page
                parentPage =
                    new AudioIndexPage
                    {
                        FileGroupId = currentPage.FileGroupId,
                        ObjectId = currentPage.ObjectId,
                        IndexId = currentPage.IndexId,
                        LogicalPageId = currentPage.ParentLogicalPageId
                    };
                await Database
                    .LoadFileGroupPageAsync(new LoadFileGroupPageParameters(null, parentPage, false, true))
                    .ConfigureAwait(false);
            }
            else
            {
                // Initialise new page (it will become the new root)
                var newRootPage =
                    new AudioIndexPage
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
                newRootPage.Depth = (byte)(currentPage.Depth + 1);
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

        private async Task<bool> MergePagesHandlerAsync(MergeAudioIndexPagesRequest request)
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
                    .Cast<AudioIndexLogicalInfo>()
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

                // Update the root index information with logical id
                //  of the primary index page.
                var root = GetIndexInfo(primaryPage.IndexId);
                root.RootLogicalPageId = primaryPage.LogicalPageId;
                root.RootIndexDepth = primaryPage.Depth;
            }

            return true;
        }

        private bool NeedToSplitPage(ushort indexCount, ushort maxIndexEntries, byte fillFactor)
        {
            // Full pages always need a split
            if (indexCount == maxIndexEntries)
            {
                return true;
            }

            // Everything else relies upon the fill-factor
            // NOTE: We treat a fill-factor of zero the same as 100...
            if (fillFactor == 0)
            {
                fillFactor = 100;
            }

            // Determine whether we have passed the allowed fill ratio
            var currentFillRatio = (byte)((double)indexCount * 100.0 / (double)maxIndexEntries);
            return currentFillRatio >= fillFactor;
        }

        private async Task<FindAudioIndexResult> FindIndexHandlerAsync(FindAudioIndexRequest request)
        {
            // Setup search state machine
            // TODO: Refactor this into state-machine classes
            AudioIndexPage parentPage = null;
            var logicalId = request.Message.RootInfo.RootLogicalPageId;
            var fillFactor = request.Message.RootInfo.FillFactor;
            var isForInsert = request.Message.IsForInsert;
            var findInfo = new AudioIndexInfo(request.Message.SampleIndex);

            // Main find loop
            AudioIndexPage indexPage = null;
            while (true)
            {
                // Load the current index page
                if (indexPage == null || indexPage.LogicalPageId != logicalId)
                {
                    // TODO: Setup lock mode (read or intent exclusive)
                    indexPage =
                        new AudioIndexPage
                        {
                            FileGroupId = request.Message.RootInfo.IndexFileGroupId,
                            LogicalPageId = logicalId
                        };
                    await Database
                        .LoadFileGroupPageAsync(
                            new LoadFileGroupPageParameters(
                                null, indexPage, false, true))
                        .ConfigureAwait(false);
                }

                // Page splits for insert; need to split page as necessary on the way down to ensure space is available
                AudioIndexPage nextPage = null;
                if (isForInsert && NeedToSplitPage(indexPage.IndexCount, indexPage.MaxIndexEntries, fillFactor))
                {
                    nextPage = new AudioIndexPage();
                    var split = new SplitAudioIndexPageParameters(parentPage, indexPage, nextPage);
                    await SplitPageAsync(split).ConfigureAwait(false);
                }

                // Perform crab-search through index table entries and split as necessary

                // Search for descent point
                for (var index = 0;
                    indexPage != null && index < indexPage.IndexCount;
                    ++index)
                {
                    // Get lower and upper comparison values
                    var compLower = indexPage.CompareIndex(index, findInfo);
                    var compHigher =
                        (nextPage != null && index + 1 >= indexPage.IndexCount)
                        ? nextPage.CompareIndex(0, findInfo)
                        : indexPage.CompareIndex(index + 1, findInfo);

                    // Determine whether this is a descent point
                    var canDescend = false;
                    if (index == 0 && compLower >= 0 && isForInsert)
                    {
                        // We are before the first descent point handling an insert so rewrite first entry
                        indexPage.IndexEntries[0] = findInfo;
                        canDescend = true;
                    }
                    else if (compLower <= 0 && compHigher > 0)
                    {
                        canDescend = true;
                    }

                    // Keep searching if this isn't the point we are looking for
                    if (!canDescend)
                    {
                        continue;
                    }

                    // If this is a leaf index page then we are finished
                    if (indexPage.TryGetIndexEntryLeafInfo(index, out var leaf))
                    {
                        return new FindAudioIndexResult(indexPage, leaf);
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
                if (nextPage != null &&
                    indexPage != null &&
                    nextPage.LogicalPageId == indexPage.NextLogicalPageId)
                {
                    // Traverse into split page
                    indexPage = nextPage;
                    nextPage = null;
                }
                else
                {
                    // We really really shouldn't reach this point

                    if (indexPage != null &&
                        indexPage.NextLogicalPageId != LogicalPageId.Zero)
                    {
                        // Determine new logical id and make this page
                        //	the new previous page.
                        logicalId = indexPage.NextLogicalPageId;
                        indexPage = null;
                    }

                    // Sanity check
                    System.Diagnostics.Debug.Assert(indexPage == null);
                }
            }
        }

        private async Task<bool> EnumerateIndexEntriesHandlerAsync(EnumerateIndexEntriesRequest request)
        {
            var find = new FindAudioIndexParameters(
                request.Message.Index,
                request.Message.FromValue,
                false);
            var result = await FindIndexAsync(find).ConfigureAwait(false);
            if (result == null) return false;

            var indexPage = result.Page;
            var endIndexInfo = new AudioIndexInfo(request.Message.ToValue);
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
                        new AudioIndexPage
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
                        (AudioIndexLeafInfo)indexPage.IndexEntries[entryIndex],
                        iterationIndex++))
                {
                    break;
                }

                ++entryIndex;
            }

            return true;
        }

        private async Task<AudioIndexPage> CreateIndexPageAsync(
            RootAudioIndexInfo rootInfo,
            FileGroupId fileGroupId,
            ObjectId objectId,
            IndexId indexId,
            byte depth = 0)
        {
            var indexPage =
                new AudioIndexPage
                {
                    FileGroupId = fileGroupId,
                    ObjectId = objectId,
                    IndexId = indexId,
                    ReadOnly = false
                };
            await Database
                .InitFileGroupPageAsync(
                    new InitFileGroupPageParameters(
                        null, indexPage, true, true, true, true))
                .ConfigureAwait(false);
            indexPage.SetHeaderDirty();
            indexPage.SetContext(_ownerAudio, rootInfo);
            indexPage.Depth = depth;
            return indexPage;
        }
        #endregion
    }
}

