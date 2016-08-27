namespace Zen.Trunk.Storage.Data
{
	/// <summary>
	/// <c>SecondaryFileGroupDevice</c> represents a secondary file-group.
	/// </summary>
	/// <remarks>
	/// This class uses a <see cref="SecondaryFileGroupRootPage"/> object as the
	/// file-group root page type.
	/// </remarks>
	public class SecondaryFileGroupDevice : FileGroupDevice
	{
		public SecondaryFileGroupDevice(FileGroupId id, string name)
			: base(id, name)
		{
		}

		public override RootPage CreateRootPage(bool isPrimaryFile)
		{
			return base.CreateRootPage(isPrimaryFile);
		}
	}
}
