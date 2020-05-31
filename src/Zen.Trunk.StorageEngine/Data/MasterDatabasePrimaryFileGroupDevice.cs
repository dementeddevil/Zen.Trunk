using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// <c>MasterDatabasePrimaryFileGroupDevice</c> represents the primary 
    /// file-group of the master database.
    /// </summary>
    /// <remarks>
    /// This class uses a <see cref="MasterDatabasePrimaryFileGroupRootPage"/>
    /// object as the file-group root page type.
    /// </remarks>
    public class MasterDatabasePrimaryFileGroupDevice : PrimaryFileGroupDevice
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MasterDatabasePrimaryFileGroupDevice"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="name">The name.</param>
        public MasterDatabasePrimaryFileGroupDevice(FileGroupId id, string name)
            : base(id, name)
        {
        }

        /// <summary>
        /// Creates the root page.
        /// </summary>
        /// <returns></returns>
        public override IRootPage CreateRootPage()
        {
            return new MasterDatabasePrimaryFileGroupRootPage { FileGroupId = FileGroupId };
        }

        /// <summary>
        /// Processes the primary root page during open handling.
        /// </summary>
        /// <param name="rootPage">The root page.</param>
        /// <returns></returns>
        protected override async Task ProcessPrimaryRootPageAsync(PrimaryFileGroupRootPage rootPage)
        {
            await base.ProcessPrimaryRootPageAsync(rootPage);

            if (!(rootPage is MasterDatabasePrimaryFileGroupRootPage masterRootPage))
            {
                return;
            }

            var masterDatabase = GetService<MasterDatabaseDevice>();
            foreach (var databaseInfo in masterRootPage.GetDatabaseEnumerator())
            {
                // TODO: Mount each database
                var attachParameters = new AttachDatabaseParameters(databaseInfo.Name);
                await masterDatabase.AttachDatabaseAsync(attachParameters).ConfigureAwait(false);
            }
        }
    }
}
