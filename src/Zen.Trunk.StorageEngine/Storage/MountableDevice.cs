namespace Zen.Trunk.Storage
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// </summary>
	[CLSCompliant(false)]
	public abstract class MountableDevice : TraceableObject, IMountableDevice, IServiceProvider, IDisposable
	{
		#region Private Fields
		private IServiceProvider _parentServiceProvider;
		private int _deviceState = (int)MountableDeviceState.Closed;
		private bool _disposed;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MountableDevice"/> class.
		/// </summary>
		protected MountableDevice()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MountableDevice"/> class.
		/// </summary>
		/// <param name="parentServiceProvider">The parent service provider.</param>
		protected MountableDevice(IServiceProvider parentServiceProvider)
		{
			_parentServiceProvider = parentServiceProvider;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets a value indicating whether this instance is being created.
		/// </summary>
		/// <value><c>true</c> if this instance is create; otherwise, <c>false</c>.</value>
		public bool IsCreate
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the state of the device.
		/// </summary>
		/// <value>The state of the device.</value>
		public MountableDeviceState DeviceState
		{
			get
			{
				return (MountableDeviceState)_deviceState;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Opens this instance.
		/// </summary>
		/// <returns></returns>
		public async Task Open(bool isCreate)
		{
			Tracer.WriteVerboseLine("Open - Enter");
			CheckDisposed();
			MutateStateOrThrow(MountableDeviceState.Closed, MountableDeviceState.Opening);
			try
			{
				IsCreate = isCreate;
				await Task.Run(() => OnOpen()).ConfigureAwait(false);
			}
			catch
			{
				MutateStateOrThrow(MountableDeviceState.Opening, MountableDeviceState.Closed);
				throw;
			}
			finally
			{
				IsCreate = false;
				Tracer.WriteVerboseLine("Open - Exit");
			}
			MutateStateOrThrow(MountableDeviceState.Opening, MountableDeviceState.Open);
		}

		/// <summary>
		/// Closes this instance.
		/// </summary>
		/// <returns></returns>
		public async Task Close()
		{
			Tracer.WriteVerboseLine("Close - Enter");
			CheckDisposed();
			MutateStateOrThrow(MountableDeviceState.Open, MountableDeviceState.Closing);
			try
			{
				await Task.Run(() => OnClose()).ConfigureAwait(false);
			}
			finally
			{
				MutateStateOrThrow(MountableDeviceState.Closing, MountableDeviceState.Closed);
				Tracer.WriteVerboseLine("Close - Exit");
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			DisposeManagedObjects();
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Releases managed resources
		/// </summary>
		protected virtual void DisposeManagedObjects()
		{
			Debug.Assert(
				_deviceState == (int)MountableDeviceState.Closed,
				string.Format(
				"{0} should be closed prior to dispose.", GetType().FullName));
			_disposed = true;
		}

		/// <summary>
		/// Creates the tracer.
		/// </summary>
		/// <param name="tracerName">Name of the tracer.</param>
		/// <returns></returns>
		protected override ITracer CreateTracer(string tracerName)
		{
			return TS.CreatePageDeviceTracer(tracerName);
		}

		/// <summary>
		/// Checks if this instance has been disposed.
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		/// Thrown if this instance has already been disposed.
		/// </exception>
		protected void CheckDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}

		/// <summary>
		/// Called when opening the device.
		/// </summary>
		/// <returns></returns>
		protected virtual Task OnOpen()
		{
			return CompletedTask.Default;
		}

		/// <summary>
		/// Called when closing the device.
		/// </summary>
		/// <returns></returns>
		protected virtual Task OnClose()
		{
			return CompletedTask.Default;
		}

		/// <summary>
		/// Gets the service object of the specified type.
		/// </summary>
		/// <param name="serviceType">An object that specifies the type of service object to get.</param>
		/// <returns>
		/// A service object of type <paramref name="serviceType"/>.-or- null if there is no service object of type <paramref name="serviceType"/>.
		/// </returns>
		protected virtual object GetService(Type serviceType)
		{
			if (serviceType == typeof(IMountableDevice))
			{
				return this;
			}

			if (_parentServiceProvider != null)
			{
				return _parentServiceProvider.GetService(serviceType);
			}

			return null;
		}

		/// <summary>
		/// Gets the service object of the specified type.
		/// </summary>
		/// <typeparam name="T">Type of service object to get</typeparam>
		/// <returns>
		/// A service object of type <typeparamref name="T"/>.
		/// -or- 
		/// null if there is no service object of type <typeparamref name="T"/>.
		/// </returns>
		protected T GetService<T>()
		{
			return (T)GetService(typeof(T));
		}
		#endregion

		#region Private Methods
		private void MutateStateOrThrow(MountableDeviceState currentState, MountableDeviceState newState)
		{
			if (Interlocked.CompareExchange(
				ref _deviceState, (int)newState, (int)currentState) != (int)currentState)
			{
				throw new InvalidOperationException(
					"Buffer device is in unexpected state.");
			}
		}
		#endregion

		#region IServiceProvider Members
		/// <summary>
		/// Gets the service object of the specified type.
		/// </summary>
		/// <param name="serviceType">An object that specifies the type of service object to get.</param>
		/// <returns>
		/// A service object of type <paramref name="serviceType"/>.-or- null if there is no service object of type <paramref name="serviceType"/>.
		/// </returns>
		object IServiceProvider.GetService(Type serviceType)
		{
			return GetService(serviceType);
		}
		#endregion
	}

}
