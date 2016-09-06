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
	public class MasterDatabasePrimaryFileGroupDevice : FileGroupDevice
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
        /// <param name="isPrimaryFile">if set to <c>true</c> [is primary file].</param>
        /// <returns></returns>
        public override RootPage CreateRootPage(bool isPrimaryFile)
		{
			if (isPrimaryFile)
			{
				return new MasterDatabasePrimaryFileGroupRootPage();
			}

			return base.CreateRootPage(isPrimaryFile);
		}
	}
}
