using System.Diagnostics;
using System.Threading;

namespace Zen.Trunk.VirtualMemory
{
    [DebuggerDisplay(
        "Used={" + nameof(UsedSpacePercent) + "}%",
        Name = "Cache [{" + nameof(CacheId) + "}]")]
    internal class VirtualBufferCache
	{
		#region Private Fields
		private static int _nextCacheId = 1;

		private readonly VirtualBuffer[] _buffers;
        private readonly VirtualBufferFactorySettings _settings;
        private int _usedBuffers;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualBufferCache"/> class.
		/// </summary>
		/// <param name="baseAddress">The base address.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="bufferSlots">The buffer slots.</param>
		public VirtualBufferCache(
		    SafeCommitableMemoryHandle baseAddress,
			VirtualBufferFactorySettings settings)
		{
			CacheId = Interlocked.Increment(ref _nextCacheId);
			BaseAddress = baseAddress;
            _settings = settings;

			_buffers = new VirtualBuffer[_settings.PagesPerCacheBlock];
			for (var index = 0; index < _settings.PagesPerCacheBlock; ++index)
			{
				_buffers[index] = new VirtualBuffer(this, baseAddress, _settings.BufferSize, index);
				baseAddress = SafeNativeMethods.GetCommitableMemoryHandle(
					baseAddress, _settings.BufferSize, _settings.BufferSize);
			}

			NextBaseAddress = baseAddress;
		}
		#endregion

		#region Internal Properties
		internal int CacheId { get; }

	    internal bool IsHalfFull => UsedSpacePercent > 50;

	    internal bool IsNearlyFull => UsedSpacePercent > 75;

	    internal bool IsFull => UsedSpacePercent > 97;

	    internal int UsedSpacePercent => (_usedBuffers * 100) / _settings.PagesPerCacheBlock;

	    internal SafeCommitableMemoryHandle BaseAddress { get; }

	    internal SafeCommitableMemoryHandle NextBaseAddress { get; }
        #endregion

        #region Public Methods
        public VirtualBuffer AllocateBuffer()
        {
            for (var index = 0; index < _settings.PagesPerCacheBlock; ++index)
            {
                var buffer = Interlocked.Exchange(ref _buffers[index], null);
                if (buffer == null) continue;

                buffer.Allocate();
                Interlocked.Increment(ref _usedBuffers);
                return buffer;
            }

            return null;
        }

        public void FreeBuffer(VirtualBuffer buffer)
        {
            Interlocked.Exchange(ref _buffers[buffer.CacheSlot], buffer);
            Interlocked.Decrement(ref _usedBuffers);
        } 
        #endregion
    }
}
