namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// Defines locking primatives which apply to database objects.
    /// </summary>
    /// <remarks>
    /// In relation to tables, this lock type applies to the row schema
    /// definition.
    /// In relation to samples, this lock type applies to the media
    /// format information.
    /// </remarks>
    public enum SchemaLockType
    {
        /// <summary>
        /// No locking required.
        /// </summary>
        None = 0,

        /// <summary>
        /// Schema is locked for reading
        /// </summary>
        /// <remarks>
        /// Compatability: BulkUpdate and SchemaStability
        /// </remarks>
        SchemaStability = 1,

        /// <summary>
        /// Schema is locked due to bulk update
        /// </summary>
        /// <remarks>
        /// Compatability: BulkUpdate and SchemaStability
        /// </remarks>
        BulkUpdate = 2,

        /// <summary>
        /// Schema is locked to all.
        /// </summary>
        /// <remarks>
        /// Compatability: None
        /// </remarks>
        SchemaModification = 3,
    }
}