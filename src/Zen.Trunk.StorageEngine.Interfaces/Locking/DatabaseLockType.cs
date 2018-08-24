namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// Defines locking primitives which apply to the database itself
    /// </summary>
    public enum DatabaseLockType
    {
        /// <summary>
        /// No locking required (illegal)
        /// </summary>
        None = 0,

        /// <summary>
        /// Shared access to database
        /// </summary>
        Shared = 1,

        /// <summary>
        /// Update lock - used to serialise access to exclusive state
        /// </summary>
        Update = 2,

        /// <summary>
        /// Exclusive read/write access to database
        /// </summary>
        Exclusive = 3
    }
}