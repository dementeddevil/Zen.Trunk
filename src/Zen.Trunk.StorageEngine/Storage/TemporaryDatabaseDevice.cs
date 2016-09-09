using System.Threading.Tasks;

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

        /// <summary>
        /// Called when closing the device.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected override async Task OnCloseAsync()
        {
            // Do base class work
            await base.OnCloseAsync().ConfigureAwait(false);

            // Now delete underlying files.
            //this[]
        }
    }
}