namespace Zen.Trunk.Storage.Data.Table
{
    /// <summary>
    /// 
    /// </summary>
    public enum RowConstraintType
    {
        /// <summary>
        /// The default
        /// </summary>
        Default,
        
        /// <summary>
        /// The check
        /// </summary>
        Check,
        
        /// <summary>
        /// The timestamp
        /// </summary>
        Timestamp,

        /// <summary>
        /// The identity
        /// </summary>
        Identity
    }
}