using System;
using System.Collections.Generic;
using Zen.Trunk.Logging;

namespace Zen.Trunk.Storage.IO
{
	/// <summary>
	/// <c>VirtualBufferFactory</c> manages the heap-space allocated
	/// through the Win32 VirtualAlloc family of functions.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The factory dispenses buffer objects of a fixed size declared at
	/// construction time and each buffer is tracked 
	/// </para>
	/// <para>
	/// The heap manager is derived from <see cref="T:CriticalFinalizerObject"/>
	/// and hence is provided with CLR protection during finalization, even
	/// if the AppDomain is forcibly unloaded - this should keep memory leaks
	/// at bay.
	/// </para>
	/// </remarks>
	public class VirtualBufferFactory : IVirtualBufferFactory
	{
	    private static readonly ILog Logger = LogProvider.For<VirtualBufferFactory>();

        private const int MinimumReservationMb = 16;
        private const long OneMegaByte = 1024L * 1024L;

		private readonly int _reservationPages;
		private readonly object _syncBufferChain = new object();
		private readonly int _bufferSize;
		private readonly int _cacheBlockSize;
		private readonly int _maxCacheElements;
		private SafeMemoryHandle _reservationBaseAddress;
		private LinkedList<VirtualBufferCache> _bufferChain;

		public VirtualBufferFactory(int bufferSize, int reservationMb)
		{
            // Buffer size must be multiple of system page size
            if((bufferSize % VirtualBuffer.SystemPageSize) != 0)
            {
                throw new ArgumentException(
                    $"Buffer size must be multiple of {VirtualBuffer.SystemPageSize}.",
                    nameof(bufferSize));
            }
			_bufferSize = bufferSize;

			// Minimum reservation amount = 16Mb
			if (reservationMb < MinimumReservationMb)
			{
				reservationMb = MinimumReservationMb;
			}

			// Calculate number of pages to reserve
			_reservationPages = (int)((((long)reservationMb) * OneMegaByte) /
				VirtualBuffer.SystemPageSize);

			// Determine maximum number of pages
			var totalPages = _reservationPages * VirtualBuffer.SystemPageSize / bufferSize;
			_cacheBlockSize = Math.Max(8, totalPages / 16);

			// Determine maximum number of caches
			_maxCacheElements = totalPages / _cacheBlockSize;

            // Debugging information
		    if (Logger.IsDebugEnabled())
		    {
		        Logger.Debug(
		            $"Virtual buffer factory initialised with reservation of {reservationMb}Mb and buffer size of {bufferSize}");
		    }
            if(Logger.IsInfoEnabled())
            {
                Logger.Info(
                    $"Virtual buffer factory {totalPages} pages split across {_maxCacheElements} caches available");
            }
		}

		public int BufferSize => _bufferSize;

		public bool IsNearlyFull
		{
			get
			{
				if (_bufferChain == null || _bufferChain.Count < _maxCacheElements)
				{
					return false;
				}

                foreach (var cache in _bufferChain)
				{
					if (!cache.IsNearlyFull)
					{
						return false;
					}
				}
				return true;
			}
		}

	    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes",
			Justification = "Throwing an out of memory exception is an acceptable usage scenario for this method.")]
		public unsafe IVirtualBuffer AllocateBuffer()
		{
			// Reserve page space as needed.
			if (_reservationBaseAddress == null)
			{
				lock (_syncBufferChain)
				{
					if (_reservationBaseAddress == null)
					{
						ReservePages();
					}
				}
			}

			// Create buffer chain as required
			if (_bufferChain == null)
			{
				lock (_syncBufferChain)
				{
					if (_bufferChain == null)
					{
						var newChain = new LinkedList<VirtualBufferCache>();
						newChain.AddFirst(new VirtualBufferCache(
							SafeNativeMethods.GetCommitableMemoryHandle(_reservationBaseAddress, _bufferSize),
							_bufferSize, _cacheBlockSize));
						_bufferChain = newChain;
					}
				}
			}

			// Walk buffer cache
			var current = _bufferChain.First;
			while (true)
			{
				var buffer = current.Value.AllocateBuffer();
				if (buffer != null)
				{
					return buffer;
				}

				// Create node and add to chain
				if (current.Next == null)
				{
					// Check whether we are about to exceed the reservation pages
					if (((current.Value.NextBaseAddress.DangerousGetHandle().ToInt64() - _reservationBaseAddress.DangerousGetHandle().ToInt64()) /
						VirtualBuffer.SystemPageSize) >= _reservationPages)
					{
						throw new OutOfMemoryException("Virtual buffer resources exhausted.");
					}

					// Lock and recheck
					lock (_syncBufferChain)
					{
						if (current.Next == null)
						{
							// Increase size of the buffer chain
							_bufferChain.AddLast(new VirtualBufferCache(
								current.Value.NextBaseAddress, _bufferSize, _cacheBlockSize));
						}
					}
				}

				// Advance to next cache object
				current = current.Next;
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes",
			Justification = "Throwing an out of memory exception is an acceptable usage scenario for this method.")]
		private void ReservePages()
		{
			// Determine total bytes to reserve
			var totalBytes = ((ulong)VirtualBuffer.SystemPageSize) *
				((ulong)_reservationPages);

            if (Logger.IsInfoEnabled())
            {
                Logger.Info(
                    $"Reservation of {_reservationPages} pages at {totalBytes} total bytes.");
            }

			_reservationBaseAddress = SafeNativeMethods.VirtualReserve(
				new UIntPtr(totalBytes), SafeNativeMethods.PAGE_NOACCESS);
		}

		#region IDisposable Members
		public void Dispose()
		{
			Dispose(true);
            GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_reservationBaseAddress != null)
			{
				var totalBytes = ((ulong)VirtualBuffer.SystemPageSize) *
					((ulong)_reservationPages);

			    if (Logger.IsInfoEnabled())
			    {
			        Logger.Info(
			            $"Release of {_reservationPages} pages at {totalBytes} total bytes.");
			    }

                _reservationBaseAddress.Dispose();
				_reservationBaseAddress = null;
			}
		}
		#endregion
	}
}
