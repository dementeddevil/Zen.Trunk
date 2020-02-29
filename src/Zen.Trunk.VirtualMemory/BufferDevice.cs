using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Context;
using Zen.Trunk.Extensions;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="IBufferDevice" />
    public abstract class BufferDevice : IBufferDevice
	{
		#region Private Fields
		private static readonly ILogger Logger = Log.ForContext<BufferDevice>();
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
			using (LogContext.PushProperty("Method", nameof(OpenAsync)))
			{
				CheckDisposed();
				MutateStateOrThrow(MountableDeviceState.Closed, MountableDeviceState.Opening);
				try
				{
					await OnOpenAsync().ConfigureAwait(false);
				}
				catch
				{
					MutateStateOrThrow(MountableDeviceState.Opening, MountableDeviceState.Closed);
					throw;
				}
				MutateStateOrThrow(MountableDeviceState.Opening, MountableDeviceState.Open);
			}
		}

		/// <summary>
		/// Closes this instance.
		/// </summary>
		/// <returns></returns>
		public async Task CloseAsync()
		{
            using (LogContext.PushProperty("Method", nameof(CloseAsync)))
		    {
				CheckDisposed();
		        MutateStateOrThrow(MountableDeviceState.Open, MountableDeviceState.Closing);
		        try
		        {
		            await OnCloseAsync().ConfigureAwait(false);
		        }
		        finally
		        {
		            MutateStateOrThrow(MountableDeviceState.Closing, MountableDeviceState.Closed);
		        }
		    }
		}

	    /// <summary>
	    /// Loads the page data from the physical page into the supplied buffer.
	    /// </summary>
	    /// <param name="pageId">The virtual page identifier.</param>
	    /// <param name="buffer">The buffer.</param>
	    /// <returns>
	    /// A <see cref="Task"/> representing the asynchronous operation.
	    /// </returns>
	    /// <remarks>
	    /// When scatter/gather I/O is enabled then the request is queued until
	    /// the device is flushed.
	    /// </remarks>
	    public abstract Task LoadBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer);

	    /// <summary>
	    /// Saves the page data from the supplied buffer to the physical page.
	    /// </summary>
	    /// <param name="pageId">The virtual page identifier.</param>
	    /// <param name="buffer">The buffer.</param>
	    /// <returns>
	    /// A <see cref="Task"/> representing the asynchronous operation.
	    /// </returns>
	    /// <remarks>
	    /// When scatter/gather I/O is enabled then the request is queued until
	    /// the device is flushed.
	    /// </remarks>
	    public abstract Task SaveBufferAsync(VirtualPageId pageId, IVirtualBuffer buffer);

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
		protected virtual Task OnOpenAsync()
		{
			return CompletedTask.Default;
		}

		/// <summary>
		/// Called when closing the device.
		/// </summary>
		/// <returns></returns>
		protected virtual Task OnCloseAsync()
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
				throw new InvalidOperationException("Buffer device is in unexpected state.");
			}
		}
		#endregion
	}
}
