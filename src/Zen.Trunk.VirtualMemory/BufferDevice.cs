using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;
using Zen.Trunk.Logging;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="IBufferDevice" />
    public abstract class BufferDevice : IBufferDevice
	{
		#region Private Fields
	    private static readonly ILog Logger = LogProvider.For<BufferDevice>();

		private int _deviceState = (int)MountableDeviceState.Closed;
		private bool _disposed;
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
		    if (Logger.IsDebugEnabled())
		    {
		        Logger.Debug("Open - Enter");
		    }

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
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Open - Exit");
                }
            }
            MutateStateOrThrow(MountableDeviceState.Opening, MountableDeviceState.Open);
		}

		/// <summary>
		/// Closes this instance.
		/// </summary>
		/// <returns></returns>
		public async Task CloseAsync()
		{
            if (Logger.IsDebugEnabled())
            {
                Logger.Debug("Close - Enter");
            }
            CheckDisposed();
			MutateStateOrThrow(MountableDeviceState.Open, MountableDeviceState.Closing);
			try
			{
				await OnClose().ConfigureAwait(false);
			}
			finally
			{
				MutateStateOrThrow(MountableDeviceState.Closing, MountableDeviceState.Closed);
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Close - Exit");
                }
            }
        }

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		    GC.SuppressFinalize(this);
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			Debug.Assert(
				_deviceState == (int)MountableDeviceState.Closed,
			    $"{GetType().FullName} should be closed prior to dispose.");
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
