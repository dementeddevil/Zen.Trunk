using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Zen.Trunk.VirtualMemory
{
    internal sealed class AdvancedStreamAsyncResult : IAsyncResult
    {
        #region Internal Fields
        internal bool _completedSynchronously;
        internal int _EndXxxCalled;
        internal int _errorCode;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal SafeFileHandle _handle;
        internal bool _isComplete;
        internal bool _isWrite;
        internal bool _isScatterGather;
        internal int _numBufferedBytes;
        internal int _numBytes;
        internal unsafe NativeOverlapped* _overlapped;
        internal AsyncCallback _userCallback;
        internal object _userStateObject;
        internal ManualResetEvent _waitHandle;
        internal GCHandle[] _pinnedBuffers;
        #endregion

        #region Public Properties
        // Properties
        /// <summary>
        /// Gets a user-defined object that qualifies or contains information about an asynchronous operation.
        /// </summary>
        /// <returns>A user-defined object that qualifies or contains information about an asynchronous operation.</returns>
        public object AsyncState => _userStateObject;

        /// <summary>
        /// Gets a <see cref="T:System.Threading.WaitHandle" /> that is used to wait for an asynchronous operation to complete.
        /// </summary>
        /// <returns>A <see cref="T:System.Threading.WaitHandle" /> that is used to wait for an asynchronous operation to complete.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "Wait handle is stored and returned to the caller.")]
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_waitHandle == null)
                {
                    var event2 = new ManualResetEvent(false);
                    unsafe
                    {
                        if ((_overlapped != null) && (_overlapped->EventHandle != IntPtr.Zero))
                        {
                            event2.SafeWaitHandle = new SafeWaitHandle(_overlapped->EventHandle, true);
                        }
                    }
                    if (_isComplete)
                    {
                        event2.Set();
                    }
                    _waitHandle = event2;
                }
                return _waitHandle;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the asynchronous operation completed synchronously.
        /// </summary>
        /// <returns>true if the asynchronous operation completed synchronously; otherwise, false.</returns>
        public bool CompletedSynchronously => _completedSynchronously;

        /// <summary>
        /// Gets a value that indicates whether the asynchronous operation has completed.
        /// </summary>
        /// <returns>true if the operation is complete; otherwise, false.</returns>
        public bool IsCompleted => _isComplete;
        #endregion

        #region Internal Methods
        internal static AdvancedStreamAsyncResult CreateBufferedReadResult(int numBufferedBytes, AsyncCallback userCallback, object userStateObject)
        {
            var result = new AdvancedStreamAsyncResult
            {
                _userCallback = userCallback,
                _userStateObject = userStateObject,
                _isWrite = false,
                _numBufferedBytes = numBufferedBytes
            };
            return result;
        }

        internal void CallUserCallback()
        {
            // Will be safe to unpin buffers now
            if (_pinnedBuffers != null)
            {
                foreach (var handle in _pinnedBuffers)
                {
                    handle.Free();
                }
                _pinnedBuffers = null;
            }

            if (_userCallback != null)
            {
                _completedSynchronously = false;
                ThreadPool.QueueUserWorkItem(CallUserCallbackWorker);
            }
            else
            {
                _isComplete = true;
                _waitHandle?.Set();
            }
        }
        #endregion

        #region Private Methods
        private void CallUserCallbackWorker(object callbackState)
        {
            _isComplete = true;
            _waitHandle?.Set();
            _userCallback(this);
        }
        #endregion
    }
}