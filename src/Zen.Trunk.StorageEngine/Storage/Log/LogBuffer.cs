using System;
using System.IO;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;

namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// <c>LogBuffer</c> provides a buffer implementation suitable for
    /// database log pages.
    /// </summary>
    public sealed class LogBuffer : StatefulBuffer
    {
        #region Internal Types
        private enum LogBufferStateType
        {
            Free,
            Load,
            Allocated,
            Dirty,
        }

        private static class LogBufferStateFactory
        {
            private static readonly State FreeStateObject = new FreeState();
            private static readonly State LoadStateObject = new LoadState();
            private static readonly State AllocatedStateObject = new AllocatedState();
            private static readonly State DirtyStateObject = new DirtyState();

            public static State GetState(LogBufferStateType state)
            {
                switch (state)
                {
                    case LogBufferStateType.Free:
                        return FreeStateObject;
                    case LogBufferStateType.Load:
                        return LoadStateObject;
                    case LogBufferStateType.Allocated:
                        return AllocatedStateObject;
                    case LogBufferStateType.Dirty:
                        return DirtyStateObject;
                    default:
                        throw new InvalidOperationException("Invalid buffer state.");
                }
            }
        }

        private abstract class LogBufferState : State
        {
            public abstract LogBufferStateType StateType
            {
                get;
            }

            public virtual Task Init(LogBuffer instance, VirtualPageId pageId)
            {
                InvalidState();
                return CompletedTask.Default;
            }

            public virtual Task Load(LogBuffer instance, VirtualPageId pageId)
            {
                InvalidState();
                return CompletedTask.Default;
            }

        }

        private class FreeState : LogBufferState
        {
            public override LogBufferStateType StateType => LogBufferStateType.Free;

            public override Task Init(LogBuffer instance, VirtualPageId pageId)
            {
                instance.PageId = pageId;
                if (instance._buffer == null)
                {
                    instance._buffer = instance.BufferFactory.AllocateBuffer();
                }
                return instance.SwitchState(LogBufferStateType.Allocated);
            }
        }

        private class LoadState : LogBufferState
        {
            public override LogBufferStateType StateType => LogBufferStateType.Load;

            public override async Task OnEnterStateAsync(StatefulBuffer instance, State lastState, object userState)
            {
                // Ask page buffer to load from underlying device.
                var logPage = (LogBuffer)instance;
                await logPage.DoLoadAsync().ConfigureAwait(false);

                // Switch to allocated state
                await logPage.SwitchState(LogBufferStateType.Allocated).ConfigureAwait(false);
            }
        }

        private class AllocatedState : LogBufferState
        {
            public override LogBufferStateType StateType => LogBufferStateType.Allocated;

            public override Task SetFreeAsync(StatefulBuffer instance)
            {
                return ((LogBuffer)instance).SwitchState(LogBufferStateType.Free);
            }

            public override Task SetDirtyAsync(StatefulBuffer instance)
            {
                return ((LogBuffer)instance).SwitchState(LogBufferStateType.Dirty);
            }
        }

        private class DirtyState : LogBufferState
        {
            #region Public Properties
            /// <summary>
            /// Overridden. Returns a value indicating the implemented state for
            /// this state object.
            /// </summary>
            public override LogBufferStateType StateType => LogBufferStateType.Dirty;
            #endregion

            #region Public Methods
            public override Task SetDirtyAsync(StatefulBuffer instance)
            {
                // Ignore - we're already dirty
                return CompletedTask.Default;
            }
            #endregion
        }
        #endregion

        #region Private Fields
        private readonly Stream _backingStore;
        private IVirtualBuffer _buffer;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Constructs a device buffer object associated with the given owner
        /// device.
        /// </summary>
        public LogBuffer(FileStream backingStore, IVirtualBufferFactory bufferFactory)
        {
            _backingStore = backingStore;
            BufferFactory = bufferFactory;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <value>The size of the buffer.</value>
        public override int BufferSize => BufferFactory.BufferSize;

        /// <summary>
        /// Gets a boolean value indicating whether this buffer can be freed.
        /// </summary>
        public override bool CanFree => CurrentPageBufferState.StateType != LogBufferStateType.Dirty;
        #endregion

        #region Private Properties
        private IVirtualBufferFactory BufferFactory { get; }

        private LogBufferStateType CurrentStateType => CurrentPageBufferState.StateType;

        private LogBufferState CurrentPageBufferState => (LogBufferState)CurrentState;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the specified page identifier.
        /// </summary>
        /// <param name="pageId">The page identifier.</param>
        /// <returns></returns>
        public Task InitAsync(VirtualPageId pageId)
        {
            return CurrentPageBufferState.Init(this, pageId);
        }

        /// <summary>
        /// Loads the specified page identifier.
        /// </summary>
        /// <param name="pageId">The page identifier.</param>
        /// <returns></returns>
        public Task LoadAsync(VirtualPageId pageId)
        {
            return CurrentPageBufferState.Load(this, pageId);
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// Called internally by page objects to retrieve their backing stream.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="writable"></param>
        /// <returns></returns>
        public override Stream GetBufferStream(int offset, int count, bool writable)
        {
            // Throw if state marks buffer as locked
            /*if (IsLocked)
			{
				throw new InvalidOperationException("Buffer is locked.");
			}*/

            return _buffer.GetBufferStream(offset, count, writable);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; 
        /// <c>false</c> to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _buffer?.Dispose();
                }
                _buffer = null;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
        #endregion

        #region Private Methods
        private async Task DoLoadAsync()
        {
            Task loadTask;
            var buffer = new byte[_buffer.BufferSize];
            lock (_backingStore)
            {
                _backingStore.Seek(_buffer.BufferSize * PageId.PhysicalPageId, SeekOrigin.Begin);
                loadTask = _backingStore.ReadAsync(buffer, 0, _buffer.BufferSize);
            }
            await loadTask.ConfigureAwait(false);
            _buffer.InitFrom(buffer);
        }

        private Task DoSaveAsync()
        {
            Task saveTask;
            var buffer = new byte[_buffer.BufferSize];
            _buffer.CopyTo(buffer);
            lock (_backingStore)
            {
                _backingStore.Seek(_buffer.BufferSize * PageId.PhysicalPageId, SeekOrigin.Begin);
                saveTask = _backingStore.WriteAsync(buffer, 0, _buffer.BufferSize);
            }
            return saveTask;
        }

        private Task SwitchState(LogBufferStateType newState, object userState = null)
        {
            return SwitchStateAsync(LogBufferStateFactory.GetState(newState), userState);
        }
        #endregion
    }
}
