namespace Zen.Trunk.Storage.Data
{
	/// <summary>
	/// <c>PrimaryFileGroupDevice</c> represents a primary file-group.
	/// </summary>
	/// <remarks>
	/// This class uses a <see cref="PrimaryFileGroupRootPage"/> object as the
	/// file-group root page type.
	/// </remarks>
	public class PrimaryFileGroupDevice : FileGroupDevice
	{
		public PrimaryFileGroupDevice(DatabaseDevice owner, FileGroupId id, string name)
			: base(owner, id, name)
		{
		}
	}
}
