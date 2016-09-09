namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Data.DatabaseDevice" />
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