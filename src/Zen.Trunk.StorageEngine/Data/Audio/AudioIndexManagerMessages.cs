using System;

namespace Zen.Trunk.Storage.Data.Audio
{
    public class CreateAudioIndexParameters
    {
        public CreateAudioIndexParameters(
            string name,
            FileGroupId indexFileGroupId,
            AudioIndexSubType indexSubType)
        {
            Name = name;
            IndexFileGroupId = indexFileGroupId;
            IndexSubType = indexSubType;
        }

        public string Name { get; }

        public FileGroupId IndexFileGroupId { get; }
        
        public AudioIndexSubType IndexSubType { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SplitAudioIndexPageParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initialises an instance of <see cref="T:SplitAudioIndexPageParameters" />.
        /// </summary>
        public SplitAudioIndexPageParameters(
            RootAudioIndexInfo rootInfo,
            AudioIndexPage pageToSplit,
            AudioIndexPage splitPage)
        {
            RootInfo = rootInfo;
            PageToSplit = pageToSplit;
            SplitPage = splitPage;
        }

        /// <summary>
        /// Initialises an instance of <see cref="T:SplitAudioIndexPageParameters" />.
        /// </summary>
        public SplitAudioIndexPageParameters(
            AudioIndexPage parentPage,
            AudioIndexPage pageToSplit,
            AudioIndexPage splitPage)
        {
            ParentPage = parentPage;
            PageToSplit = pageToSplit;
            SplitPage = splitPage;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the root index information.
        /// </summary>
        /// <value>The root info.</value>
        public RootAudioIndexInfo RootInfo { get; }

        /// <summary>
        /// Gets the parent page.
        /// </summary>
        /// <value>The parent page.</value>
        public AudioIndexPage ParentPage { get; }

        /// <summary>
        /// Gets the page to split.
        /// </summary>
        /// <value>The page to split.</value>
        public AudioIndexPage PageToSplit { get; }

        /// <summary>
        /// Gets the split page.
        /// </summary>
        /// <value>The split page.</value>
        public AudioIndexPage SplitPage { get; }
        #endregion
    }

    public class MergeAudioIndexPageParameters
    {
        #region Public Constructors
        public MergeAudioIndexPageParameters(
            IndexId indexId,
            AudioIndexPage parentPage,
            AudioIndexPage primaryPage,
            AudioIndexPage pageToBeMerged)
        {
            IndexId = indexId;
            ParentPage = parentPage;
            PrimaryPage = primaryPage;
            PageToBeMerged = pageToBeMerged;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the index identifier.
        /// </summary>
        /// <value>
        /// The index identifier.
        /// </value>
        public IndexId IndexId { get; }

        /// <summary>
        /// Gets the parent page.
        /// </summary>
        /// <value>
        /// The parent page.
        /// </value>
        public AudioIndexPage ParentPage { get; }

        /// <summary>
        /// Gets the primary page.
        /// </summary>
        /// <value>
        /// The primary page.
        /// </value>
        public AudioIndexPage PrimaryPage { get; }

        /// <summary>
        /// Gets the page to be merged.
        /// </summary>
        /// <value>
        /// The page to be merged.
        /// </value>
        public AudioIndexPage PageToBeMerged { get; }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class FindAudioIndexParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FindAudioIndexParameters" /> class.
        /// </summary>
        /// <param name="rootInfo">The root information.</param>
        /// <param name="value">The value.</param>
        /// <param name="forInsert">if set to <c>true</c> [for insert].</param>
        public FindAudioIndexParameters(RootAudioIndexInfo rootInfo, long value, bool forInsert)
        {
            RootInfo = rootInfo;
            SampleIndex = value;
            IsForInsert = forInsert;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the root index information.
        /// </summary>
        /// <value>The root info.</value>
        public RootAudioIndexInfo RootInfo { get; }

        /// <summary>
        /// Gets the sample index.
        /// </summary>
        /// <value>The value.</value>
        public long SampleIndex { get; }

        /// <summary>
        /// Gets the logical page identifier.
        /// </summary>
        /// <value>
        /// The logical page identifier.
        /// </value>
        /// <remarks>
        /// This is only valid when performing inserts so the final leaf page entry can be written.
        /// </remarks>
        public LogicalPageId LogicalPageId { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is for insert.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is for insert; otherwise, <c>false</c>.
        /// </value>
        public bool IsForInsert { get; }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class FindAudioIndexResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FindAudioIndexResult"/> class.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="entry">The entry.</param>
        public FindAudioIndexResult(AudioIndexPage page, AudioIndexLeafInfo entry)
        {
            Page = page;
            Entry = entry;
        }

        /// <summary>
        /// Gets the page.
        /// </summary>
        /// <value>
        /// The page.
        /// </value>
        public AudioIndexPage Page { get; }

        /// <summary>
        /// Gets the entry.
        /// </summary>
        /// <value>
        /// The entry.
        /// </value>
        public AudioIndexLeafInfo Entry { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class EnumerateIndexEntriesParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerateIndexEntriesParameters"/> class.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="fromKeys">From keys.</param>
        /// <param name="toKeys">To keys.</param>
        /// <param name="iterationFunc">The iteration function.</param>
        public EnumerateIndexEntriesParameters(
            RootAudioIndexInfo index,
            long fromValue,
            long toValue,
            Func<AudioIndexPage, AudioIndexLeafInfo, int, bool> iterationFunc)
        {
            Index = index;
            FromValue = fromValue;
            ToValue = toValue;
            OnIteration = iterationFunc;
        }

        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        public RootAudioIndexInfo Index { get; }

        /// <summary>
        /// Gets the value controlling the starting bound for enumeration.
        /// </summary>
        /// <value>
        /// From value.
        /// </value>
        public long FromValue { get; }

        /// <summary>
        /// Gets the value controlling the ending bound for enumeration.
        /// </summary>
        /// <value>
        /// To value.
        /// </value>
        public long ToValue { get; }

        /// <summary>
        /// Gets the on iteration.
        /// </summary>
        /// <value>
        /// The on iteration.
        /// </value>
        public Func<AudioIndexPage, AudioIndexLeafInfo, int, bool> OnIteration { get; }
    }

    public class RebuildAudioIndexParameters
    {
        public RebuildAudioIndexParameters(
            FileGroupId fileGroupId,
            ObjectId objectId,
            IndexId indexId)
        {
            FileGroupId = fileGroupId;
            ObjectId = objectId;
            IndexId = indexId;
        }

        public FileGroupId FileGroupId { get; }

        public ObjectId ObjectId { get; }

        public IndexId IndexId { get; }
    }
}
