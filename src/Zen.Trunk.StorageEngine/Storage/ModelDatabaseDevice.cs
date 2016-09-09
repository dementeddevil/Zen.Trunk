namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>ModelDatabaseDevice</c> extends database device to provide the
    /// model database.
    /// </summary>
    /// <remarks>
    /// The model database is used as a template database when creating new
    /// database instances and is also used during startup to construct the
    /// temporary database instance.
    /// </remarks>
    /// <seealso cref="DatabaseDevice" />
    public class ModelDatabaseDevice : DatabaseDevice
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelDatabaseDevice"/> class.
        /// </summary>
        public ModelDatabaseDevice()
            : base(DatabaseId.Model)
        {
        }
        #endregion
    }
}