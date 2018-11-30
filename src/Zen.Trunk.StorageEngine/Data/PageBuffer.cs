using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;
using Zen.Trunk.Logging;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
	/// <summary>
	/// <c>PageBuffer</c> extends <see cref="StatefulBuffer"/> to provide state
	/// management for database pages.
	/// </summary>
	/// <remarks>
	/// State support for database pages requires the following;
	/// 1. Separation of readers and writers where appropriate
	/// 2. Delay writing until log-writer has written change information
	/// 3. Load and save of data page to the underlying device
	/// </remarks>
	public sealed class PageBuffer : StatefulBuffer, IPageEnlistmentNotification
	{
        /// <summary>
        /// Tracks the state of a page-buffer within the page-buffer lifecycle
        /// </summary>
        public enum PageBufferStateType
		{
			/// <summary>
			/// Buffer is free.
			/// </summary>
			/// <remarks>
			/// <para>
			/// Entry states
			/// (Init state)
			/// Allocated
			/// </para>
			/// <para>
			/// Exit states
			/// Load
			/// Allocated
			/// </para>
			/// </remarks>
			Free,

			/// <summary>
			/// Buffer load has been requested.
			/// </summary>
			/// <remarks>
			/// <para>
			/// Entry states
			/// Free
			/// </para>
			/// <para>
			/// Exit states
			/// Load
			/// </para>
			/// </remarks>
			PendingLoad,

			/// <summary>
			/// Buffer is being loaded from disk.
			/// </summary>
			/// <remarks>
			/// <para>
			/// Entry states
			/// PendingLoad
			/// </para>
			/// <para>
			/// Exit states
			/// Allocated
			/// </para>
			/// </remarks>
			Load,

			/// <summary>
			/// Buffer has been allocated.
			/// </summary>
			/// <remarks>
			/// <para>
			/// Entry states
			/// Free
			/// Load
			/// </para>
			/// <para>
			/// Exit states
			/// Free
			/// Dirty
			/// </para>
			/// </remarks>
			Allocated,

			/// <summary>
			/// Buffer contains dirty (unwritten) data.
			/// </summary>
			/// <remarks>
			/// <para>
			/// Entry states
			/// Allocated
			/// AllocatedWritable
			/// </para>
			/// <para>
			/// Exit states
			/// Allocated
			/// AllocatedWritable
			/// Log
			/// </para>
			/// </remarks>
			Dirty,

			/// <summary>
			/// Buffer is logging changes to the transaction log.
			/// </summary>
			/// <remarks>
			/// <para>
			/// Entry states
			/// Dirty
			/// </para>
			/// <para>
			/// Exit states
			/// AllocatedWritable
			/// </para>
			/// </remarks>
			Log,

			/// <summary>
			/// Buffer has been allocated and has changes that must be written
			/// to storage.
			/// </summary>
			/// <remarks>
			/// <para>
			/// Entry states
			/// Log
			/// </para>
			/// <para>
			/// Exit states
			/// Dirty
			/// Write
			/// </para>
			/// </remarks>
			AllocatedWritable,
		}

		#region Internal Types
		private static class PageBufferStateFactory
		{
			private static readonly State FreeStateObject = new FreeState();
			private static readonly State LoadStateObject = new LoadState();
			private static readonly State PendingLoadStateObject = new PendingLoadState();
			private static readonly State AllocatedStateObject = new AllocatedState();
			private static readonly State DirtyStateObject = new DirtyState();
			private static readonly State LogStateObject = new LogState();
			private static readonly State AllocatedWritableStateObject = new AllocatedWritableState();

			public static State GetState(PageBufferStateType state)
			{
				switch (state)
				{
					case PageBufferStateType.Free:
						return FreeStateObject;

                    case PageBufferStateType.Load:
						return LoadStateObject;

                    case PageBufferStateType.PendingLoad:
						return PendingLoadStateObject;

                    case PageBufferStateType.Allocated:
						return AllocatedStateObject;

                    case PageBufferStateType.Dirty:
						return DirtyStateObject;

                    case PageBufferStateType.Log:
						return LogStateObject;

                    case PageBufferStateType.AllocatedWritable:
						return AllocatedWritableStateObject;

                    default:
						throw new InvalidOperationException("Unknown page buffer state.");
				}
			}
		}

		private abstract class PageBufferState : State
		{
			public abstract PageBufferStateType StateType
			{
				get;
			}

			public virtual Task Init(
				PageBuffer instance, VirtualPageId pageId, LogicalPageId logicalId)
			{
				InvalidState();
				return CompletedTask.Default;
			}

			public virtual Task RequestLoad(
				PageBuffer instance, VirtualPageId pageId, LogicalPageId logicalId)
			{
				InvalidState();
				return CompletedTask.Default;
			}

			public virtual Task Load(PageBuffer instance)
			{
				InvalidState();
				return CompletedTask.Default;
			}

			public virtual Task Save(PageBuffer instance)
			{
				InvalidState();
				return CompletedTask.Default;
			}

			public virtual Task PrepareForCommit(
				PageBuffer instance, long timestamp)
			{
				InvalidState();
				return CompletedTask.Default;
			}

			public virtual Task Commit(
				PageBuffer instance, long timestamp)
			{
				InvalidState();
				return CompletedTask.Default;
			}

			public virtual Task Rollback(
				PageBuffer instance)
			{
				InvalidState();
				return CompletedTask.Default;
			}
		}

		private class FreeState : PageBufferState
		{
			public override PageBufferStateType StateType => PageBufferStateType.Free;

		    public override Task SetFreeAsync(StatefulBuffer instance)
			{
				return CompletedTask.Default;
			}

			public override Task Init(
				PageBuffer instance, VirtualPageId pageId, LogicalPageId logicalId)
			{
				instance.PageId = pageId;
				instance.LogicalPageId = logicalId;
				instance.IsNew = true;
				instance.EnsureNewBufferAllocated();

				// Switch to allocated state
				return instance.SwitchStateAsync(PageBufferStateType.Allocated);
			}

			public override Task RequestLoad(PageBuffer instance, VirtualPageId pageId, LogicalPageId logicalId)
			{
				instance.PageId = pageId;
				instance.LogicalPageId = logicalId;

				// Switch to load state
				return instance.SwitchStateAsync(PageBufferStateType.PendingLoad);
			}
		}

		private class PendingLoadState : PageBufferState
		{
			public override PageBufferStateType StateType => PageBufferStateType.PendingLoad;

		    public override Task Load(PageBuffer instance)
			{
				// Allocate the buffer if required and switch state
				instance.EnsureNewBufferAllocated();
				return instance.SwitchStateAsync(PageBufferStateType.Load);
			}
		}

		private class LoadState : PageBufferState
		{
			public override PageBufferStateType StateType => PageBufferStateType.Load;

		    public override async Task OnEnterStateAsync(StatefulBuffer instance, State lastState, object userState)
			{
				var pageBuffer = (PageBuffer)instance;
				await pageBuffer
					.LoadNewBufferAsync()
					.ConfigureAwait(false);

				await pageBuffer
					.SwitchStateAsync(PageBufferStateType.Allocated)
					.ConfigureAwait(false);
			}
		}

		private class AllocatedState : PageBufferState
		{
			public override PageBufferStateType StateType => PageBufferStateType.Allocated;

		    public override Task SetFreeAsync(StatefulBuffer instance)
			{
				return ((PageBuffer)instance).SwitchStateAsync(PageBufferStateType.Free);
			}

			public override Task SetDirtyAsync(StatefulBuffer instance)
			{
				instance.IsDirty = true;
				return ((PageBuffer)instance).SwitchStateAsync(PageBufferStateType.Dirty);
			}
		}

		private class DirtyState : PageBufferState
		{
			public override PageBufferStateType StateType => PageBufferStateType.Dirty;

		    public override Task SetDirtyAsync(StatefulBuffer instance)
			{
				return CompletedTask.Default;
			}

			public override Task PrepareForCommit(PageBuffer instance, long timestamp)
			{
				if (TrunkTransactionContext.Current == null)
				{
					throw new InvalidOperationException("No internal transaction context.");
				}
				return CompletedTask.Default;
			}

			public override Task Commit(PageBuffer instance, long timestamp)
			{
				return instance.SwitchStateAsync(PageBufferStateType.Log, timestamp);
			}

			public override Task Rollback(PageBuffer instance)
			{
				// Discard changes and switch to allocated state
				if (instance._oldBuffer != null)
				{
					instance._newBuffer.Dispose();
					instance._newBuffer = instance._oldBuffer;
					instance._oldBuffer = null;
				}
				return instance.SwitchStateAsync(PageBufferStateType.Allocated);
			}
		}

		private class LogState : PageBufferState
		{
			public override PageBufferStateType StateType => PageBufferStateType.Log;

		    public override async Task OnEnterStateAsync(StatefulBuffer instance, State lastState, object userState)
			{
				// Must have transaction context
				if (TrunkTransactionContext.Current == null)
				{
					throw new InvalidOperationException("No internal transaction context.");
				}

				var pageBufferInstance = (PageBuffer)instance;
				var timestamp = (long)userState;

				// Create transaction log entry
				TransactionLogEntry entry;
				if (pageBufferInstance.IsNew)
				{
					entry = new PageImageCreateLogEntry(
						pageBufferInstance._newBuffer,
						instance.PageId.Value,
						timestamp);
				}
				else if (pageBufferInstance.IsDeleted)
				{
					entry = new PageImageDeleteLogEntry(
						pageBufferInstance._oldBuffer,
						instance.PageId.Value,
						timestamp);
				}
				else
				{
					entry = new PageImageUpdateLogEntry(
						pageBufferInstance._oldBuffer,
						pageBufferInstance._newBuffer,
						instance.PageId.Value,
						timestamp);
				}

				// Write log record to underlying device.
			    if (TrunkTransactionContext.Current is ITrunkTransactionPrivate privTxn)
				{
					await privTxn.WriteLogEntryAsync(entry).ConfigureAwait(false);
				}

				// Update new/delete status bits
				if (pageBufferInstance.IsDeleted)
				{
					pageBufferInstance.IsNew = true;
					pageBufferInstance.IsDeleted = false;
				}
				else if (pageBufferInstance.IsNew)
				{
					pageBufferInstance.IsNew = false;
				}

				// Update buffer and signal pending write
				pageBufferInstance._newBuffer.CopyTo(pageBufferInstance._oldBuffer);
				pageBufferInstance.IsDirty = false;

				// Switch to allocated state
				await pageBufferInstance
					.SwitchStateAsync(PageBufferStateType.AllocatedWritable)
					.ConfigureAwait(false);
			}
		}

		private class AllocatedWritableState : PageBufferState
		{
			public override PageBufferStateType StateType => PageBufferStateType.AllocatedWritable;

		    public override Task Save(PageBuffer instance)
			{
				// Alias the buffer to save and invalidate
				var buffer = instance._oldBuffer;
				instance._oldBuffer = null;

				// Issue save on aliased buffer - do not wait
			    // ReSharper disable once UnusedVariable
				var taskNoWait = instance.SaveBufferThenDisposeAsync(buffer);

				// Switch to the allocated state now
				return instance.SwitchStateAsync(PageBufferStateType.Allocated);
			}

			public override Task SetDirtyAsync(StatefulBuffer instance)
			{
				instance.IsDirty = true;
				return ((PageBuffer)instance).SwitchStateAsync(PageBufferStateType.Dirty);
			}
		}

		private class StateChangeTrigger
		{
			private readonly PageBufferStateType[] _triggers;
			private readonly TaskCompletionSource<object> _task;

			public StateChangeTrigger(PageBufferStateType[] triggers, object state)
			{
				_triggers = triggers;
				_task = new TaskCompletionSource<object>(state);
			}

			public Guid Id { get; } = Guid.NewGuid();

		    public Task Task => _task.Task;

		    public bool CompleteTrigger(PageBufferStateType state)
			{
				var result = false;
				if (_triggers.Any(item => item == state))
				{
					_task.TrySetResult(null);
					result = true;
				}
				return result;
			}
		}
        #endregion

        #region Private Fields
        private static readonly ILog Logger = LogProvider.For<PageBuffer>();

        private readonly IBufferDevice _bufferDevice;
		private IVirtualBuffer _oldBuffer;
		private IVirtualBuffer _newBuffer;
		private TransactionId _currentTransactionId;
		private long _timestamp;
		private readonly ConcurrentDictionary<Guid, StateChangeTrigger> _triggers =
			new ConcurrentDictionary<Guid, StateChangeTrigger>();
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="PageBuffer" /> class.
		/// </summary>
		/// <param name="bufferDevice">The buffer device.</param>
		public PageBuffer(IBufferDevice bufferDevice)
		{
			_bufferDevice = bufferDevice;
			_newBuffer = _bufferDevice.BufferFactory.AllocateBuffer();

			// Set initial state
			SwitchStateAsync(PageBufferStateType.Free);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets/sets a boolean value indicating whether the buffer is new.
		/// </summary>
		public bool IsNew
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is deleted.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is deleted; otherwise, <c>false</c>.
		/// </value>
		public bool IsDeleted
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets a boolean value indicating whether this buffer has a write
		/// pending.
		/// </summary>
		/// <value>
		/// <c>true</c> if this buffer has a write pending; otherwise, <c>false</c>.
		/// </value>
		public bool IsWritePending => CurrentStateType == PageBufferStateType.AllocatedWritable;

	    /// <summary>
		/// Gets a boolean value indicating whether this buffer has a read
		/// pending.
		/// </summary>
		/// <value>
		/// <c>true</c> if this buffer has a read pending; otherwise, <c>false</c>.
		/// </value>
		public bool IsReadPending => CurrentStateType == PageBufferStateType.PendingLoad;

        /// <summary>
        /// Gets the logical identifier.
        /// </summary>
        /// <value>
        /// The logical identifier.
        /// </value>
        public LogicalPageId LogicalPageId
		{
			get;
			internal set;
		}

        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <value>
        /// The size of the buffer.
        /// </value>
        public override int BufferSize => _bufferDevice.BufferFactory.BufferSize;

        /// <summary>
        /// Gets a boolean value indicating whether this buffer can be freed.
        /// </summary>
        public override bool CanFree => CurrentStateType == PageBufferStateType.Allocated;

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        /// <value>
        /// The timestamp.
        /// </value>
        public long Timestamp
		{
			get => _timestamp;
            set => _timestamp = value;
        }

        /// <summary>
        /// Gets the current page buffer state type.
        /// </summary>
		public PageBufferStateType CurrentStateType => CurrentPageBufferState.StateType;
		#endregion

		#region Private Properties
	    private PageBufferState CurrentPageBufferState => (PageBufferState)CurrentState;
        #endregion

        #region Public Methods
        /// <summary>
        /// Enlists the in transaction.
        /// </summary>
        /// <exception cref="InvalidOperationException">Page buffer modification must occur within a transaction.</exception>
        public void EnlistInTransaction()
		{
			if (TrunkTransactionContext.Current == null)
			{
				throw new InvalidOperationException(
					"Page buffer modification must occur within a transaction.");
			}
		    
		    (TrunkTransactionContext.Current as ITrunkTransactionPrivate)?.Enlist(this);
		}

        /// <summary>
        /// Performs asynchronous initialisation of the page buffer.
        /// </summary>
        /// <param name="pageId">The page identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <returns></returns>
        public Task InitAsync(VirtualPageId pageId, LogicalPageId logicalId)
		{
			return CurrentPageBufferState.Init(this, pageId, logicalId);
		}

        /// <summary>
        /// Queues a request-to-load for this page buffer using the specified
        /// virtual page or logical page identifiers.
        /// </summary>
        /// <param name="pageId">The page identifier.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <returns></returns>
        /// <remarks>
        /// The request is queued and will be completed when the underlying
        /// device has it's read requests flushed.
        /// </remarks>
        public Task RequestLoadAsync(VirtualPageId pageId, LogicalPageId logicalId)
		{
			return CurrentPageBufferState.RequestLoad(this, pageId, logicalId);
		}

        /// <summary>
        /// Performs an asynchronous load of this page buffer.
        /// </summary>
        /// <returns></returns>
        public Task LoadAsync()
		{
			return CurrentPageBufferState.Load(this);
		}

        /// <summary>
        /// Performs an asynchronous save of this page buffer.
        /// </summary>
        /// <returns></returns>
        public Task SaveAsync()
		{
			return CurrentPageBufferState.Save(this);
		}

		/// <summary>
		/// Gets a backing stream that can be used to access the contents of
		/// this page buffer object.
		/// </summary>
		/// <param name="offset">
		/// Byte offset into the page for where the stream should start.
		/// </param>
		/// <param name="count">
		/// Number of bytes to be returned in the stream.
		/// </param>
		/// <param name="writable">
		/// Set to <c>true</c> to return a writable stream;
		/// otherwise <c>false</c>.
		/// </param>
		/// <returns>
		/// A <see cref="Stream"/> corresponding to the desired byte range.
		/// </returns>
		/// <remarks>
		/// If there is no active transaction context <see cref="TrunkTransactionContext.Current"/>
		/// then the calling thread is assumed to own the buffer.
		/// If a writable stream is desired then internally the code will
		/// make a copy of the current buffer. This supports both rollback
		/// and simultaneous access from other threads.
		/// </remarks>
		public override Stream GetBufferStream(int offset, int count, bool writable)
		{
            /*// Throw if state marks buffer as locked
			if (IsLocked)
			{
				throw new InvalidOperationException("Buffer is locked.");
			}*/

            // Get transaction identifier and check whether this is an
            //  uncommitted read (aka dirty read)
            var transactionId = TransactionId.Zero;
			var isReadUncommittedTxn = false;
			if (TrunkTransactionContext.Current != null)
			{
				transactionId = TrunkTransactionContext.Current.TransactionId;
				if (TrunkTransactionContext.Current.IsolationLevel == System.Transactions.IsolationLevel.ReadUncommitted)
				{
					isReadUncommittedTxn = true;
					writable = false;
				}
			}

			// When we don't have a current transaction or when the current
			//	transaction matches the active transaction, or the current
			//	transaction is using read-uncommitted; we use the main buffer
			if (_currentTransactionId == TransactionId.Zero ||
				_currentTransactionId == transactionId ||
				isReadUncommittedTxn)
			{
				if (writable)
				{
					// First request for writable buffer must make copy of current.
					if (_oldBuffer == null)
					{
						_oldBuffer = _bufferDevice.BufferFactory.AllocateBuffer();
						_newBuffer.CopyTo(_oldBuffer);
					}
				}

				// Save current transaction ID if necessary
				if (_currentTransactionId == TransactionId.Zero)
				{
					_currentTransactionId = transactionId;
				}

			    if (Logger.IsDebugEnabled())
			    {
			        Logger.Debug($"GetBufferStream backed by current buffer {_newBuffer.BufferId}");
			    }
			    return _newBuffer.GetBufferStream(offset, count, writable);
			}

			// Everything else uses the old buffer in read mode...
		    if (_oldBuffer == null)
		    {
		        throw new InvalidOperationException("Stream is unavailable.");
		    }
		    if (writable)
		    {
		        throw new InvalidOperationException("Another transaction already has write access.");
		    }

		    if (Logger.IsDebugEnabled())
		    {
		        Logger.Debug($"GetBufferStream backed by old buffer {_oldBuffer.BufferId}");
		    }
		    return _oldBuffer.GetBufferStream(offset, count, false);
		}
		#endregion

		#region Private Methods
		private Task LoadNewBufferAsync()
		{
			return LoadBufferAsync(_newBuffer);
		}

		private Task LoadBufferAsync(IVirtualBuffer buffer)
		{
		    if (_bufferDevice is IMultipleBufferDevice mbd)
			{
				return mbd.LoadBufferAsync(PageId, buffer);
			}

		    if (_bufferDevice is ISingleBufferDevice sbd)
		    {
		        return sbd.LoadBufferAsync(PageId.PhysicalPageId, buffer);
		    }

		    throw new InvalidOperationException();
		}

		private async Task SaveBufferThenDisposeAsync(IVirtualBuffer buffer)
		{
		    try
		    {
		        if (_bufferDevice is IMultipleBufferDevice mbd)
			    {
				    await mbd.SaveBufferAsync(PageId, buffer).ConfigureAwait(false);
			    }
			    else if (_bufferDevice is ISingleBufferDevice sbd)
			    {
				    await sbd.SaveBufferAsync(PageId.PhysicalPageId, buffer).ConfigureAwait(false);
			    }
			    else
			    {
				    throw new InvalidOperationException();
			    }
		    }
		    finally
		    {
    			buffer.Dispose();
		    }
		}

		private void EnsureNewBufferAllocated()
		{
			if (_newBuffer == null)
			{
				_newBuffer = _bufferDevice.BufferFactory.AllocateBuffer();
			}
		}

	    private Task SwitchStateAsync(PageBufferStateType newState, object userState = null)
		{
			return SwitchStateAsync(PageBufferStateFactory.GetState(newState), userState);
		}

	    // ReSharper disable once UnusedMember.Local
		private Task WaitForAnyStateAsync(params PageBufferStateType[] states)
		{
			if (states.Any(item => item == CurrentStateType))
			{
				return CompletedTask.Default;
			}

			var trigger = new StateChangeTrigger(states, null);
			_triggers.TryAdd(trigger.Id, trigger);
			return trigger.Task;
		}

	    // ReSharper disable once UnusedMember.Local
		private void RaiseStateTriggers(PageBufferStateType state)
		{
			foreach (var trigger in _triggers.Values.ToArray())
			{
				if (trigger.CompleteTrigger(state))
				{
					_triggers.TryRemove(trigger.Id, out var _);
				}
			}
		}
		#endregion

		#region IPageEnlistmentNotification methods
		async void IPageEnlistmentNotification.Prepare(PreparingPageEnlistment prepare)
		{
			// Sanity check
			CheckDisposed();
			await CurrentPageBufferState
				.PrepareForCommit(this, _timestamp)
				.ConfigureAwait(false);
			prepare.Prepared();
		}

		async void IPageEnlistmentNotification.Commit(PageEnlistment enlistment)
		{
			// Sanity check
			CheckDisposed();
			try
			{
				await CurrentPageBufferState
					.Commit(this, _timestamp)
					.ConfigureAwait(false);
				enlistment.Done();
			}
			finally
			{
				_currentTransactionId = TransactionId.Zero;
			}
		}

		async void IPageEnlistmentNotification.Rollback(PageEnlistment enlistment)
		{
			// Sanity check
			CheckDisposed();
			try
			{
				await CurrentPageBufferState
					.Rollback(this)
					.ConfigureAwait(false);
				enlistment.Done();
			}
			finally
			{
				_currentTransactionId = TransactionId.Zero;
			}
		}

		void IPageEnlistmentNotification.Complete()
		{
			_currentTransactionId = TransactionId.Zero;
			_timestamp = 0;
		}
		#endregion
	}
}
