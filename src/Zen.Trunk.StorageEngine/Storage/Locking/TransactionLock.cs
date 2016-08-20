﻿namespace Zen.Trunk.Storage.Locking
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;

	/// <summary>
	/// Generic transaction-based lock implementation
	/// </summary>
	/// <typeparam name="LockTypeEnum"></typeparam>
	public abstract class TransactionLock<LockTypeEnum> :
		TransactionLockBase, IReferenceLock
		where LockTypeEnum : struct, IComparable, IConvertible, IFormattable // enum
	{
		#region Lock Messages
		#region LockRequestBase
		protected class LockRequestBase : TaskCompletionSource<bool>
		{
			#region Internal Constructors
			internal LockRequestBase(LockTypeEnum lockType, uint transactionId)
			{
				Lock = lockType;
				TransactionId = transactionId;
			}
			#endregion

			#region Public Properties
			internal LockTypeEnum Lock
			{
				get;
				set;
			}
			internal uint TransactionId
			{
				get;
				private set;
			}
			#endregion
		}
		#endregion

		#region AcquireLock
		protected class AcquireLock : LockRequestBase
		{
			#region Internal Constructors
			internal AcquireLock(LockTypeEnum lockType, uint transactionId)
				: base(lockType, transactionId)
			{
			}
			#endregion
		}
		#endregion

		#region ReleaseLock
		protected class ReleaseLock : LockRequestBase
		{
			#region Internal Constructors
			internal ReleaseLock(LockTypeEnum lockType, uint transactionId)
				: base(lockType, transactionId)
			{
			}
			#endregion
		}
		#endregion

		#region QueryLock
		protected class QueryLock : LockRequestBase
		{
			#region Internal Constructors
			internal QueryLock(LockTypeEnum lockType, uint transactionId)
				: base(lockType, transactionId)
			{
			}
			#endregion
		}

		#region DiscardLock
		protected class DiscardLock : LockRequestBase
		{
			#region Internal Constructors
			internal DiscardLock(LockTypeEnum lockType, uint transactionId)
				: base(lockType, transactionId)
			{
			}
			#endregion
		}
		#endregion
		#endregion
		#endregion

		#region Lock State
		protected abstract class State
		{
			/// <summary>
			/// Gets the lock type that this state represents.
			/// </summary>
			/// <value>The lock.</value>
			public abstract LockTypeEnum Lock
			{
				get;
			}

			/// <summary>
			/// Gets an array of lock types that this state is compatable with.
			/// </summary>
			/// <value>The compatable locks.</value>
			public abstract LockTypeEnum[] CompatableLocks
			{
				get;
			}

			/// <summary>
			/// Gets a boolean value indicating whether an exclusive lock can
			/// be acquired from this state.
			/// </summary>
			/// <value>
			/// <c>true</c> if an exclusive lock can be acquired from this
			/// state; otherwise, <c>false</c>. The default is <c>false</c>.
			/// </value>
			public virtual bool CanEnterExclusiveLock => false;

		    /// <summary>
			/// Determines whether the specified lock type is equivalent to an
			/// exclusive lock.
			/// </summary>
			/// <param name="lockType">Type of the lock.</param>
			/// <returns>
			/// <c>true</c> if the lock type is an exclusive lock; otherwise,
			/// <c>false</c>.
			/// </returns>
			public abstract bool IsExclusiveLock(LockTypeEnum lockType);

			/// <summary>
			/// Called when this state is entered.
			/// </summary>
			/// <param name="owner">The owner.</param>
			/// <param name="oldState">The old state.</param>
			public virtual void OnEnterState(TransactionLock<LockTypeEnum> owner, State oldState)
			{
			}

			/// <summary>
			/// Called when this state is exited.
			/// </summary>
			/// <param name="owner">The owner.</param>
			/// <param name="newState">The new state.</param>
			public virtual void OnExitState(TransactionLock<LockTypeEnum> owner, State newState)
			{
			}

			/// <summary>
			/// Determines whether the specified request can acquire the lock.
			/// </summary>
			/// <param name="owner">The owner.</param>
			/// <param name="request">The request.</param>
			/// <returns>
			/// <c>true</c> if the request can acquire lock; otherwise,
			/// <c>false</c>.
			/// </returns>
			public virtual bool CanAcquireLock(TransactionLock<LockTypeEnum> owner, AcquireLock request)
			{
				// We can acquire the lock if any of the following are true;
				//	1. Active request count is zero
				//	2. Active request count is one, the transaction id matches
				//		the request and the requested lock is different to that
				//		currently held.
				//	3. The requested lock is compatable and there are no other
				//		pending requests.
				//	4. The requested lock is an exclusive lock, we can enter an
				//		exclusive lock, the this transaction id is the only
				//		active transaction on the lock
				if ((owner._activeRequests.Count == 0) ||
					/*(owner._activeRequests.Count == 1 &&
					owner._activeRequests.ContainsKey(request.TransactionId) &&
					!owner.IsEquivalentLock(owner._activeRequests[request.TransactionId].Lock, request.Lock)) ||*/
					(owner._pendingRequests.Count == 0 && IsLockCompatable(request)) ||
					(owner._activeRequests.Count == 1 && owner._activeRequests.ContainsKey(request.TransactionId) && CanEnterExclusiveLock && IsExclusiveLock(request.Lock)))
				{
					return true;
				}

				return false;
			}

			/// <summary>
			/// Determines whether the lock type specified in the request is
			/// compatable with this lock state.
			/// </summary>
			/// <param name="request">The request.</param>
			/// <returns>
			/// <c>true</c> if the specified request is compatable; otherwise,
			/// <c>false</c>.
			/// </returns>
			public bool IsLockCompatable(AcquireLock request)
			{
				if (CompatableLocks.Length > 0)
				{
					foreach (var lockType in CompatableLocks)
					{
						if (Convert.ToInt32(lockType) == Convert.ToInt32(request.Lock))
						{
							return true;
						}
					}
				}
				return false;
			}
		}
		#endregion

		#region Private Fields
		private ConcurrentExclusiveSchedulerPair _taskInterleave;
		private ActionBlock<AcquireLock> _acquireLockAction;
		private ActionBlock<ReleaseLock> _releaseLockAction;
		private ActionBlock<QueryLock> _queryLockAction;
		private string _id;
		private int _referenceCount = 0;
		private bool _initialised;
		private readonly Dictionary<uint, AcquireLock> _activeRequests = new Dictionary<uint, AcquireLock>();
		private AcquireLock _pendingExclusiveRequest;
		private readonly Queue<AcquireLock> _pendingRequests = new Queue<AcquireLock>();
		private State _currentState;
		#endregion

		#region Public Events
		/// <summary>
		/// Fired prior to the final disposal of the transaction lock.
		/// </summary>
		public event EventHandler FinalRelease;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TransactionLock&lt;LockTypeEnum&gt;"/> class.
		/// </summary>
		public TransactionLock()
		{
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets/sets the lock Id.
		/// </summary>
		public string Id
		{
			get
			{
				return _id;
			}
			set
			{
				_id = value;
			}
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets enumeration value for the lock representing the "none" lock.
		/// </summary>
		/// <value>The type of the none lock.</value>
		protected abstract LockTypeEnum NoneLockType
		{
			get;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Initialises the lock object.
		/// </summary>
		public void Initialise()
		{
			if (_initialised)
			{
				throw new InvalidOperationException("Lock already initialised.");
			}

			// Setup initial state object
			_currentState = GetStateFromType(NoneLockType);
			_currentState.OnEnterState(this, null);

			// Initialise receiver arbiters
			_taskInterleave = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default);
			_acquireLockAction = new ActionBlock<AcquireLock>(
				(request) =>
				{
					OnAcquireLock(request);
				},
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ExclusiveScheduler
				});
			_releaseLockAction = new ActionBlock<ReleaseLock>(
				(request) =>
				{
					OnReleaseLock(request);
				},
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ExclusiveScheduler
				});
			_queryLockAction = new ActionBlock<QueryLock>(
				(request) =>
				{
					OnQueryLock(request);
				},
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = _taskInterleave.ConcurrentScheduler
				});

			_initialised = true;
			TraceVerbose("Initialized");
		}

		/// <summary>
		/// Traces the state of the lock.
		/// </summary>
		/// <param name="message">The message.</param>
		public void TraceLockState(string message)
		{
#if TRACE
			TraceVerbose("{0} {1:X8} {2}",
				new object[]
				{
					message,
					Thread.CurrentThread.GetHashCode (),
					Thread.CurrentThread.Name 
				});
#endif
		}

		/// <summary>
		/// Increases the reference count on this lock object.
		/// </summary>
		public void AddRefLock()
		{
			Interlocked.Increment(ref _referenceCount);
		}

		/// <summary>
		/// Decreases the reference count on this lock object.
		/// </summary>
		/// <returns>Returns true if this is the final release.</returns>
		public bool ReleaseRefLock()
		{
			if (Interlocked.Decrement(ref _referenceCount) == 0)
			{
				OnFinalRelease();
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Determines whether the current transaction has the given lock type.
		/// </summary>
		/// <param name="lockType"></param>
		/// <returns>Boolean. True indicates lock is held.</returns>
		/// <remarks>
		/// This method will throw if the current thread does not have a
		/// transaction context.
		/// </remarks>
		public bool HasLock(LockTypeEnum lockType)
		{
			// Retrieve connection id and create request object.
			// Will throw if no connection information is available for the
			//  calling thread.
			var transactionId = GetThreadTransactionId(true);
			return HasLock(transactionId, lockType);
		}

		/// <summary>
		/// Acquires the specified lock type.
		/// </summary>
		/// <param name="lockType"></param>
		/// <param name="timeout"></param>
		/// <remarks>
		/// <para>
		/// This method will throw if the current thread does not have a
		/// transaction context.
		/// </para>
		/// <para>
		/// This method will throw if the lock is not acquired within the
		/// specified timeout period.
		/// </para>
		/// </remarks>
		public void Lock(LockTypeEnum lockType, TimeSpan timeout)
		{
			// Retrieve connection id and create request object.
			// Will throw if no connection information is available for the
			//  calling thread.
			var transactionId = GetThreadTransactionId(true);
			Lock(transactionId, lockType, timeout);
		}

		/// <summary>
		/// Releases the lock by downgrading to the none lock state
		/// </summary>
		public void Unlock()
		{
			Unlock(NoneLockType);
		}

		/// <summary>
		/// Releases the lock by downgrading to the given lock type.
		/// </summary>
		/// <param name="newLockType"></param>
		/// <remarks>
		/// The new lock type should be of a lower state than the
		/// current lock type or this method may throw an exception.
		/// The lock timeout period is fixed at 10 seconds so get it
		/// right!
		/// </remarks>
		public void Unlock(LockTypeEnum newLockType)
		{
			// Retrieve connection id and create request object.
			// Will throw if no connection information is available for the
			//  calling thread.
			var transactionId = GetThreadTransactionId(true);
			Unlock(transactionId, newLockType);
		}
		#endregion

		#region Internal Methods
		internal void Lock(uint transactionId, LockTypeEnum lockType, TimeSpan timeout)
		{
			// Post lock acquisition message
			var request = new AcquireLock(lockType, transactionId);
			_acquireLockAction.Post(request);
			if (!request.Task.Wait(timeout))
			{
				request.TrySetResult(false);
				throw new TimeoutException("Acquire lock timeout.");
			}
			if (request.Task.IsFaulted)
			{
				// Rethrow exception here
				throw request.Task.Exception;
			}

			// Increment reference count on lock
			AddRefLock();
		}

		internal void Unlock(uint transactionId, LockTypeEnum newLockType)
		{
			// Post lock acquisition message
			var request = new ReleaseLock(newLockType, transactionId);
			_releaseLockAction.Post(request);
			request.Task.Wait();
			if (request.Task.IsFaulted)
			{
				throw request.Task.Exception;
			}

			// Release reference count on lock
			ReleaseRefLock();
		}
		#endregion

		#region Protected Methods
#if TRACE
		protected override string GetTracePrefix()
		{
			return $"{base.GetTracePrefix()} ID: {_id} Rc: {_referenceCount} Txn:{GetThreadTransactionId(false)}";
		}
#endif

		protected internal bool HasLock(uint transactionId, LockTypeEnum lockType)
		{
			// Post lock acquisition message
			var request = new QueryLock(lockType, transactionId);
			_queryLockAction.Post(request);
			request.Task.Wait();
			return request.Task.Result;
		}

		/// <summary>
		/// When overridden by derived class, gets the state object from
		/// the specified state type.
		/// </summary>
		/// <param name="lockType">Type of the lock.</param>
		/// <returns></returns>
		protected abstract State GetStateFromType(LockTypeEnum lockType);

		/// <summary>
		/// Called when last reference to the lock is released.
		/// </summary>
		protected virtual void OnFinalRelease()
		{
			if (FinalRelease != null)
			{
				FinalRelease(this, new EventArgs());
			}
		}

		protected void OnAcquireLock(AcquireLock request)
		{
			if (!OnQueryAcquireLock(request))
			{
				if (_currentState.IsExclusiveLock(request.Lock))
				{
					// If we cannot enter exclusive lock from this state
					//	or
					// If the active request list does not contain this transaction
					//	or
					// If the active request for this transaction does not match the
					//	current state lock
					// then we cannot enter exclusive state from this transaction
					if (!_currentState.CanEnterExclusiveLock ||
						!_activeRequests.ContainsKey(request.TransactionId) ||
						!IsEquivalentLock(_activeRequests[request.TransactionId].Lock, _currentState.Lock))
					{
						request.TrySetException(new LockException(
							"Cannot enter exclusive lock from current lock state."));
						return;
					}

					// Save the request (we will try to enter exclusive mode
					//	all other requests have been released)
					System.Diagnostics.Debug.Assert(_pendingExclusiveRequest == null);
					_pendingExclusiveRequest = request;
				}
				else
				{
					// Ignore requests for downgraded lock here
					if (_activeRequests.ContainsKey(request.TransactionId) &&
						IsDowngradedLock(_activeRequests[request.TransactionId].Lock, request.Lock))
					{
						// Simply mark the request as satisfied but to not
						//	add to the list of active requests
						request.TrySetResult(false);
						return;
					}

					// We can upgrade the lock only if there are no pending
					//	requests
					if (_activeRequests.Count == 1 &&
						_activeRequests.ContainsKey(request.TransactionId) &&
						_pendingRequests.Count == 0 &&
						_pendingExclusiveRequest == null)
					{
						// Replace the active request and return
						_activeRequests[request.TransactionId] = request;
						request.TrySetResult(false);
						return;
					}

					// Any lock acquisition request not satisfied by
					//	the current state object must be queued.
					_pendingRequests.Enqueue(request);
				}
			}
			else
			{
				try
				{
					// Acquire the lock
					var result = false;
					if (_activeRequests.ContainsKey(request.TransactionId))
					{
						_activeRequests[request.TransactionId] = request;
					}
					else
					{
						_activeRequests.Add(request.TransactionId, request);
						result = true;
					}

					// Lock acquired.
					request.TrySetResult(result);
				}
				finally
				{
					UpdateActiveLockState();
				}
			}
		}

		/// <summary>
		/// Called to determine whether the lock can be acquired by the
		/// specified <see cref="T:AcquireLock"/> request.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <returns></returns>
		protected virtual bool OnQueryAcquireLock(AcquireLock request)
		{
			return _currentState.CanAcquireLock(this, request);
		}

		/// <summary>
		/// Called to remove or downgrade the lock state for the transaction
		/// associated with the specified <see cref="T:ReleaseLock"/> request.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <returns></returns>
		protected virtual void OnReleaseLock(ReleaseLock request)
		{
			try
			{
				if (!_activeRequests.ContainsKey(request.TransactionId))
				{
					//throw new InvalidOperationException("Lock not held by transaction.");
					TraceVerbose("Release lock called when lock not held.");
				}
				else
				{
					if (IsEquivalentLock(request.Lock, NoneLockType))
					{
						_activeRequests.Remove(request.TransactionId);
						if (_activeRequests.Count == 0)
						{
							UpdateActiveLockState();
						}
					}
					else if (!IsDowngradedLock(_activeRequests[request.TransactionId].Lock, request.Lock))
					{
						throw new InvalidOperationException(
							"New lock type is not downgrade of current lock.");
					}
					else
					{
						_activeRequests[request.TransactionId].Lock = request.Lock;
					}
				}
				request.TrySetResult(true);
			}
			catch (Exception e)
			{
				request.TrySetException(e);
			}
			finally
			{
				ReleaseWaitingRequests();
			}
		}

		/// <summary>
		/// Called to determine whether the transaction associated with the
		/// specified <see cref="T:QueryLock"/> has the specified locking
		/// mode.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <returns></returns>
		protected virtual void OnQueryLock(QueryLock request)
		{
			if (!_activeRequests.ContainsKey(request.TransactionId))
			{
				// Current transaction is not in active list therefore
				//	lock is not held
				request.TrySetResult(false);
			}
			else if (IsEquivalentLock(request.Lock, NoneLockType))
			{
				// Using the NoneLockType means check if we have any kind
				//	of lock; since transaction id is in the active list
				//	this must be the case...
				request.TrySetResult(true);
			}
			else if (IsEquivalentLock(_currentState.Lock, request.Lock) ||
				IsDowngradedLock(_currentState.Lock, request.Lock))
			{
				// Current lock state is the same as or superior to the 
				//	request lock so lock must be held
				request.TrySetResult(true);
			}
			else
			{
				// If we get to this point then we don't have the lock
				request.TrySetResult(false);
			}
		}

		/// <summary>
		/// Gets the current <typeparamref name="LockTypeEnum"/> that
		/// represents the current state of the lock.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// The current lock state is determined by the <see cref="T:AcquireLock"/>
		/// objects in the active request list.
		/// </remarks>
		protected LockTypeEnum GetActiveLockType()
		{
			var lockType = NoneLockType;
			foreach (var request in _activeRequests.Values)
			{
				var conv = (IConvertible)request.Lock;
				if (Convert.ToInt32(request.Lock) > Convert.ToInt32(lockType))
				{
					lockType = request.Lock;
				}
			}
			return lockType;
		}

		protected void SetActiveRequest(AcquireLock request)
		{
			_activeRequests.Add(request.TransactionId, request);
			if (!request.TrySetResult(true))
			{
				_activeRequests.Remove(request.TransactionId);
			}
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Gets the transaction ID from the thread context.
		/// </summary>
		/// <remarks>
		/// If throwIfMissing is set to true then this method will throw
		/// a <see cref="LockException"/> if the
		/// caller does not have transaction info.
		/// If throwIfMissing is set to false then this method will return
		/// zero if the caller does not have transaction info.
		/// </remarks>
		/// <param name="throwIfMissing"></param>
		/// <returns>Transaction ID or zero if none.</returns>
		private uint GetThreadTransactionId(bool throwIfMissing)
		{
			uint transactionId = 0;
			if (TrunkTransactionContext.Current != null)
			{
				transactionId = TrunkTransactionContext.Current.TransactionId;
			}
			else if (throwIfMissing)
			{
				throw new LockException("Current thread has no transaction context!");
			}
			return transactionId;
		}

		private void UpdateActiveLockState()
		{
			var lockType = GetActiveLockType();
			var newState = GetStateFromType(lockType);
			if (_currentState != newState)
			{
				var oldState = _currentState;
				if (oldState != null)
				{
					oldState.OnExitState(this, newState);
				}
				_currentState = newState;
				if (newState != null)
				{
					newState.OnEnterState(this, oldState);
				}
			}
		}

		private void ReleaseWaitingRequests()
		{
			UpdateActiveLockState();

			if (_pendingExclusiveRequest != null)
			{
				if (!ReleaseWaitingRequest(_pendingExclusiveRequest))
				{
					return;
				}

				// Discard exclusive request and update state
				_pendingExclusiveRequest = null;
				UpdateActiveLockState();
			}

			while (_pendingRequests.Count > 0)
			{
				// Attempt to release the next request from the queue
				var queuedRequest = _pendingRequests.Peek();
				if (!ReleaseWaitingRequest(queuedRequest))
				{
					break;
				}

				// Remove element from queue and update active lock state
				_pendingRequests.Dequeue();
				UpdateActiveLockState();
			}
		}

		private bool ReleaseWaitingRequest(AcquireLock request)
		{
			// Skip aborted requests
			if (request.Task.IsCanceled ||
				request.Task.IsCompleted ||
				request.Task.IsFaulted)
			{
				return true;
			}

			// If this lock is compatable then add to active list
			//	and release waiting request.
			if (_currentState.CanAcquireLock(this, request))
			{
				SetActiveRequest(request);
				return true;
			}

			// Incompatable lock.
			return false;
		}

		private bool IsDowngradedLock(LockTypeEnum currentLock, LockTypeEnum newLock)
		{
			return (Convert.ToInt32(currentLock) > Convert.ToInt32(newLock));
		}

		private bool IsEquivalentLock(LockTypeEnum currentLock, LockTypeEnum newLock)
		{
			return (Convert.ToInt32(currentLock) == Convert.ToInt32(newLock));
		}
		#endregion

		#region IReferenceLock Members
		void IReferenceLock.AddRefLock()
		{
			this.AddRefLock();
		}

		void IReferenceLock.ReleaseLock()
		{
			this.ReleaseRefLock();
		}
		#endregion
	}
}
