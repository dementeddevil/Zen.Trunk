namespace Zen.Trunk.Storage.IO
{
	using System.Threading;

	internal class VirtualBufferCache
	{
		#region Private Fields
		private static int s_nextCacheId = 0;
		private int _cacheId;
		private SafeCommitableMemoryHandle _baseAddress;
		private SafeCommitableMemoryHandle _nextAddress;
		private int _bufferSize;
		private int _bufferCacheSize;
		private VirtualBuffer[] _buffers;
		private int _usedBuffers;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualBufferCache"/> class.
		/// </summary>
		/// <param name="baseAddress">The base address.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="bufferSlots">The buffer slots.</param>
		public unsafe VirtualBufferCache(SafeCommitableMemoryHandle baseAddress, int bufferSize, int bufferSlots)
		{
			_cacheId = Interlocked.Increment(ref s_nextCacheId);
			_baseAddress = baseAddress;
			_bufferSize = bufferSize;
			_bufferCacheSize = bufferSlots;

			_buffers = new VirtualBuffer[_bufferCacheSize];
			for (int index = 0; index < _bufferCacheSize; ++index)
			{
				VirtualBuffer buffer = new VirtualBuffer(baseAddress, bufferSize, this, index);
				_buffers[index] = buffer;
				baseAddress = SafeNativeMethods.GetCommitableMemoryHandle(baseAddress, bufferSize, bufferSize);
			}
			_nextAddress = baseAddress;
		}
		#endregion

		#region Internal Properties
		internal int CacheId
		{
			get
			{
				return _cacheId;
			}
		}

		internal bool IsHalfFull
		{
			get
			{
				return UsedSpace > 50;
			}
		}

		internal bool IsNearlyFull
		{
			get
			{
				return UsedSpace > 75;
			}
		}

		internal bool IsFull
		{
			get
			{
				return UsedSpace > 97;
			}
		}

		internal int UsedSpace
		{
			get
			{
				return (_usedBuffers * 100) / _bufferCacheSize;
			}
		}

		internal SafeCommitableMemoryHandle BaseAddress
		{
			get
			{
				return _baseAddress;
			}
		}

		internal SafeCommitableMemoryHandle NextBaseAddress
		{
			get
			{
				return _nextAddress;
			}
		}
		#endregion

		public VirtualBuffer AllocateBuffer()
		{
			for (int index = 0; index < _bufferCacheSize; ++index)
			{
				VirtualBuffer buffer = Interlocked.Exchange<VirtualBuffer>(
					ref _buffers[index], null);
				if (buffer != null)
				{
					buffer.Allocate();
					Interlocked.Increment(ref _usedBuffers);
					return buffer;
				}
			}
			return null;
		}

		public void FreeBuffer(VirtualBuffer buffer)
		{
			Interlocked.Exchange<VirtualBuffer>(ref _buffers[buffer.CacheSlot], buffer);
			Interlocked.Decrement(ref _usedBuffers);
		}
	}
}
