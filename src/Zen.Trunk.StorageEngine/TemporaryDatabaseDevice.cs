using System.IO;
using System.Threading.Tasks;
using Zen.Trunk.Storage.Logging;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DatabaseDevice" />
    public class TemporaryDatabaseDevice : DatabaseDevice
    {
        private string _tempDataPathname;
        private string _tempLogPathname;

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryDatabaseDevice"/> class.
        /// </summary>
        public TemporaryDatabaseDevice()
            : base(DatabaseId.Temporary)
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates and opens the database
        /// </summary>
        /// <param name="tempFolder">The temporary folder.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task CreateAndOpenAsync(string tempFolder)
        {
            // Setup data and log path names
            _tempDataPathname = Path.Combine(tempFolder, "tempdb.mddf");
            _tempLogPathname = Path.Combine(tempFolder, "tempdb.mlf");

            // Add file-group device
            var addDataFileParams = 
                new AddFileGroupDeviceParameters(
                    FileGroupId.Primary,
                    "PRIMARY",
                    "MASTER",
                    _tempDataPathname,
                    DeviceId.Primary,
                    128);
            await AddFileGroupDeviceAsync(addDataFileParams)
                .ConfigureAwait(false);

            // Add log device
            var addLogFileParams =
                new AddLogDeviceParameters(
                    "PRIMARY",
                    _tempLogPathname,
                    DeviceId.Primary,
                    128,
                    8);
            await AddLogDeviceAsync(addLogFileParams)
                .ConfigureAwait(false);

            // Issue open request now
            await OpenAsync(true).ConfigureAwait(false);
        }
        #endregion

        #region Protected Methods
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
            if (File.Exists(_tempDataPathname))
            {
                File.Delete(_tempDataPathname);
            }
            if (File.Exists(_tempLogPathname))
            {
                File.Delete(_tempLogPathname);
            }
        }
        #endregion
    }
}