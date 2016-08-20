namespace Zen.Trunk.Storage.Log
{
	using System.Threading.Tasks.Dataflow;

	public class AddLogDeviceParameters : AddDeviceParameters
	{
		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="AddLogDeviceParameters"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="pathName">Name of the path.</param>
		/// <param name="createPageCount">The create page count.</param>
		/// <param name="growthPages">The growth pages.</param>
		/// <param name="updateRootPage">if set to <c>true</c> [update root page].</param>
		/// <param name="deviceId">The device unique identifier.</param>
		public AddLogDeviceParameters(string name, string pathName, uint createPageCount = 0, uint growthPages = 0, bool updateRootPage = false, ushort deviceId = 0)
			: base(name, pathName, createPageCount, deviceId)
		{
			GrowthPages = growthPages;
			UpdateRootPage = updateRootPage;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the growth pages.
		/// </summary>
		/// <value>
		/// The growth pages.
		/// </value>
		public uint GrowthPages
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets a value indicating whether [update root page].
		/// </summary>
		/// <value>
		///   <c>true</c> if [update root page]; otherwise, <c>false</c>.
		/// </value>
		public bool UpdateRootPage
		{
			get;
			private set;
		}
		#endregion
	}

	public class RemoveLogDeviceParameters : RemoveDeviceParameters
	{
		public RemoveLogDeviceParameters(ushort deviceId, bool updateRootPage = false)
			: base(deviceId)
		{
			UpdateRootPage = updateRootPage;
		}

		public RemoveLogDeviceParameters(string name, bool updateRootPage = false)
			: base(name)
		{
			UpdateRootPage = updateRootPage;
		}

		public bool UpdateRootPage
		{
			get;
			private set;
		}
	}
}
