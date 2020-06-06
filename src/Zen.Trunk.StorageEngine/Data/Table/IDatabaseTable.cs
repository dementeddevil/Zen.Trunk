using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data.Table
{
    public interface IDatabaseTable : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this instance is a system table.
        /// </summary>
        /// <value>
        /// <c>true</c> if system table; otherwise, <c>false</c>.
        /// </value>
        bool IsSystemTable { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is loading.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is loading; otherwise, <c>false</c>.
        /// </value>
        bool IsLoading { get; }

        /// <summary>
        /// Gets the schema first logical identifier.
        /// </summary>
        /// <value>
        /// The schema first logical identifier.
        /// </value>
        LogicalPageId SchemaFirstLogicalPageId { get; }

        /// <summary>
        /// Gets the schema last logical identifier.
        /// </summary>
        /// <value>
        /// The schema last logical identifier.
        /// </value>
        LogicalPageId SchemaLastLogicalPageId { get; }

        /// <summary>
        /// Gets the schema root page.
        /// </summary>
        /// <value>
        /// The schema root page.
        /// </value>
        TableSchemaRootPage SchemaRootPage { get; }

        /// <summary>
        /// Gets or sets the first logical page identifier for table data.
        /// </summary>
        /// <value>
        /// The logical page identifier.
        /// </value>
        /// <exception cref="LockException">
        /// Thrown if locking the table schema for modification fails.
        /// </exception>
        /// <exception cref="LockTimeoutException">
        /// Thrown if locking the table schema for modification fails due to timeout.
        /// </exception>
        LogicalPageId DataFirstLogicalPageId { get; }

        /// <summary>
        /// Gets or sets the last logical page identifier for table data.
        /// </summary>
        /// <value>
        /// The logical page identifier.
        /// </value>
        /// <exception cref="LockException">
        /// Thrown if locking the table schema for modification fails.
        /// </exception>
        /// <exception cref="LockTimeoutException">
        /// Thrown if locking the table schema for modification fails due to timeout.
        /// </exception>
        LogicalPageId DataLastLogicalPageId { get; set; }

        /// <summary>
        /// Gets/sets a value controlling whether explicitly setting the 
        /// identity column on insert operations is supported.
        /// </summary>
        bool AllowIdentityInsert { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is heap.
        /// </summary>
        /// <remarks>
        /// A heap table is a table that does not have a clustered index.
        /// </remarks>
        /// <value>
        /// <c>true</c> if this instance is heap; otherwise, <c>false</c>.
        /// </value>
        bool IsHeap { get; }

        /// <summary>
        /// Gets the clustered index definition.
        /// </summary>
        /// <value>
        /// The clustered index definition or null if this is a heap.
        /// </value>
        RootTableIndexInfo ClusteredIndex { get; }

        /// <summary>
        /// Gets a value indicating whether this table instance has any data.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [has data]; otherwise, <c>false</c>.
        /// </value>
        bool HasData { get; }

        /// <summary>
        /// Gets the table minimum row size.
        /// </summary>
        ushort MinRowSize { get; }

        /// <summary>
        /// Gets the table maximum row size.
        /// </summary>
        ushort MaxRowSize { get; }

        /// <summary>
        /// Gets or sets the lock timeout.
        /// </summary>
        /// <value>The lock timeout.</value>
        TimeSpan LockTimeout { get; set; }

        /// <summary>
        /// Gets the columns defined on this table.
        /// </summary>
        /// <value>The columns.</value>
        IList<TableColumnInfo> Columns { get; }

        /// <summary>
        /// Gets the column constraints defined on this table.
        /// </summary>
        /// <value>The constraints.</value>
        IList<RowConstraint> Constraints { get; }

        /// <summary>
        /// Gets the owner file-group device.
        /// </summary>
        /// <value>The database.</value>
        IFileGroupDevice FileGroupDevice { get; }

        /// <summary>
        /// Loads the table schema starting from the specified logical id
        /// </summary>
        /// <param name="firstLogicalPageId">The first logical id.</param>
        /// <returns></returns>
        Task LoadSchemaAsync(LogicalPageId firstLogicalPageId);

        /// <summary>
        /// Begins the column update.
        /// </summary>
        /// <remarks>
        /// This method must be called prior to modification of column
        /// elements or column definitions.
        /// </remarks>
        void BeginColumnUpdate();

        /// <summary>
        /// Adds the column.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="index">The index.</param>
        /// <remarks>
        /// A call to BeginColumnUpdate must occur once before calls to this method.
        /// </remarks>
        void AddColumn(TableColumnInfo column, int index);

        /// <summary>
        /// Removes the column.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <remarks>
        /// A call to BeginColumnUpdate must occur once before calls to this method.
        /// </remarks>
        void RemoveColumn(int index);

        /// <summary>
        /// Removes the column.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <remarks>
        /// A call to BeginColumnUpdate must occur once before calls to this method.
        /// </remarks>
        void RemoveColumn(string name);

        /// <summary>
        /// Adds the constraint.
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        /// <remarks>
        /// A call to BeginColumnUpdate must occur once before calls to this method.
        /// </remarks>
        void AddConstraint(RowConstraint constraint);

        /// <summary>
        /// Removes the constraint.
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        /// <remarks>
        /// A call to BeginColumnUpdate must occur once before calls to this method.
        /// </remarks>
        void RemoveConstraint(RowConstraint constraint);

        /// <summary>
        /// Ends the column update and updates the table rows.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The table data will be rewritten during this method unless the 
        /// object is currently being loaded from storage and as such may
        /// fail if an exclusive lock cannot be obtained on the table -
        /// although of course - we should already have one given the lock
        /// we must already have on the schema.
        /// </para>
        /// <para>
        /// A call to BeginColumnUpdate must occur once before calls to this method.
        /// </para>
        /// </remarks>
        Task EndColumnUpdate();

        /// <summary>
        /// Creates the specified index on the table and returns the index id.
        /// </summary>
        /// <param name="info">The information.</param>
        Task<IndexId> CreateIndexAsync(CreateTableIndexParameters info);

        /// <summary>
        /// Updates the size of the row.
        /// </summary>
        void UpdateRowSize();

        /// <summary>
        /// Searches for a column definition with the given ID.
        /// </summary>
        /// <param name="columnId"></param>
        /// <returns></returns>
        TableColumnInfo FindColumn(ushort columnId);

        /// <summary>
        /// Determines the schema changes.
        /// </summary>
        /// <param name="rewriteSchema">if set to <c>true</c> [rewrite schema].</param>
        /// <param name="rewriteTable">if set to <c>true</c> [rewrite table].</param>
        void DetermineSchemaChanges(bool rewriteSchema, bool rewriteTable);

        /// <summary>
        /// Processes the constraints.
        /// </summary>
        /// <param name="rowData">The row data.</param>
        /// <param name="forInsert">if set to <c>true</c> [for insert].</param>
        void ProcessConstraints(object[] rowData, bool forInsert);

        /// <summary>
        /// Adds the row.
        /// </summary>
        /// <param name="columnIDs">The column identifiers.</param>
        /// <param name="rowData">The row data.</param>
        /// <returns></returns>
        Task AddRow(uint[] columnIDs, object[] rowData);
    }
}