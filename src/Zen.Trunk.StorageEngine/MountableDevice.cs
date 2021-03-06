﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;
using Autofac.Core;
using Serilog.Context;
using Zen.Trunk.Extensions;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>MountableDevice</c> defines a device that can support being
    /// mounted and dismounted.
    /// </summary>
    [CLSCompliant(false)]
    public abstract class MountableDevice : IMountableDevice, IDisposable
    {
        #region Private Fields
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
        /// <param name="isCreate">
        /// if set to <c>true</c> then open will be treated as create;
        /// otherwise; <c>false</c>.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task OpenAsync(bool isCreate)
        {
            using (LogContext.PushProperty("Method", nameof(OpenAsync)))
            {
                if (LifetimeScope == null)
                {
                    throw new InvalidOperationException();
                }

                CheckDisposed();
                MutateStateOrThrow(MountableDeviceState.Closed, MountableDeviceState.Opening);
                try
                {
                    IsCreate = isCreate;
                    //await Task.Run(OnOpenAsync).ConfigureAwait(false);
                    await OnOpenAsync().ConfigureAwait(false);
                }
                catch
                {
                    MutateStateOrThrow(MountableDeviceState.Opening, MountableDeviceState.Closed);
                    throw;
                }
                finally
                {
                    IsCreate = false;
                }
                MutateStateOrThrow(MountableDeviceState.Opening, MountableDeviceState.Open);
            }
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task CloseAsync()
        {
            using (LogContext.PushProperty("Method", nameof(CloseAsync)))
            {
                CheckDisposed();
                MutateStateOrThrow(MountableDeviceState.Open, MountableDeviceState.Closing);
                try
                {
                    //await Task.Run(OnCloseAsync).ConfigureAwait(false);
                    await OnCloseAsync().ConfigureAwait(false);
                }
                finally
                {
                    MutateStateOrThrow(MountableDeviceState.Closing, MountableDeviceState.Closed);
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
        /// <param name="isolationLevel">The isolation level.</param>
        public void BeginTransaction(IsolationLevel isolationLevel)
        {
            TrunkTransactionContext.BeginTransaction(LifetimeScope, isolationLevel, TimeSpan.FromSeconds(30));
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
        /// <param name="isolationLevel">The isolation level.</param>
        /// <param name="timeout">The timeout.</param>
        public void BeginTransaction(IsolationLevel isolationLevel, TimeSpan timeout)
        {
            TrunkTransactionContext.BeginTransaction(LifetimeScope, isolationLevel, timeout);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Hookups the page site.
        /// </summary>
        /// <param name="page">The page.</param>
        protected void HookupPageSite(IPage page)
        {
            page.SetLifetimeScope(LifetimeScope);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; 
        /// <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_deviceState != (int)MountableDeviceState.Closed)
                {
                    Serilog.Log.Warning("{DeviceType} should be closed prior to dispose {CurrentState}",
                        GetType().FullName, (MountableDeviceState)_deviceState);
                }

                LifetimeScope?.Dispose();
            }

            LifetimeScope = null;
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
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected virtual Task OnOpenAsync()
        {
            return CompletedTask.Default;
        }

        /// <summary>
        /// Called when closing the device.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected virtual Task OnCloseAsync()
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
        /// <typeparam name="TService">Desired service type.</typeparam>
        /// <param name="parameters">Any necessary constructor parameters.</param>
        /// <returns>
        /// An instance of <typeparamref name="TService"/>.
        /// </returns>
        /// <exception cref="DependencyResolutionException">
        /// Thrown if the service cannot be resolved.
        /// </exception>
        protected TService GetService<TService>(params Parameter[] parameters)
        {
            CheckDisposed();
            return LifetimeScope.Resolve<TService>(parameters);
        }

        /// <summary>
        /// Resolves the device service with the specified name.
        /// </summary>
        /// <typeparam name="TService">Desired service type.</typeparam>
        /// <param name="serviceName">Name of the registered service.</param>
        /// <param name="parameters">Any necessary constructor parameters.</param>
        /// <returns>
        /// An instance of <typeparamref name="TService"/>.
        /// </returns>
        /// <exception cref="DependencyResolutionException">
        /// Thrown if the service cannot be resolved.
        /// </exception>
        protected TService GetService<TService>(string serviceName, params Parameter[] parameters)
        {
            CheckDisposed();
            return LifetimeScope.ResolveNamed<TService>(serviceName, parameters);
        }
        #endregion

        #region Private Methods
        private void MutateStateOrThrow(MountableDeviceState currentState, MountableDeviceState newState)
        {
            if (Interlocked.CompareExchange(
                ref _deviceState, (int)newState, (int)currentState) != (int)currentState)
            {
                throw new InvalidOperationException(
                    $"Buffer device is in unexpected state; cannot transition from {currentState} to {newState}; device is currently {(MountableDeviceState)_deviceState}");
            }
        }
        #endregion
    }

}
