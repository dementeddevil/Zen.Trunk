using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Serilog;

namespace Zen.Trunk.VirtualMemory
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
    [DebuggerDisplay("Cache Usage: {_bufferChain?.Count ?? 0}/{_maxCacheElements}, Used: {UsedSpacePercent}%, IsNearlyFull: {IsNearlyFull}")]
    public sealed class VirtualBufferFactory : IVirtualBufferFactory
    {
        private static readonly ILogger Logger = Log.ForContext<VirtualBufferFactory>();
        private const int MinimumReservationMb = 16;
        private const long OneMegaByte = 1024L * 1024L;

        private readonly int _reservationPages;
        private readonly object _syncBufferChain = new object();
        private readonly int _cacheBlockSize;
        private readonly int _maxCacheElements;
        private readonly VirtualBufferFactorySettings _settings;
        private SafeMemoryHandle _reservationBaseAddress;
        private LinkedList<VirtualBufferCache> _bufferChain;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualBufferFactory" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <exception cref="ArgumentException">Buffer size must be multiple of {VirtualBuffer.SystemPageSize}.</exception>
        public VirtualBufferFactory(VirtualBufferFactorySettings settings)
        {
            _settings = settings;

            // Minimum reservation amount = 16Mb
            var reservationMb = settings.BufferSize * settings.ReservationPageCount;
            if (reservationMb < MinimumReservationMb)
            {
                reservationMb = MinimumReservationMb;
            }

            // Calculate number of pages to reserve (in system page size units)
            _reservationPages = (int)((reservationMb * OneMegaByte) /
                VirtualBuffer.SystemPageSize);

            // Determine maximum number of pages
            var totalPages = _reservationPages * VirtualBuffer.SystemPageSize / settings.BufferSize;
            _cacheBlockSize = Math.Max(8, settings.PagesPerCacheBlock);

            // Determine maximum number of caches
            _maxCacheElements = totalPages / _cacheBlockSize;

            // Debugging information
            Logger.Debug(
                "Virtual buffer factory initialised with reservation of {ReservationMb}Mb and buffer size of {BufferSize}",
                reservationMb,
                settings.BufferSize);
            Logger.Information(
                "Virtual buffer factory {TotalPages} pages split across {MaxCacheElements} caches available",
                totalPages,
                _maxCacheElements);
        }

        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <value>
        /// The size of the buffer.
        /// </value>
        public int BufferSize => _settings.BufferSize;

        /// <summary>
        /// Gets a value indicating whether this instance is nearly full.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is nearly full; otherwise, <c>false</c>.
        /// </value>
        public bool IsNearlyFull => _bufferChain != null && _bufferChain.Count == _maxCacheElements && _bufferChain.All(bc => bc.IsNearlyFull);

        /// <summary>
        /// Gets the factory used buffer percentage
        /// </summary>
        /// <remarks>
        /// This property is not thread-safe and returns a percentage based on
        /// the maximum amount of memory that has been reserved.
        /// </remarks>
	    public int UsedSpacePercent => _bufferChain.Sum(c => c.UsedSpacePercent) / _maxCacheElements;

        /// <summary>
        /// Allocates the buffer.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="OutOfMemoryException">Virtual buffer resources exhausted.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes",
            Justification = "Throwing an out of memory exception is an acceptable usage scenario for this method.")]
        public IVirtualBuffer AllocateBuffer()
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
                            SafeNativeMethods.GetCommitableMemoryHandle(_reservationBaseAddress, BufferSize),
                            _settings));
                        _bufferChain = newChain;
                    }
                }
            }

            // Walk buffer cache
            var current = _bufferChain.First;
            while (true)
            {
                // ReSharper disable once PossibleNullReferenceException
                var buffer = current.Value.AllocateBuffer();
                if (buffer != null)
                {
                    return buffer;
                }

                // Create node and add to chain
                if (current.Next == null)
                {
                    // Check whether we are about to exceed the reservation pages
                    // ReSharper disable once PossibleNullReferenceException
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
                                current.Value.NextBaseAddress, _settings));
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
            try
            {
                // Determine total bytes to reserve
                var totalBytes = ((ulong)VirtualBuffer.SystemPageSize) * ((ulong)_reservationPages);

                Logger.Information(
                    $"Reservation of {_reservationPages} pages at {totalBytes} total bytes.");

                _reservationBaseAddress = SafeNativeMethods.VirtualReserve(
                    new UIntPtr(totalBytes), SafeNativeMethods.PAGE_NOACCESS);
            }
            catch (Win32Exception e)
            {
                throw new OutOfMemoryException("ReservePages failed", e);
            }
        }

        #region IDisposable Members
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_reservationBaseAddress != null)
            {
                var totalBytes = ((ulong)VirtualBuffer.SystemPageSize) *
                    ((ulong)_reservationPages);

                Logger.Information(
                    $"Release of {_reservationPages} pages at {totalBytes} total bytes.");

                _reservationBaseAddress.Dispose();
                _reservationBaseAddress = null;
            }
        }
        #endregion
    }
}
