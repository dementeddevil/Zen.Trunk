using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;
using Autofac.Core;
using Zen.Trunk.Extensions;
using Zen.Trunk.Logging;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
	/// <summary>
	/// </summary>
	[CLSCompliant(false)]
	public abstract class MountableDevice : IMountableDevice, IDisposable
	{
		#region Private Fields
	    private static readonly ILog Logger = LogProvider.For<MountableDevice>();

		private int _deviceState = (int)MountableDeviceState.Closed;
		private bool _disposed;
	    #endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets a value indicating whether this instance is being created.
		/// </summary>
		/// <value><c>true</c> if this instance is create; otherwise, <c>false</c>.</value>
		public bool IsCreate { get; private set; }

		/// <summary>
		/// Gets the state of the device.
		/// </summary>
		/// <value>The state of the device.</value>
		public MountableDeviceState DeviceState => (MountableDeviceState)_deviceState;

        /// <summary>
        /// Gets the lifetime scope.
        /// </summary>
        /// <value>
        /// The lifetime scope.
        /// </value>
        public ILifetimeScope LifetimeScope { get; private set; }

	    #endregion

        #region Public Methods
        /// <summary>
        /// Initialises the device lifetime scope.
        /// </summary>
        /// <param name="parentLifetimeScope">The parent lifetime scope.</param>
        public void InitialiseDeviceLifetimeScope(ILifetimeScope parentLifetimeScope)
        {
            LifetimeScope = parentLifetimeScope.BeginLifetimeScope(BuildDeviceLifetimeScope);
        }

        /// <summary>
        /// Opens this instance.
        /// </summary>
        /// <returns></returns>
        public async Task OpenAsync(bool isCreate)
		{
		    if (LifetimeScope == null)
		    {
		        throw new InvalidOperationException();    
		    }

		    if (Logger.IsDebugEnabled())
		    {
		        Logger.Debug("Open - Enter");
		    }
			CheckDisposed();
			MutateStateOrThrow(MountableDeviceState.Closed, MountableDeviceState.Opening);
			try
			{
				IsCreate = isCreate;
				await Task.Run(OnOpen).ConfigureAwait(false);
			}
			catch
			{
				MutateStateOrThrow(MountableDeviceState.Opening, MountableDeviceState.Closed);
				throw;
			}
			finally
			{
				IsCreate = false;
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
				await Task.Run(OnClose).ConfigureAwait(false);
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
			DisposeManagedObjects();
		}

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        public void BeginTransaction()
	    {
	        TrunkTransactionContext.BeginTransaction(LifetimeScope);
	    }

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <param name="transactionOptions">The transaction options.</param>
        public void BeginTransaction(TransactionOptions transactionOptions)
        {
            TrunkTransactionContext.BeginTransaction(LifetimeScope, transactionOptions);
        }

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        public void BeginTransaction(TimeSpan timeout)
        {
            TrunkTransactionContext.BeginTransaction(LifetimeScope, timeout);
        }

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <param name="isoLevel">The iso level.</param>
        /// <param name="timeout">The timeout.</param>
        public void BeginTransaction(IsolationLevel isoLevel, TimeSpan timeout)
        {
            TrunkTransactionContext.BeginTransaction(LifetimeScope, isoLevel, timeout);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Hookups the page site.
        /// </summary>
        /// <param name="page">The page.</param>
        protected void HookupPageSite(Page page)
	    {
	        page.SetLifetimeScope(LifetimeScope);
	    }

		/// <summary>
		/// Releases managed resources
		/// </summary>
		protected virtual void DisposeManagedObjects()
		{
			Debug.Assert(
				_deviceState == (int)MountableDeviceState.Closed,
			    $"{GetType().FullName} should be closed prior to dispose.");
			_disposed = true;

            LifetimeScope?.Dispose();
            LifetimeScope = null;
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
        /// Builds the device lifetime scope.
        /// </summary>
        /// <param name="builder">The builder.</param>
        protected virtual void BuildDeviceLifetimeScope(ContainerBuilder builder)
        {
            builder.RegisterInstance(this)
                .As<IMountableDevice>()
                .As(GetType());
            builder.RegisterType<TrunkTransaction>().AsSelf();
        }

        /// <summary>
        /// Resolves the device service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        protected T ResolveDeviceService<T>(params Parameter[] parameters)
        {
            CheckDisposed();
            return LifetimeScope.Resolve<T>(parameters);
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
