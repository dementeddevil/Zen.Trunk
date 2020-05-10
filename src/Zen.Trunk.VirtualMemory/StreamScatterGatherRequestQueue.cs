using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// <c>StreamScatterGatherRequestQueue</c> builds blocks of requests that
    /// represent pending reads or pending writes requests on an underlying
    /// store.
    /// </summary>
    public class StreamScatterGatherRequestQueue<TScatterGatherRequestArray>
        where TScatterGatherRequestArray : ScatterGatherRequestArray
    {
		#region Private Fields
	    private readonly ISystemClock _systemClock;
        private readonly Func<ScatterGatherRequest, TScatterGatherRequestArray> _arrayFactory;

		private readonly TimeSpan _maximumRequestAge;
		private readonly TimeSpan _coalesceRequestsPeriod;
		private readonly int _maximumRequestBlockLength;
		private readonly int _maximumRequestBlocks;

		private DateTime _lastCoalescedAt;
		private DateTime _lastScavengeAt;

		private readonly List<TScatterGatherRequestArray> _requestBlocks =
			new List<TScatterGatherRequestArray>();
		private readonly object _syncCallback = new object();
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamScatterGatherRequestQueue{TScatterGatherRequestArray}"/> class.
        /// </summary>
        /// <param name="systemClock"></param>
        /// <param name="settings">The request queue settings.</param>
        /// <param name="arrayFactory">The factory method called to create a request tracker.</param>
        public StreamScatterGatherRequestQueue(
            ISystemClock systemClock,
            StreamScatterGatherRequestQueueSettings settings,
            Func<ScatterGatherRequest, TScatterGatherRequestArray> arrayFactory)
		{
		    _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
            _arrayFactory = arrayFactory ?? throw new ArgumentNullException(nameof(arrayFactory));
		    if (settings == null) throw new ArgumentNullException(nameof(settings));

		    _maximumRequestAge = settings.MaximumRequestAge;
		    _coalesceRequestsPeriod = settings.CoalesceRequestsPeriod;
		    _maximumRequestBlockLength = settings.MaximumRequestBlockLength;
		    _maximumRequestBlocks = settings.MaximumRequestBlocks;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Queues the specified buffer to the list of pending operations.
		/// </summary>
		/// <param name="physicalPageId">The physical page id.</param>
		/// <param name="buffer">The buffer.</param>
		/// <returns>A <see cref="Task"/> that encapsulates the data-transfer operation.</returns>
		/// <remarks>
		/// Buffers are placed into groups such that a given group will be composed of buffers of
		/// adjacent physical pages.
		///	If an existing group does not exist for the specified buffer then a new one will be
		/// created without flushing any existing groups.
		/// </remarks>
		[CLSCompliant(false)]
		public Task QueueBufferRequestAsync(uint physicalPageId, IVirtualBuffer buffer)
		{
			var request = new ScatterGatherRequest(physicalPageId, buffer);

			// Attempt to add to existing array or create a new one
			var added = false;
			lock (_syncCallback)
			{
				foreach (var array in _requestBlocks)
				{
					added = array.AddRequest(request);
					if (added)
					{
						break;
					}
				}

				if (!added)
				{
					_requestBlocks.Add(_arrayFactory(request));
				}
			}

			return request.Task;
		}

		/// <summary>
		/// Flushes the buffers stored in this instance to the underlying store.
		/// </summary>
		/// <remarks>
		/// Prior to flushing the arrays, this method will coalesce arrays into longer chains if possible
		/// in order to minimise the number of I/O calls required.
		/// </remarks>
		public async Task FlushAsync()
		{
		    // ReSharper disable once InconsistentlySynchronizedField
			if (_requestBlocks.Count > 0)
			{
			    List<TScatterGatherRequestArray> workToDo = null;

			    lock (_syncCallback)
				{
                    CoalesceRequests();

					if (_requestBlocks.Count > 0)
					{
						workToDo = new List<TScatterGatherRequestArray>();
						workToDo.AddRange(_requestBlocks);
						_requestBlocks.Clear();
					}
				}

			    if (workToDo != null)
			    {
			        var flushList = workToDo.Select(FlushArray);
				    await Task.WhenAll(flushList.ToArray()).ConfigureAwait(false);
			    }
			}
		}

        /// <summary>
        /// Flushes only the data necessary.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This method performs the following actions;
        /// 1. If the Coalesce Period has expired, this method will coalesce arrays into longer
        ///		chains if possible.
        /// 2. If the number arrays are greater than Max Array limit then the oldest arrays will be
        ///		flushed.
        /// 3. If any arrays contain requests older than Max Request Age or have more requests than
        ///		the Max Array Length limit, then these will be flushed.
        /// This method should be called periodically to ensure timely handling of requests.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly",
			MessageId = "Optimised", Justification = "English spelling")]
		public async Task OptimisedFlushAsync()
		{
			CoalesceIfNeeded();

			await FlushIfNeeded().ConfigureAwait(false);
			await ScavengeIfNeeded().ConfigureAwait(false);
		}
		#endregion

		#region Private Methods
		private void CoalesceIfNeeded()
		{
			if ((_systemClock.UtcNow - _lastCoalescedAt) > _coalesceRequestsPeriod)
			{
				CoalesceRequests();
			}
		}

		private async Task FlushIfNeeded()
		{
			List<TScatterGatherRequestArray> workToDo = null;

		    // ReSharper disable once InconsistentlySynchronizedField
			while (_requestBlocks.Count > _maximumRequestBlocks)
			{
				lock (_syncCallback)
				{
					if (_requestBlocks.Count > _maximumRequestBlocks)
					{
						if (workToDo == null)
						{
							workToDo = new List<TScatterGatherRequestArray>();
						}

						workToDo.Add(_requestBlocks[0]);
						_requestBlocks.RemoveAt(0);
					}
				}
			}

			if (workToDo != null)
			{
			    var flushList = workToDo.Select(FlushArray);
				await Task.WhenAll(flushList.ToArray()).ConfigureAwait(false);
			}
		}

		private Task ScavengeIfNeeded()
		{
		    if ((_systemClock.UtcNow - _lastScavengeAt) > _maximumRequestAge)
			{
				return ScavengeRequests();
			}

		    return CompletedTask.Default;
		}

		private void CoalesceRequests()
		{
		    // ReSharper disable once InconsistentlySynchronizedField
			if (_requestBlocks.Count > 1)
			{
				lock (_syncCallback)
				{
					if (_requestBlocks.Count > 1)
					{
						var primaryRequest = 0;
						var candidateRequest = 1;
						while (primaryRequest < _requestBlocks.Count - 1)
						{
							if (_requestBlocks[primaryRequest].Consume(_requestBlocks[candidateRequest]))
							{
								_requestBlocks.RemoveAt(candidateRequest);
							}
							else
							{
								++candidateRequest;
							}

							if (candidateRequest >= _requestBlocks.Count)
							{
								++primaryRequest;
								candidateRequest = primaryRequest + 1;
							}
						}
					}
				}
			}

			_lastCoalescedAt = _systemClock.UtcNow;
		}

		private async Task ScavengeRequests()
		{
			List<TScatterGatherRequestArray> workToDo = null;
			lock (_syncCallback)
			{
				while (_requestBlocks.Count > 0 && _requestBlocks[0].RequiresFlush(_maximumRequestAge, _maximumRequestBlockLength))
				{
					if (workToDo == null)
					{
						workToDo = new List<TScatterGatherRequestArray>();
					}

					workToDo.Add(_requestBlocks[0]);
					_requestBlocks.RemoveAt(0);
				}
			}

			if (workToDo != null)
			{
				var flushList = workToDo.Select(FlushArray);
				await Task.WhenAll(flushList.ToArray()).ConfigureAwait(false);
			}

			_lastScavengeAt = _systemClock.UtcNow;
		}

		private Task FlushArray(TScatterGatherRequestArray array)
		{
		    return array != null ? array.FlushAsync() : CompletedTask.Default;
		}
		#endregion
	}
}
