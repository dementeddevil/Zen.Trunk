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
        /// <summary>
        /// Initializes a new instance of the <see cref="PrimaryFileGroupDevice"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="name">The name.</param>
        public PrimaryFileGroupDevice(FileGroupId id, string name)
            : base(id, name)
        {
        }

        public override RootPage CreateRootPage()
        {
            return new PrimaryFileGroupRootPage { FileGroupId = FileGroupId };
        }
    }
}
