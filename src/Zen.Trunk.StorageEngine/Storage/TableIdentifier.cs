namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>TableIdentifier</c> uniquely identifies a table.
    /// </summary>
    /// <remarks>
    /// The table resolver is context-sensitive so that the following defaults
    /// apply;
    /// 1. If the <see cref="DatabaseName"/> is empty then the current database is used.
    /// 2. If the <see cref="SchemaName"/> is empty then the default schema "dbo" is used.
    /// </remarks>
    public struct TableIdentifier
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableIdentifier"/> struct.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="schemaName">Name of the schema.</param>
        /// <param name="tableName">Name of the table.</param>
        public TableIdentifier(string databaseName, string schemaName, string tableName)
        {
            DatabaseName = databaseName;
            SchemaName = schemaName;
            TableName = tableName;
        }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        /// <value>
        /// The name of the database.
        /// </value>
        public string DatabaseName { get; }

        /// <summary>
        /// Gets the name of the schema.
        /// </summary>
        /// <value>
        /// The name of the schema.
        /// </value>
        public string SchemaName { get; }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>
        /// The name of the table.
        /// </value>
        public string TableName { get; }
    }
}
