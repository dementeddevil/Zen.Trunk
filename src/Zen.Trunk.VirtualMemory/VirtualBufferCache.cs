using System.Threading;

namespace Zen.Trunk.VirtualMemory
{
    internal class VirtualBufferCache
	{
		#region Private Fields
		private static int _nextCacheId = 1;

	    private readonly int _bufferCacheSize;
		private readonly VirtualBuffer[] _buffers;
		private int _usedBuffers;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualBufferCache"/> class.
		/// </summary>
		/// <param name="baseAddress">The base address.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="bufferSlots">The buffer slots.</param>
		public VirtualBufferCache(SafeCommitableMemoryHandle baseAddress, int bufferSize, int bufferSlots)
		{
			CacheId = Interlocked.Increment(ref _nextCacheId);
			BaseAddress = baseAddress;
			_bufferCacheSize = bufferSlots;

			_buffers = new VirtualBuffer[_bufferCacheSize];
			for (var index = 0; index < _bufferCacheSize; ++index)
			{
				var buffer = new VirtualBuffer(baseAddress, bufferSize, this, index);
				_buffers[index] = buffer;
				baseAddress = SafeNativeMethods.GetCommitableMemoryHandle(baseAddress, bufferSize, bufferSize);
			}
			NextBaseAddress = baseAddress;
		}
		#endregion

		#region Internal Properties
		internal int CacheId { get; }

	    internal bool IsHalfFull => UsedSpace > 50;

	    internal bool IsNearlyFull => UsedSpace > 75;

	    internal bool IsFull => UsedSpace > 97;

	    internal int UsedSpace => (_usedBuffers * 100) / _bufferCacheSize;

	    internal SafeCommitableMemoryHandle BaseAddress { get; }

	    internal SafeCommitableMemoryHandle NextBaseAddress { get; }
	    #endregion

		public VirtualBuffer AllocateBuffer()
		{
			for (var index = 0; index < _bufferCacheSize; ++index)
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
	}
}
