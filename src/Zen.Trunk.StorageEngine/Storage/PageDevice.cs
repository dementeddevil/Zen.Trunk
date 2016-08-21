using Autofac;

namespace Zen.Trunk.Storage
{
	public abstract class PageDevice : MountableDevice
	{
		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="PageDevice"/> class.
		/// </summary>
		/// <param name="parentServiceProvider">The parent service provider.</param>
		protected PageDevice(ILifetimeScope parentServiceProvider)
			: base(parentServiceProvider)
		{
		}
		#endregion
	}
}
