using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;

namespace Zen.Trunk.Storage
{
	/// <summary>
	/// </summary>
	[CLSCompliant(false)]
	public abstract class MountableDevice : TraceableObject, IMountableDevice, IDisposable
	{
		#region Private Fields
		private readonly ILifetimeScope _parentServiceProvider;
		private int _deviceState = (int)MountableDeviceState.Closed;
		private bool _disposed;
        private ILifetimeScope _lifetimeScope;
        #endregion

        #region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MountableDevice"/> class.
		/// </summary>
		/// <param name="parentServiceProvider">The parent service provider.</param>
		protected MountableDevice(ILifetimeScope parentServiceProvider)
		{
			_parentServiceProvider = parentServiceProvider;
            InitialiseDeviceLifetimeScope(_parentServiceProvider);
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
		public MountableDeviceState DeviceState => (MountableDeviceState)_deviceState;

	    public ILifetimeScope LifetimeScope => _lifetimeScope;

	    #endregion

		#region Public Methods
		/// <summary>
		/// Opens this instance.
		/// </summary>
		/// <returns></returns>
		public async Task OpenAsync(bool isCreate)
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
		public async Task CloseAsync()
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
			    $"{GetType().FullName} should be closed prior to dispose.");
			_disposed = true;

            _lifetimeScope?.Dispose();
            _lifetimeScope = null;
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

        protected void InitialiseDeviceLifetimeScope(ILifetimeScope parentLifetimeScope)
        {
            _lifetimeScope = parentLifetimeScope.BeginLifetimeScope(BuildDeviceLifetimeScope);
        }

        protected virtual void BuildDeviceLifetimeScope(ContainerBuilder builder)
        {
            builder.RegisterInstance(this)
                .As<IMountableDevice>()
                .As(GetType());
        }

        protected T ResolveDeviceService<T>(params Parameter[] parameters)
        {
            CheckDisposed();
            return _lifetimeScope.Resolve<T>(parameters);
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
	}

}
