using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Zen.Trunk.Extensions;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Logging;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// <c>PageBuffer</c> provides state management for database pages.
    /// </summary>
    /// <remarks>
    /// State support for database pages requires the following;
    /// 1. Separation of readers and writers where appropriate
    /// 2. Delay writing until log-writer has written change information
    /// 3. Load and save of data page to the underlying device
    /// </remarks>
    public sealed class PageBuffer : IPageEnlistmentNotification, IPageBuffer
    {
        /// <summary>
        /// Tracks the state of a page-buffer within the page-buffer lifecycle
        /// </summary>
        public enum StateType
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

        #region Private Types
        private static class StateFactory
        {
            private static readonly State FreeStateObject = new FreeState();
            private static readonly State LoadStateObject = new LoadState();
            private static readonly State PendingLoadStateObject = new PendingLoadState();
            private static readonly State AllocatedStateObject = new AllocatedState();
            private static readonly State DirtyStateObject = new DirtyState();
            private static readonly State LogStateObject = new LogState();
            private static readonly State AllocatedWritableStateObject = new AllocatedWritableState();

            public static State GetState(StateType state)
            {
                switch (state)
                {
                    case StateType.Free:
                        return FreeStateObject;

                    case StateType.Load:
                        return LoadStateObject;

                    case StateType.PendingLoad:
                        return PendingLoadStateObject;

                    case StateType.Allocated:
                        return AllocatedStateObject;

                    case StateType.Dirty:
                        return DirtyStateObject;

                    case StateType.Log:
                        return LogStateObject;

                    case StateType.AllocatedWritable:
                        return AllocatedWritableStateObject;

                    default:
                        throw new InvalidOperationException("Unknown page buffer state.");
                }
            }
        }

        /// <summary>
        /// Base class for State pattern dealing with buffer behaviour.
        /// </summary>
        private abstract class State
        {
            #region Public Properties
            public abstract StateType StateType
            {
                get;
            }
            #endregion

            #region Public Methods
            /// <summary>
            /// Performs state specific buffer add-ref logic.
            /// </summary>
            /// <param name="instance"></param>
            public virtual void AddRef(PageBuffer instance)
            {
            }

            /// <summary>
            /// Performs state specific buffer release-ref logic.
            /// </summary>
            /// <param name="instance"></param>
            public virtual void Release(PageBuffer instance)
            {
            }

            /// <summary>
            /// Performs state specific buffer deallocation
            /// </summary>
            /// <param name="instance"></param>
            public virtual Task SetFreeAsync(PageBuffer instance)
            {
                InvalidState();
                return CompletedTask.Default;
            }

            /// <summary>
            /// Performs state specific buffer dirty operation.
            /// </summary>
            /// <param name="instance"></param>
            public virtual Task SetDirtyAsync(PageBuffer instance)
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
            public virtual Task OnEnterStateAsync(PageBuffer instance, State lastState, object userState)
            {
                return CompletedTask.Default;
            }

            /// <summary>
            /// Notifies class that buffer instance is leaving state.
            /// </summary>
            /// <param name="instance"></param>
            /// <param name="nextState"></param>
            /// <param name="userState"></param>
            public virtual Task OnLeaveStateAsync(PageBuffer instance, State nextState, object userState)
            {
                return CompletedTask.Default;
            }

            public virtual Task Init(PageBuffer instance, VirtualPageId pageId, LogicalPageId logicalId)
            {
                InvalidState();
                return CompletedTask.Default;
            }

            public virtual Task RequestLoad(PageBuffer instance, VirtualPageId pageId, LogicalPageId logicalId)
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

            public virtual Task PrepareForCommit(PageBuffer instance, long timestamp)
            {
                InvalidState();
                return CompletedTask.Default;
            }

            public virtual Task Commit(PageBuffer instance, long timestamp)
            {
                InvalidState();
                return CompletedTask.Default;
            }

            public virtual Task Rollback(PageBuffer instance)
            {
                InvalidState();
                return CompletedTask.Default;
            }
            #endregion

            #region Protected Methods
            /// <summary>
            /// Called whenever an invalid state is encountered.
            /// </summary>
            protected void InvalidState([CallerMemberName] string callerMethod = null)
            {
                throw new InvalidOperationException($"{GetType().Name}::{callerMethod} call is invalid in this state.");
            }
            #endregion
        }

        private class FreeState : State
        {
            public override StateType StateType => StateType.Free;

            public override Task SetFreeAsync(PageBuffer instance)
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
                return instance.SwitchStateAsync(StateType.Allocated);
            }

            public override Task RequestLoad(PageBuffer instance, VirtualPageId pageId, LogicalPageId logicalId)
            {
                instance.PageId = pageId;
                instance.LogicalPageId = logicalId;

                // Switch to load state
                return instance.SwitchStateAsync(StateType.PendingLoad);
            }
        }

        private class PendingLoadState : State
        {
            public override StateType StateType => StateType.PendingLoad;

            public override Task Load(PageBuffer instance)
            {
                // Allocate the buffer if required and switch state
                instance.EnsureNewBufferAllocated();
                return instance.SwitchStateAsync(StateType.Load);
            }
        }

        private class LoadState : State
        {
            public override StateType StateType => StateType.Load;

            public override async Task OnEnterStateAsync(PageBuffer instance, State lastState, object userState)
            {
                await instance
                    .LoadNewBufferAsync()
                    .ConfigureAwait(false);

                await instance
                    .SwitchStateAsync(StateType.Allocated)
                    .ConfigureAwait(false);
            }
        }

        private class AllocatedState : State
        {
            public override StateType StateType => StateType.Allocated;

            public override Task SetFreeAsync(PageBuffer instance)
            {
                return instance.SwitchStateAsync(StateType.Free);
            }

            public override Task SetDirtyAsync(PageBuffer instance)
            {
                instance.IsDirty = true;
                return instance.SwitchStateAsync(StateType.Dirty);
            }
        }

        private class DirtyState : State
        {
            public override StateType StateType => StateType.Dirty;

            public override Task SetDirtyAsync(PageBuffer instance)
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
                return instance.SwitchStateAsync(StateType.Log, timestamp);
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
                return instance.SwitchStateAsync(StateType.Allocated);
            }
        }

        private class LogState : State
        {
            public override StateType StateType => StateType.Log;

            public override async Task OnEnterStateAsync(PageBuffer instance, State lastState, object userState)
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
                if (TrunkTransactionContext.Current is ITrunkTransactionPrivate privateContext)
                {
                    await privateContext.WriteLogEntryAsync(entry).ConfigureAwait(false);
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
                    .SwitchStateAsync(StateType.AllocatedWritable)
                    .ConfigureAwait(false);
            }
        }

        private class AllocatedWritableState : State
        {
            public override StateType StateType => StateType.AllocatedWritable;

            public override Task Save(PageBuffer instance)
            {
                // Alias the buffer to save and invalidate
                var buffer = instance._oldBuffer;
                try
                {
                    instance._oldBuffer = null;

                    // Issue save on aliased buffer - do not wait
                    // ReSharper disable once UnusedVariable
                    var taskNoWait = instance.SaveBufferThenDisposeAsync(buffer);

                    // Switch to the allocated state now
                    return instance.SwitchStateAsync(StateType.Allocated);
                }
                catch
                {
                    // Restore old buffer so we don't lose anything and rethrow
                    instance._oldBuffer = buffer;
                    throw;
                }
            }

            public override Task SetDirtyAsync(PageBuffer instance)
            {
                instance.IsDirty = true;
                return instance.SwitchStateAsync(StateType.Dirty);
            }
        }
        #endregion

        #region Private Fields
        private static readonly ILogger Logger = Log.ForContext<PageBuffer>();
        private State _currentState;
        private int _refCount;
        private bool _isDisposed;
        private readonly IBufferDevice _bufferDevice;
        private IVirtualBuffer _oldBuffer;
        private IVirtualBuffer _newBuffer;
        private TransactionId _currentTransactionId;
        private long _timestamp;
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
            SwitchStateAsync(StateType.Free);
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
            private set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether this buffer can be freed.
        /// </summary>
        public bool CanFree => CurrentStateType == StateType.Allocated;

        /// <summary>
        /// Gets a boolean value indicating whether this buffer is dirty.
        /// </summary>
        public bool IsDirty
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <value>The size of the buffer.</value>
        public int BufferSize => _bufferDevice.BufferFactory.BufferSize;

        /// <summary>
        /// Gets/sets a boolean value indicating whether the buffer is new.
        /// </summary>
        public bool IsNew
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is deleted.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is deleted; otherwise, <c>false</c>.
        /// </value>
        public bool IsDeleted
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether this buffer has a write pending.
        /// </summary>
        /// <value>
        /// <c>true</c> if this buffer has a write pending; otherwise, <c>false</c>.
        /// </value>
        public bool IsWritePending => CurrentStateType == StateType.AllocatedWritable;

        /// <summary>
        /// Gets a boolean value indicating whether this buffer has a read pending.
        /// </summary>
        /// <value>
        /// <c>true</c> if this buffer has a read pending; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadPending => CurrentStateType == StateType.PendingLoad;

        /// <summary>
        /// Gets the logical identifier.
        /// </summary>
        /// <value>
        /// The logical identifier.
        /// </value>
        public LogicalPageId LogicalPageId
        {
            get;
            set;
        }

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
		public StateType CurrentStateType => CurrentState.StateType;
        #endregion

        #region Private Properties
        /// <summary>
        /// Gets the current state object.
        /// </summary>
        /// <value>The state of the current.</value>
        private State CurrentState => _currentState;
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
            return CurrentState.Init(this, pageId, logicalId);
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
            return CurrentState.RequestLoad(this, pageId, logicalId);
        }

        /// <summary>
        /// Performs an asynchronous load of this page buffer.
        /// </summary>
        /// <returns></returns>
        public Task LoadAsync()
        {
            return CurrentState.Load(this);
        }

        /// <summary>
        /// Performs an asynchronous save of this page buffer.
        /// </summary>
        /// <returns></returns>
        public Task SaveAsync()
        {
            return CurrentState.Save(this);
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
        public Stream GetBufferStream(int offset, int count, bool writable)
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
                // First request for writable buffer must make copy of current.
                if (writable && _oldBuffer == null)
                {
                    _oldBuffer = _bufferDevice.BufferFactory.AllocateBuffer();
                    _newBuffer.CopyTo(_oldBuffer);
                }

                // Save current transaction ID if necessary
                if (_currentTransactionId == TransactionId.Zero)
                {
                    _currentTransactionId = transactionId;
                }

                Logger.Debug($"GetBufferStream backed by new buffer {_newBuffer.BufferId}");
                return _newBuffer.GetBufferStream(offset, count, writable);
            }

            // Everything else uses the old buffer in read mode...
            if (writable)
            {
                throw new InvalidOperationException("Another transaction already has write access.");
            }

            if (_oldBuffer == null)
            {
                _oldBuffer = _bufferDevice.BufferFactory.AllocateBuffer();
                _newBuffer.CopyTo(_oldBuffer);
            }

            Logger.Debug($"GetBufferStream backed by old buffer {_oldBuffer.BufferId}");
            return _oldBuffer.GetBufferStream(offset, count, false);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; 
        /// <c>false</c> to release only unmanaged resources.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Debug.Assert(_refCount == 0, "Potentially invalid disposal of buffer with reference count > 0.");
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Throws an exception if this buffer has been disposed.
        /// </summary>
        private void CheckDisposed()
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
        private async Task SwitchStateAsync(State newState, object userState)
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
        }

        private Task LoadNewBufferAsync()
        {
            return LoadBufferAsync(_newBuffer);
        }

        private Task LoadBufferAsync(IVirtualBuffer buffer)
        {
            return _bufferDevice.LoadBufferAsync(PageId, buffer);
        }

        private async Task SaveBufferThenDisposeAsync(IVirtualBuffer buffer)
        {
            try
            {
                await _bufferDevice.SaveBufferAsync(PageId, buffer).ConfigureAwait(false);
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

        private Task SwitchStateAsync(StateType newState, object userState = null)
        {
            return SwitchStateAsync(StateFactory.GetState(newState), userState);
        }
        #endregion

        #region IPageEnlistmentNotification methods
        async void IPageEnlistmentNotification.Prepare(PreparingPageEnlistment prepare)
        {
            // Sanity check
            CheckDisposed();
            await CurrentState
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
                await CurrentState
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
                await CurrentState
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
