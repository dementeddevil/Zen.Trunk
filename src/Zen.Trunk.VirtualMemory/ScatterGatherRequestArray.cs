using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// <c>ScatterGatherRequestArray</c> encapsulates a number of scatter/gather
    /// requests into a batch of operations to be performed simultaneously.
    /// </summary>
    public abstract class ScatterGatherRequestArray
    {
        private static readonly ILogger Logger = Log.ForContext<ScatterGatherRequestArray>();
        private readonly ISystemClock _systemClock;
		private readonly DateTime _createdDate;
		private readonly List<ScatterGatherRequest> _callbackInfo = new List<ScatterGatherRequest>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterGatherRequestArray"/> class.
        /// </summary>
        /// <param name="systemClock">Reference clock.</param>
        /// <param name="stream">The <see cref="AdvancedStream"/></param>
        /// <param name="request">The request.</param>
        [CLSCompliant(false)]
		public ScatterGatherRequestArray(
            ISystemClock systemClock,
            AdvancedStream stream,
            ScatterGatherRequest request)
		{
            _systemClock = systemClock;
            Stream = stream;
            _createdDate = _systemClock.UtcNow;
			StartBlockNo = EndBlockNo = request.PhysicalPageId;
			_callbackInfo.Add(request);

            Logger.Debug(
                "New array created [PageId: {PhysicalPageId}]",
                request.PhysicalPageId);
		}

        protected AdvancedStream Stream { get; }

        protected uint StartBlockNo { get; private set; }

        protected uint EndBlockNo { get; private set; }

        protected ICollection<ScatterGatherRequest> CallbackInfo => _callbackInfo.AsReadOnly();

        /// <summary>
        /// Adds the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [CLSCompliant(false)]
		public bool AddRequest(ScatterGatherRequest request)
		{
			if (request.PhysicalPageId == StartBlockNo - 1)
			{
				StartBlockNo = request.PhysicalPageId;
				_callbackInfo.Insert(0, request);

                Logger.Debug(
                    "Request [PageId: {PhysicalPageId}] prepended to array [StartBlock: {StartBlockNumber}, EndPageId: {EndBlockNumber}]",
                    request.PhysicalPageId,
                    StartBlockNo,
                    EndBlockNo);
				return true;
			}

            if (request.PhysicalPageId == EndBlockNo + 1)
			{
				EndBlockNo = request.PhysicalPageId;
				_callbackInfo.Add(request);

                Logger.Debug(
                    "Request [PageId: {PhysicalPageId}] appended to array [StartBlock: {StartBlockNumber}, EndPageId: {EndBlockNumber}]",
                    request.PhysicalPageId,
                    StartBlockNo,
                    EndBlockNo);
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
			if (EndBlockNo == (other.StartBlockNo - 1))
			{
				EndBlockNo = other.EndBlockNo;
				_callbackInfo.AddRange(other._callbackInfo);

                Logger.Debug(
                    "Source array [StartPageId: {SourceStartBlockNumber}, EndPageId: {SourceEndBlockNo}] prepended to array [StartPageId: {CurrentStartBlockNo}, EndPageId: {CurrentEndBlockNo}]",
                    other.StartBlockNo,
                    other.EndBlockNo,
                    StartBlockNo,
                    EndBlockNo);
				return true;
			}

            if (StartBlockNo == (other.EndBlockNo + 1))
			{
				StartBlockNo = other.StartBlockNo;
				_callbackInfo.InsertRange(0, other._callbackInfo);

                Logger.Debug(
                    "Source array [StartPageId: {SourceStartBlockNumber}, EndPageId: {SourceEndBlockNo}] appended to array [StartPageId: {CurrentStartBlockNo}, EndPageId: {CurrentEndBlockNo}]",
                    other.StartBlockNo,
                    other.EndBlockNo,
                    StartBlockNo,
                    EndBlockNo);
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
			if ((EndBlockNo - StartBlockNo + 1) > maximumLength)
			{
                Logger.Debug("Flush required as array exceeds maximum length");
				return true;
			}

            if ((_systemClock.UtcNow - _createdDate) > maximumAge)
			{
                Logger.Debug("Flush required as array exceeds maximum age");
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
