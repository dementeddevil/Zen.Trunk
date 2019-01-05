using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zen.Trunk.Logging;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// <c>ScatterGatherRequestArray</c> encapsulates a number of scatter/gather
    /// requests into a batch of operations to be performed simultaneously.
    /// </summary>
    public abstract class ScatterGatherRequestArray
    {
        private readonly ISystemClock _systemClock;
        private static readonly ILog Logger = LogProvider.For<ScatterGatherRequestArray>();

		private readonly DateTime _createdDate;
		private readonly List<ScatterGatherRequest> _callbackInfo = new List<ScatterGatherRequest>();
		private uint _startBlockNo;
		private uint _endBlockNo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterGatherRequestArray"/> class.
        /// </summary>
        /// <param name="systemClock">Reference clock.</param>
        /// <param name="stream">The <see cref="AdvancedStream"/></param>
        /// <param name="request">The request.</param>
        [CLSCompliant(false)]
		public ScatterGatherRequestArray(ISystemClock systemClock, AdvancedStream stream, ScatterGatherRequest request)
		{
		    _systemClock = systemClock;
            Stream = stream;
            _createdDate = _systemClock.UtcNow;
			_startBlockNo = _endBlockNo = request.PhysicalPageId;
			_callbackInfo.Add(request);

            Logger.Debug($"New array created [PageId: {request.PhysicalPageId}]");
		}

        protected AdvancedStream Stream { get; }

        protected uint StartBlockNo => _startBlockNo;

        protected ICollection<ScatterGatherRequest> CallbackInfo => _callbackInfo.AsReadOnly();

        /// <summary>
        /// Adds the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [CLSCompliant(false)]
		public bool AddRequest(ScatterGatherRequest request)
		{
			if (request.PhysicalPageId == _startBlockNo - 1)
			{
				_startBlockNo = request.PhysicalPageId;
				_callbackInfo.Insert(0, request);

                Logger.Debug($"Request [PageId: {request.PhysicalPageId}] prepended to array [StartPageId: {_startBlockNo}, EndPageId: {_endBlockNo}]");
				return true;
			}

            if (request.PhysicalPageId == _endBlockNo + 1)
			{
				_endBlockNo = request.PhysicalPageId;
				_callbackInfo.Add(request);

                Logger.Debug($"Request [PageId: {request.PhysicalPageId}] appended to array [StartPageId: {_startBlockNo}, EndPageId: {_endBlockNo}]");
				return true;
			}

            return false;
		}

        /// <summary>
        /// Consumes the specified request array into this instance.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>
        /// <c>true</c> if the array was successfully consumed; otherwise <c>false</c>.
        /// </returns>
        public bool Consume(ScatterGatherRequestArray other)
		{
			if (_endBlockNo == (other._startBlockNo - 1))
			{
				_endBlockNo = other._endBlockNo;
				_callbackInfo.AddRange(other._callbackInfo);

                Logger.Debug($"Source array [StartPageId: {other._startBlockNo}, EndPageId: {other._endBlockNo}] prepended to array [StartPageId: {_startBlockNo}, EndPageId: {_endBlockNo}]");
				return true;
			}

            if (_startBlockNo == (other._endBlockNo + 1))
			{
				_startBlockNo = other._startBlockNo;
				_callbackInfo.InsertRange(0, other._callbackInfo);

                Logger.Debug($"Source array [StartPageId: {other._startBlockNo}, EndPageId: {other._endBlockNo}] appended to array [StartPageId: {_startBlockNo}, EndPageId: {_endBlockNo}]");
				return true;
			}

            return false;
		}

        /// <summary>
        /// Determines whether a flush of the underlying stream is required given
        /// the specified maximum request age and/or maximum count of requests
        /// </summary>
        /// <param name="maximumAge">The maximum age.</param>
        /// <param name="maximumLength">The maximum length.</param>
        /// <returns>
        /// <c>true</c> if a flush is required; otherwise <c>false</c>.
        /// </returns>
        public bool RequiresFlush(TimeSpan maximumAge, int maximumLength)
		{
			if ((_endBlockNo - _startBlockNo + 1) > maximumLength)
			{
                Logger.Debug($"Flush required as array exceeds maximum length");
				return true;
			}

            if ((_systemClock.UtcNow - _createdDate) > maximumAge)
			{
                Logger.Debug($"Flush required as array exceeds maximum age");
				return true;
			}

            return false;
		}

        /// <summary>
        /// Flushes the request array via the specified stream.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public abstract Task FlushAsync();


        /// <summary>
        /// Executes the specified I/O operation and completes all requests
        /// associated with the array with an appropriate response.
        /// </summary>
        /// <param name="asyncIo">A function returning a <see cref="Task"/> representing the I/O operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task ExecuteIoOperationAsync(Func<Task> asyncIo)
        {
            try
            {
                await asyncIo().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                foreach (var callback in _callbackInfo)
                {
                    callback.TrySetCanceled();
                }
                return;
            }
            catch (Exception e)
            {
                foreach (var callback in _callbackInfo)
                {
                    callback.TrySetException(e);
                }
                return;
            }

            // Notify each callback that we are now finished
            foreach (var callback in _callbackInfo)
            {
                callback.Buffer.ClearDirty();
                callback.TrySetResult(null);
            }
        }
    }
}
