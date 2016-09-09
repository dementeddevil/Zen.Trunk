namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DatabaseDevice" />
    public class TemporaryDatabaseDevice : DatabaseDevice
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryDatabaseDevice"/> class.
        /// </summary>
        public TemporaryDatabaseDevice()
            : base(DatabaseId.Temporary)
        {
        }
        #endregion
    }
}