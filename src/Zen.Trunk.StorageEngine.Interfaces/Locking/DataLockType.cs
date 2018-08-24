namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// Defines page level lock types.
    /// </summary>
    public enum DataLockType
    {
        /// <summary>
        /// No locking required.
        /// </summary>
        None = 0,

        /// <summary>
        /// Represents a shared read lock
        /// </summary>
        Shared = 1,

        /// <summary>
        /// Represents an update lock
        /// </summary>
        /// <remarks>
        /// This lock type is not enough to update the page but it is
        /// used to serialise access to the Exclusive lock.
        /// </remarks>
        Update = 2,

        /// <summary>
        /// Represents an exclusive lock
        /// </summary>
        Exclusive = 3,
    }
}