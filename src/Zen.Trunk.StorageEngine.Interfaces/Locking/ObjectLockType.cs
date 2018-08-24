namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// Defines object level lock types.
    /// </summary>
    public enum ObjectLockType
    {
        /// <summary>
        /// No locking required.
        /// </summary>
        None = 0,

        /// <summary>
        /// Transaction intents reading one or more pages owned by this object.
        /// </summary>
        IntentShared = 1,

        /// <summary>
        /// Transaction intends on reading all pages owned by this object.
        /// </summary>
        Shared = 2,

        /// <summary>
        /// Transaction intends to modify one or more (but not all) pages
        /// owned by this object.
        /// </summary>
        IntentExclusive = 3,

        /// <summary>
        /// Transaction has shared and intends to modify one or more pages
        /// owned by this object.
        /// </summary>
        SharedIntentExclusive = 4,

        /// <summary>
        /// Transaction has exclusive use of this object and all pages owned
        /// by it.
        /// </summary>
        Exclusive = 5,
    }
}