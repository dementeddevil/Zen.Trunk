using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Zen.Trunk.Storage.Data;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <b>StatefulBuffer</b> objects are the internal device representation of
    /// a page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// StatefulBuffer wraps <see cref="IVirtualBuffer"/> objects to maintain a
    /// consistent presentation state for multiple consumers.
    /// </para>
    /// <para>
    /// <see cref="PageBuffer"/> is derived from this class and implements the
    /// state logic to manage the interaction between database pages, consumers
    /// and the log writer.
    /// </para>
    /// </remarks>
    public abstract class StatefulBuffer : IDisposable
    {
        #region Internal Objects
        /// <summary>
        /// Base class for State pattern dealing with buffer behaviour.
        /// </summary>
        protected internal abstract class State
        {
            #region Public Methods
            /// <summary>
            /// Performs state specific buffer add-ref logic.
            /// </summary>
            /// <param name="instance"></param>
            public virtual void AddRef(StatefulBuffer instance)
            {
            }

            /// <summary>
            /// Performs state specific buffer release-ref logic.
            /// </summary>
            /// <param name="instance"></param>
            public virtual void Release(StatefulBuffer instance)
            {
            }

            /// <summary>
            /// Performs state specific buffer deallocation
            /// </summary>
            /// <param name="instance"></param>
            public virtual Task SetFreeAsync(StatefulBuffer instance)
            {
                InvalidState();
                return CompletedTask.Default;
            }

            /// <summary>
            /// Performs state specific buffer dirty operation.
            /// </summary>
            /// <param name="instance"></param>
            public virtual Task SetDirtyAsync(StatefulBuffer instance)
            {
                InvalidState();
                return CompletedTask.Default;
            }

            /// <summary>
            /// Notifies class that buffer instance has entered state.
            /// </summary>
            /// <param name="instance"></param>
            /// <param name="lastState"></param>
            /// <param name="userState"></param>
            public virtual Task OnEnterStateAsync(StatefulBuffer instance, State lastState, object userState)
            {
                return CompletedTask.Default;
            }

            /// <summary>
            /// Notifies class that buffer instance is leaving state.
            /// </summary>
            /// <param name="instance"></param>
            /// <param name="nextState"></param>
            /// <param name="userState"></param>
            public virtual Task OnLeaveStateAsync(StatefulBuffer instance, State nextState, object userState)
            {
                return CompletedTask.Default;
            }
            #endregion

            #region Protected Methods
            /// <summary>
            /// Called whenever an invalid state is encountered.
            /// </summary>
            protected void InvalidState()
            {
                throw new InvalidOperationException("Not valid in this state.");
            }
            #endregion
        }
        #endregion

        #region Private Fields
        private readonly SpinLockClass _sync = new SpinLockClass();
        private State _currentState;
        private int _refCount;
        private bool _isDisposed;
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets/sets the page ID associated with this buffer.
        /// </summary>
        /// <value>
        /// <see cref="VirtualPageId"/> representing the associated device and 
        /// physical page.
        /// </value>
        public VirtualPageId PageId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether this buffer can be freed.
        /// </summary>
        public abstract bool CanFree
        {
            get;
            /*{
				return (_state.BufferState == BufferState.Allocated &&
					_refCount == 0 && !_canWriteToDisk);
			}*/
        }

        /// <summary>
        /// Gets a boolean value indicating whether this buffer is dirty.
        /// </summary>
        public bool IsDirty
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <value>The size of the buffer.</value>
        public abstract int BufferSize
        {
            get;
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the current state object.
        /// </summary>
        /// <value>The state of the current.</value>
        protected State CurrentState => _currentState;
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Debug.Assert(_refCount == 0, "Potentially invalid disposal of buffer with reference count > 0.");
            DisposeManagedObjects();
        }

        /// <summary>
        /// Increments the reference count on this buffer object.
        /// </summary>
        public void AddRef()
        {
            // Sanity check
            CheckDisposed();

            // Perform interlocked increment then notify state
            Interlocked.Increment(ref _refCount);
            _currentState.AddRef(this);
        }

        /// <summary>
        /// Decrements the reference count on this buffer object.
        /// </summary>
        /// <remarks>
        /// If the count falls to zero and the page is clean then
        /// the Free event is fired signalling that the buffer is
        /// eligible for reuse.
        /// </remarks>
        public void Release()
        {
            // Sanity check
            CheckDisposed();

            Interlocked.Decrement(ref _refCount);
            _currentState.Release(this);
        }

        /// <summary>
        /// Called by page objects when they have finished writing to the
        /// buffer.
        /// </summary>
        public Task SetDirtyAsync()
        {
            return _currentState.SetDirtyAsync(this);
        }

        /// <summary>
        /// Called by cache manager to free buffer objects.
        /// </summary>
        public Task SetFreeAsync()
        {
            return _currentState.SetFreeAsync(this);
        }

        /// <summary>
        /// Called internally by page objects to retrieve their backing stream.
        /// </summary>
        /// <param name="offset">Start offset of the requested data in bytes.</param>
        /// <param name="count">Number of bytes to make available to the stream.</param>
        /// <param name="writable">if set to <c>true</c> then stream will be 
        /// writable, otherwise; <c>false</c>.</param>
        /// <returns>A <see cref="T:Stream"/> object.</returns>
        public abstract Stream GetBufferStream(int offset, int count, bool writable);
        #endregion

        #region Protected Methods
        /// <summary>
        /// Performs managed disposal of resources.
        /// </summary>
        protected virtual void DisposeManagedObjects()
        {
            _isDisposed = true;
        }

        /// <summary>
        /// Throws an exception if this buffer has been disposed.
        /// </summary>
        protected void CheckDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        /// <summary>
        /// Switches the state of the buffer to the specified state object.
        /// </summary>
        /// <param name="newState">The new state.</param>
        /// <param name="userState">
        /// Optional state information to pass to both state objects.
        /// </param>
        protected async Task SwitchStateAsync(State newState, object userState)
        {
            if (_currentState != newState)
            {
                await _sync
                    .ExecuteAsync(
                        async () =>
                        {
                            if (_currentState != newState)
                            {
                                var oldState = _currentState;
                                try
                                {
                                    // Notify old buffer state
                                    if (oldState != null)
                                    {
                                        // Wait for leave state work to complete
                                        await oldState.OnLeaveStateAsync(this, newState, userState).ConfigureAwait(false);
                                    }

                                    // Swap
                                    _currentState = newState;

                                    // Notify new buffer state
                                    if (newState != null)
                                    {
                                        // Do not wait for new state work to complete
                                        await newState.OnEnterStateAsync(this, oldState, userState).ConfigureAwait(false);
                                    }
                                }
                                catch
                                {
                                    // In the event of an error; restore previous state
                                    _currentState = oldState;
                                    throw;
                                }
                            }
                        })
                    .ConfigureAwait(false);
            }
        }
        #endregion
    }
}
