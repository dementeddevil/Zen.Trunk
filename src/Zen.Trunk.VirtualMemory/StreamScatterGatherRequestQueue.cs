using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    public class StreamScatterGatherRequestQueue
	{
		#region Private Fields
		private readonly AdvancedFileStream _stream;
		private readonly bool _isReader;

		private readonly TimeSpan _maximumWriteAge = TimeSpan.FromMilliseconds(500);
		private readonly TimeSpan _coalesceRequestsPeriod = TimeSpan.FromMilliseconds(100);
		private readonly int _maximumArraySize = 16;
		private readonly int _maximumNumberOfArrays = 5;

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
        /// <param name="isReader">
        /// <c>true</c> if the helper is to work in read-mode; otherwise <c>false</c>.
        /// </param>
        public StreamScatterGatherRequestQueue(AdvancedFileStream stream, bool isReader)
		{
			_stream = stream;
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
		///	will be composed of buffers of adjecent storage areas.
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
		/// Flushes the buffers stored in this instance to the underlying
		/// </summary>
		public async Task FlushAsync()
		{
			// Obtain the completion objects to be flushed
			List<ScatterGatherRequestArray> arrayList = null;
			if (_requests.Count > 0)
			{
				lock (_syncCallback)
				{
					if (_requests.Count > 0)
					{
						// Get array of buffers to be persisted.
						arrayList = new List<ScatterGatherRequestArray>();
						arrayList.AddRange(_requests);
						_requests.Clear();
					}
				}
			}

			// If we have work to do...
			if (arrayList != null)
			{
				// Build list of async tasks from the flush action for each array
				var flushList = new List<Task>();
				foreach (var array in arrayList)
				{
					flushList.Add(FlushArray(array));
				}

				// Use a single await operation to wait for the flush to finish
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
			List<ScatterGatherRequestArray> candidates = null;
			while (_requests.Count > _maximumNumberOfArrays)
			{
				lock (_syncCallback)
				{
					if (_requests.Count > _maximumNumberOfArrays)
					{
						if (candidates == null)
						{
							candidates = new List<ScatterGatherRequestArray>();
						}
						candidates.Add(_requests[0]);
						_requests.RemoveAt(0);
					}
				}
			}

			if (candidates != null)
			{
				var flushList = new List<Task>();
				foreach (var array in candidates)
				{
					flushList.Add(FlushArray(array));
				}
				await Task.WhenAll(flushList.ToArray()).ConfigureAwait(false);
			}
		}

		private Task ScavengeIfNeeded()
		{
			if ((DateTime.UtcNow - _lastScavengeAt) > _maximumWriteAge)
			{
				return ScavengeRequests();
			}
			else
			{
				return CompletedTask.Default;
			}
		}

		private void CoalesceRequests()
		{
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
			List<ScatterGatherRequestArray> candidates = null;
			lock (_syncCallback)
			{
				while (_requests.Count > 0 && _requests[0].RequiresFlush(_maximumWriteAge, _maximumArraySize))
				{
					if (candidates == null)
					{
						candidates = new List<ScatterGatherRequestArray>();
					}
					candidates.Add(_requests[0]);
					_requests.RemoveAt(0);
				}
			}

			if (candidates != null)
			{
				var flushList = new List<Task>();
				foreach (var array in candidates)
				{
					flushList.Add(FlushArray(array));
				}
				await Task.WhenAll(flushList.ToArray()).ConfigureAwait(false);
			}

			_lastScavengeAt = DateTime.UtcNow;
		}

		private Task FlushArray(ScatterGatherRequestArray array)
		{
			if (array != null)
			{
				if (_isReader)
				{
					return array.FlushAsReadAsync(_stream);
				}
				else
				{
					return array.FlushAsWriteAsync(_stream);
				}
			}
			return CompletedTask.Default;
		}
		#endregion
	}
}
