using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Data.Index;

namespace Zen.Trunk.Storage.Data.Table
{
    /// <summary>
    /// Represents a database table
    /// </summary>
    /// <remarks>
    /// This object is returned via IoC on calls via the FileGroupDevice
    /// </remarks>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DatabaseTable : IDisposable
    {
        #region Public Objects
        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="Collection{TableColumnInfo}" />
        public class ColumnCollection : Collection<TableColumnInfo>
        {
            #region Private Fields
            private readonly DatabaseTable _owner;

            #endregion

            #region Public Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="ColumnCollection"/> class.
            /// </summary>
            /// <param name="owner">The owner.</param>
            public ColumnCollection(DatabaseTable owner)
            {
                _owner = owner;
            }
            #endregion

            #region Internal Properties
            internal bool SkipInsertChecks { get; set; }
            #endregion

            #region Protected Methods
            /// <summary>
            /// Inserts an element into the <see cref="T:System.Collections.ObjectModel.Collection`1" /> at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
            /// <param name="item">The object to insert. The value can be null for reference types.</param>
            /// <exception cref="ArgumentException">
            /// Column name is empty.
            /// or
            /// Column name not unique.
            /// or
            /// Table can only have a single timestamp column.
            /// or
            /// Table can only have a single identity column.
            /// or
            /// Table row size is above maximum allowed.
            /// </exception>
            /// <exception cref="InvalidOperationException">Auto increment specified on unsupported data-type.</exception>
            protected override void InsertItem(int index, TableColumnInfo item)
            {
                VerifyColumnsUpdatable();

                if (!SkipInsertChecks)
                {
                    // Validate column information
                    if (string.IsNullOrEmpty(item.Name))
                    {
                        throw new ArgumentException("Column name is empty.");
                    }
                    if (this.Any(tc => string.Equals(tc.Name, item.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new ArgumentException("Column name not unique.");
                    }
                    if (item.DataType == TableColumnDataType.Timestamp && _owner._updatedConstraints.HasTimestamp)
                    {
                        throw new ArgumentException("Table can only have a single timestamp column.");
                    }
                    if (item.AutoIncrement && _owner._updatedConstraints.HasIdentity)
                    {
                        throw new ArgumentException("Table can only have a single identity column.");
                    }
                    if (item.AutoIncrement && !item.IsIncrementSupported)
                    {
                        throw new InvalidOperationException("Auto increment specified on unsupported data-type.");
                    }

                    // Check column space requirement
                    var columnSize = item.DataSize;
                    var newRowSize = _owner._rowSize + columnSize;
                    if (newRowSize.Min > 7900)
                    {
                        throw new ArgumentException("Table row size is above maximum allowed.");
                    }

                    // Setup column ID
                    item.Id = _owner._nextColumnId++;

                    // Add constraint
                    if (item.DataType == TableColumnDataType.Timestamp)
                    {
                        _owner.AddConstraint(
                            new RowConstraint(item.Id, RowConstraintType.Timestamp));
                    }
                    if (item.AutoIncrement)
                    {
                        _owner.AddConstraint(
                            new RowConstraint(item.Id, RowConstraintType.Identity));
                    }
                }

                // Add column definition
                base.InsertItem(index, item);
                ((INotifyPropertyChanged)item).PropertyChanged += _owner.Column_PropertyChanged;

                // Adjust table row size
                _owner.UpdateRowSize();
            }

            /// <summary>
            /// Replaces the element at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index of the element to replace.</param>
            /// <param name="item">The new value for the element at the specified index. The value can be null for reference types.</param>
            /// <exception cref="NotSupportedException"></exception>
            protected override void SetItem(int index, TableColumnInfo item)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Removes all elements from the <see cref="T:System.Collections.ObjectModel.Collection`1" />.
            /// </summary>
            protected override void ClearItems()
            {
                VerifyColumnsUpdatable();
                base.ClearItems();
            }

            /// <summary>
            /// Removes the element at the specified index of the <see cref="T:System.Collections.ObjectModel.Collection`1" />.
            /// </summary>
            /// <param name="index">The zero-based index of the element to remove.</param>
            protected override void RemoveItem(int index)
            {
                VerifyColumnsUpdatable();
                base.RemoveItem(index);
            }
            #endregion

            #region Private Methods
            private void VerifyColumnsUpdatable()
            {
                if (!_owner._canUpdateSchema)
                {
                    throw new InvalidOperationException("Column list cannot be modified.");
                }
            }
            #endregion
        }

        private class ConstraintCollection : Collection<RowConstraint>
        {
            #region Private Fields
            private readonly DatabaseTable _owner;
            private readonly Dictionary<ushort, List<RowConstraint>> _constraints;
            private RowConstraint _identityConstraint;
            private RowConstraint _timestampConstraint;
            #endregion

            #region Public Constructors
            public ConstraintCollection(DatabaseTable owner)
            {
                _owner = owner;
                _constraints = new Dictionary<ushort, List<RowConstraint>>();
            }
            #endregion

            #region Public Properties
            public bool HasIdentity => (_identityConstraint != null);

            public bool HasTimestamp => (_timestampConstraint != null);

            #endregion

            #region Public Methods
            public void ProcessConstraints(object[] rowData, bool forInsert)
            {
                for (var index = 0; index < _owner.Columns.Count; ++index)
                {
                    var column = _owner.Columns[index];
                    if (_constraints.ContainsKey(column.Id))
                    {
                        foreach (var constraint in _constraints[(byte)index])
                        {
                            rowData[index] = ExecuteConstraint(rowData[index],
                                column, constraint, forInsert);
                        }
                    }
                }
            }

            public object ExecuteConstraint(object data,
                TableColumnInfo column, RowConstraint constraint,
                bool forInsert)
            {
                var result = data;
                switch (constraint.ConstraintType)
                {
                    case RowConstraintType.Identity:
                        switch (column.DataType)
                        {
                            case TableColumnDataType.Int:
                                break;

                        }
                        break;
                    case RowConstraintType.Timestamp:

                    case RowConstraintType.Default:
                    case RowConstraintType.Check:
                        break;
                }
                return result;
            }

            public void RemoveAllConstraints(ushort columnId)
            {
                if (_constraints.ContainsKey(columnId))
                {
                    foreach (var constraint in _constraints[columnId])
                    {
                        // Cleanup identity and timestamp if among the
                        //	deleted constraints
                        if (_identityConstraint == constraint)
                        {
                            _identityConstraint = null;
                        }
                        if (_timestampConstraint == constraint)
                        {
                            _timestampConstraint = null;
                        }

                        // Remove from linear collection
                        var index = IndexOf(constraint);
                        base.RemoveItem(index);
                    }
                    _constraints.Remove(columnId);
                }
            }
            #endregion

            #region Protected Methods
            protected override void InsertItem(int index, RowConstraint item)
            {
                VerifyColumnsUpdatable();

                // Check constraint type
                // Only allowed one identity constraint and one timestamp constraint
                switch (item.ConstraintType)
                {
                    case RowConstraintType.Identity:
                        if (_identityConstraint != null)
                        {
                            throw new InvalidOperationException("Table can have only one identity column.");
                        }
                        _identityConstraint = item;
                        AddConstraintInternal(item);
                        break;

                    case RowConstraintType.Timestamp:
                        if (_timestampConstraint != null)
                        {
                            throw new InvalidOperationException("Table can have only one timestamp column.");
                        }
                        _timestampConstraint = item;
                        AddConstraintInternal(item);
                        break;
                    case RowConstraintType.Default:
                        if (_constraints.ContainsKey(item.ColumnId))
                        {
                            foreach (var test in _constraints[item.ColumnId])
                            {
                                if (test.ConstraintType == RowConstraintType.Default)
                                {
                                    throw new InvalidOperationException("Cannot have more than one default constraint on a column.");
                                }
                            }
                        }
                        AddConstraintInternal(item);
                        break;
                    case RowConstraintType.Check:
                        if (_constraints.ContainsKey(item.ColumnId))
                        {
                            foreach (var test in _constraints[item.ColumnId])
                            {
                                if (test.ConstraintType == RowConstraintType.Check)
                                {
                                    throw new InvalidOperationException("Cannot have more than one check constraint on a column.");
                                }
                            }
                        }
                        AddConstraintInternal(item);
                        break;
                }
                base.InsertItem(index, item);
            }

            protected override void SetItem(int index, RowConstraint item)
            {
                throw new NotSupportedException();
            }

            protected override void ClearItems()
            {
                VerifyColumnsUpdatable();
                base.ClearItems();
            }

            protected override void RemoveItem(int index)
            {
                VerifyColumnsUpdatable();

                // Remove from constraint lookup zone
                var constraint = base[index];
                if (_constraints.ContainsKey(constraint.ColumnId))
                {
                    if (_identityConstraint == constraint)
                    {
                        _identityConstraint = null;
                    }
                    if (_timestampConstraint == constraint)
                    {
                        _timestampConstraint = null;
                    }
                    _constraints[constraint.ColumnId].Remove(constraint);
                }

                // Remove from linear collection
                base.RemoveItem(index);
            }
            #endregion

            #region Private Methods
            private void VerifyColumnsUpdatable()
            {
                if (!_owner._canUpdateSchema)
                {
                    throw new InvalidOperationException("Constraint list cannot be modified.");
                }
            }

            private void AddConstraintInternal(RowConstraint constraint)
            {
                if (!_constraints.ContainsKey(constraint.ColumnId))
                {
                    _constraints.Add(constraint.ColumnId, new List<RowConstraint>());
                }
                _constraints[constraint.ColumnId].Add(constraint);
            }
            #endregion
        }
        #endregion

        #region Private Fields
        private readonly FileGroupDevice _owner;
        private readonly ILifetimeScope _lifetimeScope;

        private readonly List<TableSchemaPage> _schemaPages = new List<TableSchemaPage>();
        private byte _nextColumnId = 1;
        private bool _canUpdateSchema;
        private ColumnCollection _updatedColumns;
        private ReadOnlyCollection<TableColumnInfo> _columns;
        private ConstraintCollection _updatedConstraints;
        private ReadOnlyCollection<RowConstraint> _constraints;
        private Collection<RootTableIndexInfo> _indices;

        private InclusiveRange _rowSize;
        private ushort _rowsPerPage;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseTable"/> class.
        /// </summary>
        /// <param name="parentLifetimeScope">The parent lifetime scope.</param>
        public DatabaseTable(ILifetimeScope parentLifetimeScope)
        {
            _owner = parentLifetimeScope.Resolve<FileGroupDevice>();
            _lifetimeScope = parentLifetimeScope.BeginLifetimeScope(
                builder =>
                {
                    builder.RegisterInstance(this);
                    builder.RegisterType<TableIndexManager>()
                        .As<IndexManager>()
                        .As<TableIndexManager>()
                        .SingleInstance();
                });
#if DEBUG
            LockTimeout = TimeSpan.FromSeconds(30);
#else
			LockTimeout = TimeSpan.FromSeconds(10);
#endif

            FileGroupId = FileGroupId.Invalid;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the file group id.
        /// </summary>
        /// <value>The file group id.</value>
        public FileGroupId FileGroupId { get; set; }

        /// <summary>
        /// Gets or sets the name of the file group.
        /// </summary>
        /// <value>The name of the file group.</value>
        public string FileGroupName { get; set; }

        /// <summary>
        /// Gets the table object ID.
        /// </summary>
        public ObjectId ObjectId { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this instance is a system table.
        /// </summary>
        /// <value>
        /// <c>true</c> if system table; otherwise, <c>false</c>.
        /// </value>
        public bool IsSystemTable { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is loading.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is loading; otherwise, <c>false</c>.
        /// </value>
        public bool IsLoading { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is new table.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is new table; otherwise, <c>false</c>.
        /// </value>
        public bool IsNewTable { get; internal set; }

        /// <summary>
        /// Gets the schema first logical identifier.
        /// </summary>
        /// <value>
        /// The schema first logical identifier.
        /// </value>
        public LogicalPageId SchemaFirstLogicalPageId { get; internal set; }

        /// <summary>
        /// Gets the schema last logical identifier.
        /// </summary>
        /// <value>
        /// The schema last logical identifier.
        /// </value>
        public LogicalPageId SchemaLastLogicalPageId { get; internal set; }

        /// <summary>
        /// Gets the schema root page.
        /// </summary>
        /// <value>
        /// The schema root page.
        /// </value>
        public TableSchemaRootPage SchemaRootPage => (TableSchemaRootPage)_schemaPages[0];

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
        public LogicalPageId DataFirstLogicalPageId
        {
            get => SchemaRootPage.DataFirstLogicalPageId;
            private set => SchemaRootPage.DataFirstLogicalPageId = value;
        }

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
        public LogicalPageId DataLastLogicalPageId
        {
            get => SchemaRootPage.DataLastLogicalPageId;
            set => SchemaRootPage.DataLastLogicalPageId = value;
        }

        /// <summary>
        /// Gets/sets a value controlling whether explicitly setting the 
        /// identity column on insert operations is supported.
        /// </summary>
        public bool AllowIdentityInsert { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is heap.
        /// </summary>
        /// <remarks>
        /// A heap table is a table that does not have a clustered index.
        /// </remarks>
        /// <value>
        /// <c>true</c> if this instance is heap; otherwise, <c>false</c>.
        /// </value>
        public bool IsHeap { get; private set; }

        /// <summary>
        /// Gets the clustered index definition.
        /// </summary>
        /// <value>
        /// The clustered index definition or null if this is a heap.
        /// </value>
        public RootTableIndexInfo ClusteredIndex { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this table instance has any data.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [has data]; otherwise, <c>false</c>.
        /// </value>
        public bool HasData => DataFirstLogicalPageId.Value != 0;

        /// <summary>
        /// Gets the table minimum row size.
        /// </summary>
        public ushort MinRowSize
        {
            get
            {
                if (_rowSize == InclusiveRange.Empty)
                {
                    UpdateRowSize();
                }
                return (ushort)_rowSize.Min;
            }
        }

        /// <summary>
        /// Gets the table maximum row size.
        /// </summary>
        public ushort MaxRowSize
        {
            get
            {
                if (_rowSize == InclusiveRange.Empty)
                {
                    UpdateRowSize();
                }
                return (ushort)_rowSize.Max;
            }
        }

        /// <summary>
        /// Gets or sets the lock timeout.
        /// </summary>
        /// <value>The lock timeout.</value>
        public TimeSpan LockTimeout { get; set; }

        /// <summary>
        /// Gets the columns defined on this table.
        /// </summary>
        /// <value>The columns.</value>
        public IList<TableColumnInfo> Columns
        {
            get
            {
                if (_canUpdateSchema)
                {
                    return _updatedColumns;
                }
                return _columns;
            }
        }

        /// <summary>
        /// Gets the column constraints defined on this table.
        /// </summary>
        /// <value>The constraints.</value>
        public IList<RowConstraint> Constraints
        {
            get
            {
                if (_canUpdateSchema)
                {
                    return _updatedConstraints;
                }
                return _constraints;
            }
        }

        /// <summary>
        /// Gets the owner filegroup.
        /// </summary>
        /// <value>The database.</value>
        public FileGroupDevice Owner => _owner;
        #endregion

        #region Internal Properties
        internal IDatabaseLockManager LockingManager => _owner.LifetimeScope.Resolve<IDatabaseLockManager>();
        #endregion

        #region Public Methods
        /// <summary>
        /// Loads the table schema starting from the specified logical id
        /// </summary>
        /// <param name="firstLogicalPageId">The first logical id.</param>
        /// <returns></returns>
        public async Task LoadSchemaAsync(LogicalPageId firstLogicalPageId)
        {
            SchemaFirstLogicalPageId = firstLogicalPageId;
            IsLoading = true;
            BeginColumnUpdate();
            try
            {
                // Assume this is a heap table
                IsHeap = true;

                // Keep loading schema pages (these are linked)
                var logicalId = firstLogicalPageId;
                var isFirstSchemaPage = true;
                while (logicalId != LogicalPageId.Zero)
                {
                    // Prepare page object and load
                    var page = await LoadSchemaPageAsync(isFirstSchemaPage, logicalId).ConfigureAwait(false);

                    // No longer first page so clear now
                    isFirstSchemaPage = false;

                    // Add page information to definition
                    AddTableDefinition(page);

                    // Setup schema last logical id
                    SchemaLastLogicalPageId = page.LogicalPageId;

                    // Advance to next logical page
                    logicalId = page.NextLogicalPageId;
                }
            }
            finally
            {
                await EndColumnUpdate().ConfigureAwait(false);
                IsLoading = false;
            }
        }

        /// <summary>
        /// Begins the column update.
        /// </summary>
        /// <remarks>
        /// This method must be called prior to modification of column
        /// elements or column definitions.
        /// </remarks>
        public void BeginColumnUpdate()
        {
            // Sanity check.
            if (_canUpdateSchema)
            {
                throw new InvalidOperationException("BeginColumnUpdate already called.");
            }

            // Get exclusive schema lock
            var lm = LockingManager;
            lm.LockSchemaAsync(ObjectId, SchemaLockType.SchemaModification, LockTimeout);

            // Copy columns for performing diff checks
            _updatedColumns = new ColumnCollection(this) { SkipInsertChecks = true };
            _updatedConstraints = new ConstraintCollection(this);
            try
            {
                if (_columns != null)
                {
                    foreach (var column in _columns)
                    {
                        _updatedColumns.Add(column);
                    }
                }
                if (_constraints != null)
                {
                    foreach (var constraint in _constraints)
                    {
                        _updatedConstraints.Add(constraint);
                    }
                }
            }
            catch
            {
                _updatedColumns = null;
                _updatedConstraints = null;
                throw;
            }
            finally
            {
                if (_updatedColumns != null)
                {
                    _updatedColumns.SkipInsertChecks = false;
                }
            }
            _canUpdateSchema = true;
        }

        /// <summary>
        /// Adds the column.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="index">The index.</param>
        /// <remarks>
        /// A call to BeginColumnUpdate must occur once before calls to this method.
        /// </remarks>
        public void AddColumn(TableColumnInfo column, int index)
        {
            if (!_canUpdateSchema)
            {
                throw new InvalidOperationException();
            }

            // Add column definition
            if (index == -1)
            {
                _updatedColumns.Add(column);
            }
            else
            {
                _updatedColumns.Insert(index, column);
            }
        }

        /// <summary>
        /// Removes the column.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <remarks>
        /// A call to BeginColumnUpdate must occur once before calls to this method.
        /// </remarks>
        public void RemoveColumn(int index)
        {
            if (!_canUpdateSchema)
            {
                throw new InvalidOperationException();
            }

            var tableColumn = _updatedColumns[index];
            _updatedColumns.RemoveAt(index);
            UpdateRowSize();
            _updatedConstraints.RemoveAllConstraints(tableColumn.Id);

            // Disconnect event handler
            ((INotifyPropertyChanged)tableColumn).PropertyChanged -=
                Column_PropertyChanged;
        }

        /// <summary>
        /// Removes the column.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <remarks>
        /// A call to BeginColumnUpdate must occur once before calls to this method.
        /// </remarks>
        public void RemoveColumn(string name)
        {
            if (!_canUpdateSchema)
            {
                throw new InvalidOperationException();
            }

            for (var index = 0; index < _updatedColumns.Count; ++index)
            {
                if (string.Equals(_updatedColumns[index].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    RemoveColumn(index);
                    break;
                }
            }
        }

        /// <summary>
        /// Adds the constraint.
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        /// <remarks>
        /// A call to BeginColumnUpdate must occur once before calls to this method.
        /// </remarks>
        public void AddConstraint(RowConstraint constraint)
        {
            if (!_canUpdateSchema)
            {
                throw new InvalidOperationException();
            }
            if (_updatedConstraints == null)
            {
                _updatedConstraints = new ConstraintCollection(this);
            }
            _updatedConstraints.Add(constraint);
        }

        /// <summary>
        /// Removes the constraint.
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        /// <remarks>
        /// A call to BeginColumnUpdate must occur once before calls to this method.
        /// </remarks>
        public void RemoveConstraint(RowConstraint constraint)
        {
            if (!_canUpdateSchema)
            {
                throw new InvalidOperationException();
            }
            _updatedConstraints.Remove(constraint);
        }

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
        public async Task EndColumnUpdate()
        {
            // Sanity check.
            if (!_canUpdateSchema)
            {
                throw new InvalidOperationException("EndColumnUpdate already called.");
            }

            _canUpdateSchema = false;
            if (IsNewTable)
            {
                await CreateTableDefinition().ConfigureAwait(false);
            }
            else if (!IsLoading)
            {
                // Check whether columns or constraints have changed (probably)
                if (_columns != null && _updatedColumns != null)
                {
                    // Check whether we need to rewrite table data pages (probably)
                    var needToRewriteTableData = false;
                    if (_columns.Count != _updatedColumns.Count)
                    {
                        needToRewriteTableData = true;
                    }
                    else
                    {
                        // Look for columns that are new or removed
                        // TODO: Look for column changes that would require rewrite of table
                        for (int index = 0; !needToRewriteTableData && index < _columns.Count; ++index)
                        {
                            var oldColumn = _columns[index];
                            var newColumn = _updatedColumns[index];

                            // Rewrite column if any of the following are true:
                            // Column identifiers have changed
                            // Data types have changed
                            // Column length have changed
                            // IsVariableLength property have changed
                            if (oldColumn.Id != newColumn.Id ||
                                oldColumn.DataType != newColumn.DataType ||
                                oldColumn.Length != newColumn.Length ||
                                oldColumn.IsVariableLength != newColumn.IsVariableLength)
                            {
                                needToRewriteTableData = true;
                            }
                        }
                    }

                    // Write new table schema definition (since this may cause new
                    //	table pages we need to do this first)
                    if (needToRewriteTableData)
                    {
                        // Get exclusive lock on table (we should already have this)
                        await _schemaPages[0].SetObjectLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false);

                        // Rewrite table data based on new column layout
                        var request =
                            new RewriteTableRequest(_columns, _updatedColumns, _updatedConstraints);
                        await RewriteTableHandler(request).ConfigureAwait(false);
                    }
                }
                else
                {
                    // This is a new table so we need to create the table 
                    //	definition pages now
                    TableSchemaPage initialPage = null, rootPage = null, prevRootPage = null;
                    var firstPage = true;
                    foreach (var column in _updatedColumns)
                    {
                        var needRetry = false;
                        while (true)
                        {
                            if (rootPage == null)
                            {
                                rootPage = await InitSchemaPageAndLinkAsync(prevRootPage).ConfigureAwait(false);
                                if (firstPage)
                                {
                                    initialPage = (TableSchemaRootPage)rootPage;
                                    firstPage = false;
                                }
                            }
                            try
                            {
                                rootPage.Columns.Add(column);
                                break;
                            }
                            catch (PageException)
                            {
                                // If we already retried then rethrow the error
                                if (needRetry)
                                {
                                    throw;
                                }

                                // Current root page is full so create a new one
                                _schemaPages.Add(rootPage);
                                prevRootPage = rootPage;
                                rootPage = null;
                                needRetry = true;
                            }
                        }
                    }
                    foreach (var constraint in _updatedConstraints)
                    {
                        var needRetry = false;
                        while (true)
                        {
                            if (rootPage == null)
                            {
                                rootPage = await InitSchemaPageAndLinkAsync(prevRootPage).ConfigureAwait(false);
                                if (firstPage)
                                {
                                    initialPage = (TableSchemaRootPage)rootPage;
                                    firstPage = false;
                                }
                            }
                            try
                            {
                                rootPage.Constraints.Add(constraint);
                                break;
                            }
                            catch (PageException)
                            {
                                // If we already retried then rethrow the error
                                if (needRetry)
                                {
                                    throw;
                                }

                                // Current root page is full so create a new one
                                _schemaPages.Add(rootPage);
                                prevRootPage = rootPage;
                                rootPage = null;
                                needRetry = true;
                            }
                        }
                    }
                }
            }

            // Swap column definition lists
            _columns = new ReadOnlyCollection<TableColumnInfo>(_updatedColumns);
            _updatedColumns = null;
            _constraints = new ReadOnlyCollection<RowConstraint>(_updatedConstraints);
            _updatedConstraints = null;
            UpdateRowSize();
        }

        /// <summary>
        /// Creates the specified index on the table and returns the index id.
        /// </summary>
        /// <param name="info">The information.</param>
        public Task<IndexId> CreateIndexAsync(CreateTableIndexParameters info)
        {
            return _lifetimeScope.Resolve<TableIndexManager>().CreateIndexAsync(info);
        }

        /// <summary>
        /// Updates the size of the row.
        /// </summary>
        public void UpdateRowSize()
        {
            _rowSize.Min = 0;
            _rowSize.Max = 0;
            foreach (var column in Columns)
            {
                _rowSize.Min += column.MinDataSize;
                _rowSize.Max += column.MaxDataSize;
            }
            TableSchemaPage temp;
            if (_schemaPages.Count > 0)
            {
                temp = _schemaPages[0];
            }
            else
            {
                temp = new TableSchemaPage();
            }
            _rowsPerPage = (ushort)(temp.DataSize / _rowSize.Max);
        }

        /// <summary>
        /// Searches for a column definition with the given ID.
        /// </summary>
        /// <param name="columnId"></param>
        /// <returns></returns>
        public TableColumnInfo FindColumn(ushort columnId)
        {
            foreach (var column in _columns)
            {
                if (column.Id == columnId)
                {
                    return column;
                }
            }
            return null;
        }

        /// <summary>
        /// Determines the schema changes.
        /// </summary>
        /// <param name="rewriteSchema">if set to <c>true</c> [rewrite schema].</param>
        /// <param name="rewriteTable">if set to <c>true</c> [rewrite table].</param>
        public void DetermineSchemaChanges(bool rewriteSchema, bool rewriteTable)
        {
        }

        /// <summary>
        /// Processes the constraints.
        /// </summary>
        /// <param name="rowData">The row data.</param>
        /// <param name="forInsert">if set to <c>true</c> [for insert].</param>
        public void ProcessConstraints(object[] rowData, bool forInsert)
        {
            if (_canUpdateSchema)
            {
                _updatedConstraints.ProcessConstraints(rowData, forInsert);
            }
            else
            {
            }
        }

        /// <summary>
        /// Adds the row.
        /// </summary>
        /// <param name="columnIDs">The column i ds.</param>
        /// <param name="rowData">The row data.</param>
        /// <returns></returns>
        public async Task AddRow(uint[] columnIDs, object[] rowData)
        {
            // Sanity checks
            if (columnIDs == null)
            {
            }
            if (rowData == null)
            {
            }
            if (columnIDs.Length != rowData.Length)
            {
            }

            // Setup buffer for building row data
            var stream = new MemoryStream();
            var rowWriter = new TableRowWriter(stream, _columns);

            // We need to build a full row
            var columnIdList = columnIDs.ToList();
            for (var columnIndex = 0; columnIndex < _columns.Count; ++columnIndex)
            {
                var column = _columns[columnIndex];

                // If this column is an auto-increment field
                if (column.AutoIncrement)
                {
                    // If caller specified data, we must have identity insert
                    //	switched on
                    object incrValue;
                    if (columnIDs.Any(item => item == column.Id))
                    {
                        if (!AllowIdentityInsert)
                        {
                            throw new ArgumentException("Identity insert is switched off");
                        }

                        // Get value from inputs
                        var index = columnIdList.IndexOf(column.Id);
                        incrValue = rowData[index];
                    }
                    else
                    {
                        // TODO: Determine increment value either by 
                        //	interrogating a system table or by some other means
                        // Will probably use an in-memory increment value tracker
                        //	when table is saved we will take the current value
                        //	and update the table...
                        incrValue = 1;
                    }

                    rowWriter[columnIndex] = incrValue;
                    continue;
                }
                if (column.DataType == TableColumnDataType.Timestamp)
                {
                    // Caller cannot specify timestamp column data
                    if (columnIDs.Any(item => item == column.Id))
                    {
                        throw new ArgumentException("Cannot specify timestamp column data.");
                    }

                    // Add timestamp data
                    rowWriter[columnIndex] = (ulong)1;
                    continue;
                }

                if (!columnIDs.Any(item => item == column.Id))
                {
                    // This column has not been specified, look for default
                    object defaultValue = null;
                    var constraint = _constraints.FirstOrDefault(
                        item => item.ColumnId == column.Id &&
                        item.ConstraintType == RowConstraintType.Default);
                    if (constraint != null)
                    {
                        // TODO: Parse constraint data into appropriate object
                        //	as specified by the column...
                        defaultValue = constraint.ConstraintData;
                    }

                    // If no default found then column must allow nulls
                    if (defaultValue == null && !column.Nullable)
                    {
                        throw new ArgumentException($"No data specified for column {column.Name} which does not allow nulls.");
                    }

                    // Add default to row data
                    rowWriter[columnIndex] = defaultValue;
                    continue;
                }

                // Get specified value for this column
                var dataValue = rowData[columnIdList.IndexOf(column.Id)];
                if (dataValue == null && !column.Nullable)
                {
                    throw new ArgumentException($"No data specified for column {column.Name} which does not allow nulls.");
                }

                // Apply check constraints
                var checkConstraint = _constraints.FirstOrDefault(
                    item => item.ColumnId == column.Id &&
                        item.ConstraintType == RowConstraintType.Check);
                if (checkConstraint != null)
                {
                    // TODO: Execute the check constraint
                    //checkConstraint.Ex
                }

                rowWriter[columnIndex] = dataValue;
            }

            // Determine row size and write row to underlying stream
            var rowSize = rowWriter.RowSize;
            rowWriter.Write();
            var rowBuffer = stream.GetBuffer();

            // If we get this far we have the full row data and all constraints
            //	have been executed.

            // If the table has no logical ids then create first data page
            if (DataFirstLogicalPageId == LogicalPageId.Zero &&
                DataLastLogicalPageId == LogicalPageId.Zero)
            {
                // We need a schema modification lock at this point
                var rootPage = (TableSchemaRootPage)_schemaPages[0];
                await rootPage.SetSchemaLockAsync(SchemaLockType.SchemaModification).ConfigureAwait(false);

                // Create first data page
                var newPage = new TableDataPage();
                newPage.FileGroupId = FileGroupId;
                await newPage.SetObjectLockAsync(ObjectLockType.Exclusive).ConfigureAwait(false);
                await Owner
                    .InitDataPageAsync(new InitDataPageParameters(newPage, true, true, true))
                    .ConfigureAwait(false);

                // Update schema
                DataFirstLogicalPageId = DataLastLogicalPageId = newPage.LogicalPageId;

                // Write first row
                ushort rowIndex;
                newPage.WriteRowIfSpace(0, rowBuffer, rowSize, out rowIndex);

                // TODO: Update indicies
            }
            else
            {
                // TODO: Check all unique indices to ensure this row is unique

                // If the table has a clustered index then do a clustered
                //	insert via the index and copy the row data into the page
                //	given in the index search result

                // If the table has no clustered index we need to add data to the
                //	last page defined for the table

                // In all cases update all other indices
            }
        }

        /// <summary>
        /// Dispose of this instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        #region Internal Methods
        internal void AddTableDefinition(TableSchemaPage page)
        {
            // Only if we don't already have it...
            if (!_schemaPages.Contains(page))
            {
                // Add definition to list
                _schemaPages.Add(page);

                // Process table columns
                _updatedColumns.SkipInsertChecks = true;
                try
                {
                    foreach (var column in page.Columns)
                    {
                        _updatedColumns.Add(column);
                        if (column.Id >= _nextColumnId)
                        {
                            _nextColumnId = (byte)(column.Id + 1);
                        }
                    }
                }
                finally
                {
                    _updatedColumns.SkipInsertChecks = false;
                }

                // Process constraints
                foreach (var constraint in page.Constraints)
                {
                    _updatedConstraints.Add(constraint);
                }

                // Look for clustered index
                var clusteredIndex = page.Indices.FirstOrDefault(item => item.IndexSubType == TableIndexSubType.Clustered);
                if (clusteredIndex != null)
                {
                    if (IsHeap)
                    {
                        IsHeap = false;
                        ClusteredIndex = clusteredIndex;
                    }
                    else
                    {
                        throw new InvalidOperationException("Multiple clustered indicies found on table.");
                    }
                }
            }
        }
        #endregion

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var page in _schemaPages)
                {
                    page.Dispose();
                }

                _schemaPages.Clear();
            }
        }
        #endregion

        #region Private Methods
        private async Task CreateTableDefinition()
        {
            if (!IsNewTable)
            {
                throw new InvalidOperationException("CreateTableDefinition can only be called for new tables.");
            }

            var columnIndex = 0;
            var constraintIndex = 0;
            //var indexIndex = 0;
            TableSchemaPage currentPage = null;
            var needNewPage = true;
            var complete = false;
            while (!complete)
            {
                // Create new page if we need to
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (needNewPage)
                {
                    // Dispose of current page
                    currentPage?.Dispose();

                    // Create new page and link with any current page
                    currentPage = await InitSchemaPageAndLinkAsync(currentPage)
                        .ConfigureAwait(false);
                    _schemaPages.Add(currentPage);
                    needNewPage = false;

                    // Update first logical page id for schema as needed
                    if (SchemaFirstLogicalPageId == LogicalPageId.Zero)
                    {
                        SchemaFirstLogicalPageId = currentPage.LogicalPageId;
                    }

                    // Always update last schema logical page id
                    SchemaLastLogicalPageId = currentPage.LogicalPageId;
                }

                try
                {
                    // Keep writing column information until we run out of 
                    //	columns to write or space in the schema page...
                    while (columnIndex < _updatedColumns.Count)
                    {
                        currentPage.Columns.Add(
                            _updatedColumns[columnIndex]);

                        // If we get this far then there was room on the page
                        //	for the column data and therefore the add succeeded
                        //	so now it is safe to increment the column index...
                        ++columnIndex;
                    }

                    // Keep writing constraint information until we run out of
                    //	constraints to write or space in the page...
                    while (constraintIndex < _updatedConstraints.Count)
                    {
                        currentPage.Constraints.Add(
                            _updatedConstraints[constraintIndex]);

                        // If we get this far then there was room on the page
                        //	for the column data and therefore the add succeeded
                        //	so now it is safe to increment the column index...
                        ++constraintIndex;
                    }

                    // Keep writing index information until we run out of indices
                    //  to write or space in the page...
                    /*while (indexIndex < _indices.Count)
                    {
                        currentPage.Indices.Add(
                            _updatedIndices[indexIndex]);

                        // If we get this far then there was room on the page
                        //	for the column data and therefore the add succeeded
                        //	so now it is safe to increment the column index...
                        ++indexIndex;
                    }*/

                    // If we get this far then we must be complete
                    complete = true;
                }
                catch (PageException)
                {
                    needNewPage = true;
                }
            }

            // Dispose of current page
            currentPage?.Dispose();
        }

        private TableSchemaPage CreateSchemaPage(bool isFirstSchemaPage)
        {
            var schemaPage = isFirstSchemaPage ? new TableSchemaRootPage() : new TableSchemaPage();
            schemaPage.ObjectId = ObjectId;
            schemaPage.FileGroupId = FileGroupId;
            return schemaPage;
        }

        private async Task<TableSchemaPage> InitSchemaPageAsync(bool isFirstSchemaPage)
        {
            var schemaPage = CreateSchemaPage(isFirstSchemaPage);
            schemaPage.ReadOnly = false;
            await _owner
                .InitDataPageAsync(new InitDataPageParameters(schemaPage, true, true, true, true))
                .ConfigureAwait(true);
            return schemaPage;
        }

        private async Task<TableSchemaPage> InitSchemaPageAndLinkAsync(TableSchemaPage prevSchemaPage)
        {
            // Create root page
            var schemaPage = await InitSchemaPageAsync(prevSchemaPage == null)
                .ConfigureAwait(false);

            // Setup pointer to previous table definition page (if any)
            if (prevSchemaPage != null)
            {
                // Hookup linked-list logical id pointers
                schemaPage.PrevLogicalPageId = prevSchemaPage.LogicalPageId;
                prevSchemaPage.NextLogicalPageId = schemaPage.LogicalPageId;

                // Force save of previous page
                prevSchemaPage.Save();
            }

            return schemaPage;
        }

        private async Task<TableSchemaPage> LoadSchemaPageAsync(bool isFirstSchemaPage, LogicalPageId logicalId)
        {
            var schemaPage = CreateSchemaPage(isFirstSchemaPage);
            schemaPage.LogicalPageId = logicalId;

            // Setup page locking and then load page
            await schemaPage.SetObjectLockAsync(ObjectLockType.Shared).ConfigureAwait(false);
            await schemaPage.SetSchemaLockAsync(SchemaLockType.SchemaStability).ConfigureAwait(false);

            await _owner
                .LoadDataPageAsync(new LoadDataPageParameters(schemaPage, false, true))
                .ConfigureAwait(false);
            return schemaPage;
        }

        private void Column_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_canUpdateSchema)
            {
                throw new InvalidOperationException("Columns cannot be updated.");
            }
        }

        private async Task RewriteTableHandler(RewriteTableRequest request)
        {
            try
            {
                TableDataPage currentReadPage = null, currentWritePage = null;
                var readLogicalPageId = DataFirstLogicalPageId;
                uint readRowIndex = 0;
                uint writeRowIndex = 0;
                while (true)
                {
                    // Issue read request as required
                    if (currentReadPage == null)
                    {
                        currentReadPage = new TableDataPage();
                        currentReadPage.LogicalPageId = readLogicalPageId;
                        await _owner
                            .LoadDataPageAsync(new LoadDataPageParameters(currentReadPage, false, true))
                            .ConfigureAwait(false);
                    }

                    // Issue init request as required
                    if (currentWritePage == null)
                    {
                        currentWritePage = new TableDataPage();
                        await _owner
                            .InitDataPageAsync(new InitDataPageParameters(currentWritePage, true, true, true, true))
                            .ConfigureAwait(false);
                    }

                    // Create row reader and row writer objects
                    var rowReader = currentReadPage.GetRowReader(
                        readRowIndex, request.OldColumns);
                    var rowWriter = currentWritePage.GetRowWriter(
                        writeRowIndex, request.NewColumns);

                    // Read next row...
                    var hasWorkToDo = true;
                    while (hasWorkToDo)
                    {
                        rowReader.Read();

                        // Copy data
                        for (var index = 0; index < request.NewColumns.Length; ++index)
                        {
                            // Locate old column
                        }

                        rowWriter.Write();

                        // TODO: Finish implementation
                        hasWorkToDo = false;
                    }

                    // The next table page is held in the page pointers
                    readLogicalPageId = currentReadPage.NextLogicalPageId;
                    if (readLogicalPageId == LogicalPageId.Zero)
                    {
                        // Finished copy
                        break;
                    }
                }

                // TODO: Make copy of data first/last logical ids

                // TODO: Update schema root page with new data first/last logical ids

                // TODO: Delete old table data (walk logical table links from first to last)

                // TODO: Drop old index data for non-clustered indices and rebuild

                // Finally complete request
                request.TrySetResult(null);
            }
            catch (OperationCanceledException)
            {
                request.TrySetCanceled();
            }
            catch (Exception exception)
            {
                request.TrySetException(exception);
            }
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="TransactionContextTaskRequest{Object}" />
    public class RewriteTableRequest : TransactionContextTaskRequest<object>
    {
        #region Private Fields
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initialises an instance of <see cref="T:RewriteTable" />.
        /// </summary>
        public RewriteTableRequest(
            IEnumerable<TableColumnInfo> oldColumns,
            IEnumerable<TableColumnInfo> newColumns,
            IEnumerable<RowConstraint> newConstraints)
        {
            OldColumns = oldColumns.ToArray();
            NewColumns = newColumns.ToArray();
            NewConstraints = newConstraints.ToArray();
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the old columns.
        /// </summary>
        /// <value>
        /// The old columns.
        /// </value>
        public TableColumnInfo[] OldColumns { get; }

        /// <summary>
        /// Gets the new columns.
        /// </summary>
        /// <value>
        /// The new columns.
        /// </value>
        public TableColumnInfo[] NewColumns { get; }

        /// <summary>
        /// Gets the new constraints.
        /// </summary>
        /// <value>
        /// The new constraints.
        /// </value>
        public RowConstraint[] NewConstraints { get; }
        #endregion
    }
}
