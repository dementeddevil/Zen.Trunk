using System;
using System.Collections;
using System.Collections.Generic;

namespace Zen.Trunk.Storage.Data.Table
{
    public class CreateTableIndexParameters
    {
        public string Name { get; }

        public FileGroupId IndexFileGroupId { get; }

        public TableIndexSubType IndexSubType { get; }

        public ICollection<Tuple<ushort, TableIndexSortDirection>> Members { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SplitTableIndexPageParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initialises an instance of <see cref="T:SplitTableIndexPageParameters" />.
        /// </summary>
        public SplitTableIndexPageParameters(
            IndexId indexId,
            TableIndexPage pageToSplit,
            TableIndexPage splitPage)
        {
            IndexId = indexId;
            PageToSplit = pageToSplit;
            SplitPage = splitPage;
        }

        /// <summary>
        /// Initialises an instance of <see cref="T:SplitTableIndexPageParameters" />.
        /// </summary>
        public SplitTableIndexPageParameters(
            TableIndexPage parentPage,
            TableIndexPage pageToSplit,
            TableIndexPage splitPage)
        {
            ParentPage = parentPage;
            PageToSplit = pageToSplit;
            SplitPage = splitPage;
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
        /// <value>The parent page.</value>
        public TableIndexPage ParentPage { get; }

        /// <summary>
        /// Gets the page to split.
        /// </summary>
        /// <value>The page to split.</value>
        public TableIndexPage PageToSplit { get; }

        /// <summary>
        /// Gets the split page.
        /// </summary>
        /// <value>The split page.</value>
        public TableIndexPage SplitPage { get; }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class FindTableIndexParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FindTableIndexParameters"/> class.
        /// </summary>
        /// <param name="rootInfo">The root information.</param>
        /// <param name="keys">The keys.</param>
        public FindTableIndexParameters(RootTableIndexInfo rootInfo, object[] keys)
        {
            RootInfo = rootInfo;
            Keys = keys;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FindTableIndexParameters"/> class.
        /// </summary>
        /// <param name="rootInfo">The root information.</param>
        /// <param name="keys">The keys.</param>
        /// <param name="rowLogicalPageId">The row logical page identifier.</param>
        /// <param name="rowId">The row identifier.</param>
        public FindTableIndexParameters(RootTableIndexInfo rootInfo, object[] keys, ulong rowLogicalPageId, uint rowId)
        {
            RootInfo = rootInfo;
            Keys = keys;
            RowLogicalPageId = rowLogicalPageId;
            RowId = rowId;
            IsForInsert = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FindTableIndexParameters"/> class.
        /// </summary>
        /// <param name="rootInfo">The root information.</param>
        /// <param name="keys">The keys.</param>
        /// <param name="rowLogicalPageId">The row logical page identifier.</param>
        /// <param name="rowId">The row identifier.</param>
        /// <param name="rowSize">Size of the row.</param>
        public FindTableIndexParameters(RootTableIndexInfo rootInfo, object[] keys, ulong rowLogicalPageId, uint rowId, ushort rowSize)
        {
            RootInfo = rootInfo;
            Keys = keys;
            RowLogicalPageId = rowLogicalPageId;
            RowId = rowId;
            RowSize = rowSize;
            IsForInsert = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FindTableIndexParameters"/> class.
        /// </summary>
        /// <param name="rootInfo">The root information.</param>
        /// <param name="keys">The keys.</param>
        /// <param name="clusteredKeys">The clustered keys.</param>
        public FindTableIndexParameters(RootTableIndexInfo rootInfo, object[] keys, object[] clusteredKeys)
        {
            RootInfo = rootInfo;
            Keys = keys;
            ClusteredKey = clusteredKeys;
            IsForInsert = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FindTableIndexParameters"/> class.
        /// </summary>
        /// <param name="rootInfo">The root information.</param>
        /// <param name="keys">The keys.</param>
        /// <param name="clusteredKeys">The clustered keys.</param>
        /// <param name="rowSize">Size of the row.</param>
        public FindTableIndexParameters(RootTableIndexInfo rootInfo, object[] keys, object[] clusteredKeys, ushort rowSize)
        {
            RootInfo = rootInfo;
            Keys = keys;
            ClusteredKey = clusteredKeys;
            RowSize = rowSize;
            IsForInsert = true;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the root index information.
        /// </summary>
        /// <value>The root info.</value>
        public RootTableIndexInfo RootInfo { get; }

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public object[] Keys { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is for insert.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is for insert; otherwise, <c>false</c>.
        /// </value>
        public bool IsForInsert { get; }

        /// <summary>
        /// Gets the clustered key.
        /// </summary>
        /// <value>
        /// The clustered key.
        /// </value>
        public object[] ClusteredKey { get; }

        /// <summary>
        /// Gets the row logical id.
        /// </summary>
        /// <value>The row logical id.</value>
        /// <remarks>
        /// This value is only valid for index inserts.
        /// </remarks>
        public ulong RowLogicalPageId { get; }

        /// <summary>
        /// Gets the row id.
        /// </summary>
        /// <value>The row id.</value>
        /// <remarks>
        /// This value is only valid for index inserts.
        /// </remarks>
        public uint RowId { get; }

        /// <summary>
        /// Gets the size of the row.
        /// </summary>
        /// <value>The size of the row.</value>
        /// <remarks>
        /// This value is only valid for index inserts for a clustered index.
        /// </remarks>
        public ushort? RowSize { get; }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class FindTableIndexResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FindTableIndexResult"/> class.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="entry">The entry.</param>
        public FindTableIndexResult(TableIndexPage page, TableIndexLeafInfo entry)
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
        public TableIndexPage Page { get; }

        /// <summary>
        /// Gets the entry.
        /// </summary>
        /// <value>
        /// The entry.
        /// </value>
        public TableIndexLeafInfo Entry { get; }
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

        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        public RootTableIndexInfo Index { get; }

        /// <summary>
        /// Gets from keys.
        /// </summary>
        /// <value>
        /// From keys.
        /// </value>
        public object[] FromKeys { get; }

        /// <summary>
        /// Gets to keys.
        /// </summary>
        /// <value>
        /// To keys.
        /// </value>
        public object[] ToKeys { get; }

        /// <summary>
        /// Gets the on iteration.
        /// </summary>
        /// <value>
        /// The on iteration.
        /// </value>
        public Func<TableIndexPage, TableIndexLeafInfo, int, bool> OnIteration { get; }
    }
}