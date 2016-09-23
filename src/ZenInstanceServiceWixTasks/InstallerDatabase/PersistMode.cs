namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase
{
    public enum PersistMode
	{
		ReadOnly = 0,
		Transact = 1,
		Direct = 2,
		Create = 3,
		CreateDirect = 4,
		Patch = 8,
	}
}
