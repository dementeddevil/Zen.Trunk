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
		public MasterDatabasePrimaryFileGroupDevice(FileGroupId id, string name)
			: base(id, name)
		{
		}

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
