namespace Zen.Trunk.Storage
{
	using System;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using Zen.Trunk.Storage.IO;
	using System.Diagnostics;

	/// <summary>
	/// <b>StatefulBuffer</b> objects are the internal device representation of
	/// a page.
	/// </summary>
	/// <remarks>
	/// <para>
	/// StatefulBuffer wraps <see cref="VirtualBuffer"/> objects to maintain a
	/// consistent presentation state for multiple consumers.
	/// </para>
	/// <para>
	/// <see cref="PageBuffer"/> is derived from this class and implements the
	/// state logic to manage the interaction between database pages, consumers
	/// and the log writer.
	/// </para>
	/// </remarks>
	public abstract class StatefulBuffer : TraceableObject, IDisposable
	{
		#region Internal Objects
		/// <summary>
		/// Base class for State pattern dealing with buffer behaviour.
		/// </summary>
		protected internal abstract class State
		{
			#region Public Constructors
			/// <summary>
			/// Initializes a new instance of the <see cref="State"/> class.
			/// </summary>
			public State()
			{
			}
			#endregion

			#region Public Methods
			public virtual void AddRef(StatefulBuffer instance)
			{
			}

			public virtual void Release(StatefulBuffer instance)
			{
			}

			/// <summary>
			/// Performs state specific buffer deallocation
			/// </summary>
			/// <param name="instance"></param>
			public virtual Task SetFree(StatefulBuffer instance)
			{
				InvalidState();
				return CompletedTask.Default;
			}

			/// <summary>
			/// Performs state specific buffer dirty operation.
			/// </summary>
			/// <param name="instance"></param>
			public virtual Task SetDirty(StatefulBuffer instance)
			{
				InvalidState();
				return CompletedTask.Default;
			}

			/// <summary>
			/// Notifies class that buffer instance has entered state.
			/// </summary>
			/// <param name="buffer"></param>
			/// <param name="lastState"></param>
			public virtual Task OnEnterState(StatefulBuffer instance, State lastState, object userState)
			{
				return CompletedTask.Default;
			}

			/// <summary>
			/// Notifies class that buffer instance is leaving state.
			/// </summary>
			/// <param name="buffer"></param>
			/// <param name="nextState"></param>
			public virtual Task OnLeaveState(StatefulBuffer instance, State nextState, object userState)
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
		private readonly object _syncState = new object();
		private State _currentState;
		private int _refCount;
		private bool _isDisposed; 
		#endregion

		#region Protected Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="StatefulBuffer"/> class.
		/// </summary>
		protected StatefulBuffer()
		{
		} 
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
			System.Threading.Interlocked.Increment(ref _refCount);
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

			System.Threading.Interlocked.Decrement(ref _refCount);
			_currentState.Release(this);
		}

		/// <summary>
		/// Called by page objects when they have finished writing to the
		/// buffer.
		/// </summary>
		public Task SetDirty()
		{
			return _currentState.SetDirty(this);
		}

		/// <summary>
		/// Called by cache manager to free buffer objects.
		/// </summary>
		public Task SetFree()
		{
			return _currentState.SetFree(this);
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
		/// Creates the tracer.
		/// </summary>
		/// <param name="tracerName">Name of the tracer.</param>
		/// <returns></returns>
		protected override ITracer CreateTracer(string tracerName)
		{
			return TS.CreatePageBufferTracer(tracerName);
		}

		/// <summary>
		/// Switches the state of the buffer to the specified state object.
		/// </summary>
		/// <param name="newState">The new state.</param>
		protected async Task SwitchState(State newState, object userState)
		{
			if (_currentState != newState)
			{
				var lockTaken = false;
				//Monitor.TryEnter(_syncState, ref lockTaken);
				//if (lockTaken)
				{
					try
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
									await oldState.OnLeaveState(this, newState, userState);
								}

								// Swap
								_currentState = newState;

								// Notify new buffer state
								if (newState != null)
								{
									// Do not wait for new state work to complete
									await newState.OnEnterState(this, oldState, userState);
								}
							}
							catch
							{
								// In the event of an error; restore previous state
								_currentState = oldState;
								throw;
							}
						}
					}
					finally
					{
						if (lockTaken)
						{
							Monitor.Exit(_syncState);
						}
					}
				}
			}
		} 
		#endregion
	}
}
