namespace Zen.Trunk.Storage.Data
{
	/// <summary>
	/// The <b>DatabaseFileGroupRootPage</b> is the root page class for
	/// file-group devices. It is <b>always</b> positioned at the first logical
	/// page for the file-group that also maps to the first physical page of
	/// the primary device.
	/// The primary role for this page is to enable tracking of file-group
	/// devices. It is assumed that only PhysicalDevice object are used as
	/// child file-group devices as these objects are created during the
	/// mounting process however this behaviour can be overridden by derived
	/// classes.
	/// </summary>
	public class SecondaryFileGroupRootPage : RootPage
	{
		#region Private Fields
	    private const ulong DBSignature = 0x2948f3d3a123e501;
	    private const uint DBSchemaVersion = 0x01000001;
		#endregion

		#region Public Constructors
		public SecondaryFileGroupRootPage (/*DatabaseDevice owner*/)
		//	: base(owner)
		{
		}
		#endregion

		#region Protected Properties
		protected override ulong RootPageSignature => DBSignature;

	    protected override uint RootPageSchemaVersion => DBSchemaVersion;
	    #endregion
	}
}
