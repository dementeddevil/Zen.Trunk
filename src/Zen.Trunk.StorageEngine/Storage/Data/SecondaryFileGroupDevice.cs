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
        /// <summary>
        /// Initializes a new instance of the <see cref="SecondaryFileGroupDevice"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="name">The name.</param>
        public SecondaryFileGroupDevice(FileGroupId id, string name)
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
			return base.CreateRootPage(isPrimaryFile);
		}
	}
}
