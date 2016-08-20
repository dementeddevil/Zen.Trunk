namespace Zen.Trunk.Storage
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;

	[CLSCompliant(false)]
	public abstract class BufferDevice : TraceableObject, IBufferDevice
	{
		#region Private Fields
		private int _deviceState = (int)MountableDeviceState.Closed;
		private bool _disposed;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="BufferDevice"/> class.
		/// </summary>
		protected BufferDevice()
		{
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the buffer factory.
		/// </summary>
		/// <value>
		/// The buffer factory.
		/// </value>
		public abstract IVirtualBufferFactory BufferFactory
		{
			get;
		}

		/// <summary>
		/// Gets the state of the device.
		/// </summary>
		/// <value>The state of the device.</value>
		public MountableDeviceState DeviceState => (MountableDeviceState)_deviceState;

	    #endregion

		#region Public Methods
		/// <summary>
		/// Opens this instance.
		/// </summary>
		/// <returns></returns>
		public async Task OpenAsync()
		{
			Tracer.WriteVerboseLine("Open - Enter");
			CheckDisposed();
			MutateStateOrThrow(MountableDeviceState.Closed, MountableDeviceState.Opening);
			try
			{
				await OnOpen().ConfigureAwait(false);
			}
			catch
			{
				MutateStateOrThrow(MountableDeviceState.Opening, MountableDeviceState.Closed);
				throw;
			}
			finally
			{
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
				await OnClose().ConfigureAwait(false);
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
		/// Releases unmanaged and - optionally - managed resources
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
