namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// Defines locking primatives which apply to file-group root pages.
    /// </summary>
    public enum FileGroupRootLockType
    {
        /// <summary>
        /// No locking required (illegal)
        /// </summary>
        None = 0,

        /// <summary>
        /// Shared read access to file-group root page
        /// </summary>
        Shared = 1,

        /// <summary>
        /// Update lock - used to serialise access to exclusive state
        /// </summary>
        Update = 2,

        /// <summary>
        /// Exclusive read/write access to file-group page
        /// </summary>
        Exclusive = 3
    }
}