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
    public class StreamScatterGatherRequestQueue
	{
		#region Private Fields
		private readonly AdvancedStream _stream;
		private readonly bool _isReader;

		private readonly TimeSpan _maximumRequestAge;
		private readonly TimeSpan _coalesceRequestsPeriod;
		private readonly int _maximumRequestBlockLength;
		private readonly int _maximumRequestBlocks;

		private DateTime _lastCoalescedAt;
		private DateTime _lastScavengeAt;

		private readonly List<ScatterGatherRequestArray> _requests =
			new List<ScatterGatherRequestArray>();
		private readonly object _syncCallback = new object();
        #endregion

        #region Public Constructors
	    /// <summary>
	    /// Initializes a new instance of the <see cref="StreamScatterGatherRequestQueue"/> class.
	    /// </summary>
	    /// <param name="stream">The advanced file stream.</param>
	    /// <param name="settings">The request queue settings.</param>
	    /// <param name="isReader">
	    /// <c>true</c> if the helper is to work in read-mode; otherwise <c>false</c>.
	    /// </param>
	    public StreamScatterGatherRequestQueue(
            AdvancedStream stream,
            StreamScatterGatherRequestQueueSettings settings,
            bool isReader)
		{
			_stream = stream;
		    _maximumRequestAge = settings.MaximumRequestAge;
		    _coalesceRequestsPeriod = settings.CoalesceRequestsPeriod;
		    _maximumRequestBlockLength = settings.MaximumRequestBlockLength;
		    _maximumRequestBlocks = settings.MaximumRequestBlocks;
			_isReader = isReader;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Adds the specified buffer to the list of pending operations.
		/// </summary>
		/// <param name="physicalPageId">The physical page id.</param>
		/// <param name="buffer">The buffer.</param>
		/// <returns></returns>
		/// <remarks>
		/// Internally buffers are placed into groups such that a given group
		///	will be composed of buffers of adjacent storage areas.
		///	If a suitable group does not exist for the specified buffer then a
		///	new one will be created without flushing any existing groups.
		/// </remarks>
		[CLSCompliant(false)]
		public Task ProcessBufferAsync(uint physicalPageId, IVirtualBuffer buffer)
		{
			var request = new ScatterGatherRequest(physicalPageId, buffer);

			// Attempt to add to existing array or create a new one
			var added = false;
			lock (_syncCallback)
			{
				foreach (var array in _requests)
				{
					added = array.AddRequest(request);
					if (added)
					{
						break;
					}
				}

				if (!added)
				{
					_requests.Add(new ScatterGatherRequestArray(request));
				}
			}

			return request.Task;
		}

		/// <summary>
		/// Flushes the buffers stored in this instance to the underlying store
		/// </summary>
		public async Task FlushAsync()
		{
			// Obtain the completion objects to be flushed
			List<ScatterGatherRequestArray> workToDo = null;
		    // ReSharper disable once InconsistentlySynchronizedField
			if (_requests.Count > 0)
			{
				lock (_syncCallback)
				{
					if (_requests.Count > 0)
					{
						// Get array of buffers to be persisted.
						workToDo = new List<ScatterGatherRequestArray>();
						workToDo.AddRange(_requests);
						_requests.Clear();
					}
				}
			}

			if (workToDo != null)
			{
			    var flushList = workToDo.Select(FlushArray);
				await Task.WhenAll(flushList.ToArray()).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Flushes only the data necessary.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// The helper stores all received requests in arrays and will try to
		/// create longer block chains before issuing the scatter/gather call
		/// to transfer data.
		/// This method should be called periodically to ensure timely handling
		/// of requests
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
			if ((DateTime.UtcNow - _lastCoalescedAt) > _coalesceRequestsPeriod)
			{
				CoalesceRequests();
			}
		}

		private async Task FlushIfNeeded()
		{
			List<ScatterGatherRequestArray> workToDo = null;
		    // ReSharper disable once InconsistentlySynchronizedField
			while (_requests.Count > _maximumRequestBlocks)
			{
				lock (_syncCallback)
				{
					if (_requests.Count > _maximumRequestBlocks)
					{
						if (workToDo == null)
						{
							workToDo = new List<ScatterGatherRequestArray>();
						}

						workToDo.Add(_requests[0]);
						_requests.RemoveAt(0);
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
		    if ((DateTime.UtcNow - _lastScavengeAt) > _maximumRequestAge)
			{
				return ScavengeRequests();
			}

		    return CompletedTask.Default;
		}

		private void CoalesceRequests()
		{
		    // ReSharper disable once InconsistentlySynchronizedField
			if (_requests.Count > 1)
			{
				lock (_syncCallback)
				{
					if (_requests.Count > 1)
					{
						var primaryRequest = 0;
						var candidateRequest = 1;
						while (primaryRequest < _requests.Count - 1)
						{
							if (_requests[primaryRequest].Consume(_requests[candidateRequest]))
							{
								_requests.RemoveAt(candidateRequest);
							}
							else
							{
								++candidateRequest;
							}

							if (candidateRequest >= _requests.Count)
							{
								++primaryRequest;
								candidateRequest = primaryRequest + 1;
							}
						}
					}
				}
			}

			_lastCoalescedAt = DateTime.UtcNow;
		}

		private async Task ScavengeRequests()
		{
			List<ScatterGatherRequestArray> workToDo = null;
			lock (_syncCallback)
			{
				while (_requests.Count > 0 && _requests[0].RequiresFlush(_maximumRequestAge, _maximumRequestBlockLength))
				{
					if (workToDo == null)
					{
						workToDo = new List<ScatterGatherRequestArray>();
					}

					workToDo.Add(_requests[0]);
					_requests.RemoveAt(0);
				}
			}

			if (workToDo != null)
			{
				var flushList = workToDo.Select(FlushArray);
				await Task.WhenAll(flushList.ToArray()).ConfigureAwait(false);
			}

			_lastScavengeAt = DateTime.UtcNow;
		}

		private Task FlushArray(ScatterGatherRequestArray array)
		{
			if (array != null)
			{
			    return _isReader 
			        ? array.FlushAsReadAsync(_stream)
			        : array.FlushAsWriteAsync(_stream);
			}

			return CompletedTask.Default;
		}
		#endregion
	}
}
