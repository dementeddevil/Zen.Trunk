namespace Zen.Trunk.Storage
{
	using System;
	using System.ComponentModel;

	public abstract class PageDevice : MountableDevice
	{
		#region Private Types
		private class ServiceProviderPageSite : ISite
		{
			#region Private Fields
			private readonly IServiceProvider _parent;
			#endregion

			#region Public Constructors
			/// <summary>
			/// Initializes a new instance of the <see cref="ServiceProviderPageSite"/> class.
			/// </summary>
			/// <param name="parent">The parent.</param>
			public ServiceProviderPageSite(IServiceProvider parent)
			{
				_parent = parent;
			}
			#endregion

			#region ISite Members
			/// <summary>
			/// Gets the component associated with the <see cref="T:System.ComponentModel.ISite" /> when implemented by a class.
			/// </summary>
			/// <returns>The <see cref="T:System.ComponentModel.IComponent" /> instance associated with the <see cref="T:System.ComponentModel.ISite" />.</returns>
			/// <exception cref="System.NotImplementedException"></exception>
			public IComponent Component
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			/// <summary>
			/// Gets the <see cref="T:System.ComponentModel.IContainer" /> associated with the <see cref="T:System.ComponentModel.ISite" /> when implemented by a class.
			/// </summary>
			/// <returns>The <see cref="T:System.ComponentModel.IContainer" /> instance associated with the <see cref="T:System.ComponentModel.ISite" />.</returns>
			/// <exception cref="System.NotImplementedException"></exception>
			public IContainer Container
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			/// <summary>
			/// Determines whether the component is in design mode when implemented by a class.
			/// </summary>
			/// <returns>true if the component is in design mode; otherwise, false.</returns>
			/// <exception cref="System.NotImplementedException"></exception>
			public bool DesignMode
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			/// <summary>
			/// Gets or sets the name of the component associated with the <see cref="T:System.ComponentModel.ISite" /> when implemented by a class.
			/// </summary>
			/// <returns>The name of the component associated with the <see cref="T:System.ComponentModel.ISite" />; or null, if no name is assigned to the component.</returns>
			/// <exception cref="System.NotImplementedException">
			/// </exception>
			public string Name
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}
			#endregion

			#region IServiceProvider Members
			/// <summary>
			/// Gets the service object of the specified type.
			/// </summary>
			/// <param name="serviceType">An object that specifies the type of service object to get.</param>
			/// <returns>
			/// A service object of type <paramref name="serviceType" />.-or- null if there is no service object of type <paramref name="serviceType" />.
			/// </returns>
			public object GetService(Type serviceType)
			{
				return _parent.GetService(serviceType);
			}
			#endregion
		}
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="PageDevice"/> class.
		/// </summary>
		protected PageDevice()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PageDevice"/> class.
		/// </summary>
		/// <param name="parentServiceProvider">The parent service provider.</param>
		protected PageDevice(IServiceProvider parentServiceProvider)
			: base(parentServiceProvider)
		{
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Hookups the page site.
		/// </summary>
		/// <param name="page">The page.</param>
		protected void HookupPageSite(Page page)
		{
			if (page.Site == null)
			{
				page.Site = new ServiceProviderPageSite(this);
			}
		}
		#endregion
	}
}
