namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Concurrent;
	using Zen.Trunk.Torrent.Common;

	public enum BufferType
	{
		SmallMessageBuffer,
		MediumMessageBuffer,
		LargeMessageBuffer,
		MassiveBuffer
	}

	public class BufferManager
	{
		internal static readonly int SmallMessageBufferSize = 1 << 8;               // 256 bytes
		internal static readonly int MediumMessageBufferSize = 1 << 11;             // 2048 bytes
		internal static readonly int LargeMessageBufferSize = Piece.BlockSize + 32; // 16384 bytes + 32. Enough for a complete piece aswell as the overhead

		public static readonly ArraySegment<byte> EmptyBuffer = new System.ArraySegment<byte>(new byte[0]);

		private ObjectPool<ArraySegment<byte>> _largeMessageBuffers;
		private ObjectPool<ArraySegment<byte>> _mediumMessageBuffers;
		private ObjectPool<ArraySegment<byte>> _smallMessageBuffers;
		private ConcurrentQueue<ArraySegment<byte>> _massiveBuffers;

		private ConcurrentDictionary<int, ObjectPool<BitField>> _bitFields;

		/// <summary>
		/// The class that controls the allocating and deallocating of all byte[] buffers used in the engine.
		/// </summary>
		public BufferManager()
		{
			_bitFields = new ConcurrentDictionary<int, ObjectPool<BitField>>();
			_massiveBuffers = new ConcurrentQueue<ArraySegment<byte>>();
			_largeMessageBuffers = new ObjectPool<ArraySegment<byte>>(
				() => new ArraySegment<byte>(new byte[LargeMessageBufferSize]));
			_mediumMessageBuffers = new ObjectPool<ArraySegment<byte>>(
				() => new ArraySegment<byte>(new byte[MediumMessageBufferSize]));
			_smallMessageBuffers = new ObjectPool<ArraySegment<byte>>(
				() => new ArraySegment<byte>(new byte[SmallMessageBufferSize]));

			// Preallocate some of each buffer to help avoid heap fragmentation due to pinning
			AllocateBuffers(4, BufferType.LargeMessageBuffer);
			AllocateBuffers(4, BufferType.MediumMessageBuffer);
			AllocateBuffers(4, BufferType.SmallMessageBuffer);
		}

		internal BitField GetBitField(int length)
		{
			ObjectPool<BitField> bitFieldPool;
			if (!_bitFields.TryGetValue(length, out bitFieldPool))
			{
				bitFieldPool = new ObjectPool<BitField>(
					() => new BitField(length));
				bitFieldPool = _bitFields.GetOrAdd(length, bitFieldPool);
			}
			return bitFieldPool.GetObject();
		}

		internal BitField GetClonedBitField(BitField bitfield)
		{
			BitField clone = GetBitField(bitfield.Length);
			Buffer.BlockCopy(bitfield.Array, 0, clone.Array, 0, clone.Array.Length * 4);
			return clone;
		}

		internal void FreeBitField(ref BitField b)
		{
			ObjectPool<BitField> bitFieldPool;
			if (_bitFields.TryGetValue(b.Length, out bitFieldPool))
			{
				bitFieldPool.PutObject(b);
			}

			b = null;
		}

		/// <summary>
		/// Allocates an existing buffer from the pool
		/// </summary>
		/// <param name="buffer">The byte[]you want the buffer to be assigned to</param>
		/// <param name="type">The type of buffer that is needed</param>
		public void GetBuffer(ref ArraySegment<byte> buffer, int minCapacity)
		{
			if (minCapacity <= SmallMessageBufferSize)
			{
				GetBuffer(ref buffer, BufferType.SmallMessageBuffer);
			}
			else if (minCapacity <= MediumMessageBufferSize)
			{
				GetBuffer(ref buffer, BufferType.MediumMessageBuffer);
			}
			else if (minCapacity <= LargeMessageBufferSize)
			{
				GetBuffer(ref buffer, BufferType.LargeMessageBuffer);
			}
			else
			{
				// If we already have a massive buffer that is large enough
				//	then use that...
				if (buffer.Count >= minCapacity)
				{
					return;
				}
				else if (buffer != EmptyBuffer)
				{
					FreeBuffer(ref buffer);
				}

				System.Diagnostics.Debug.Assert(minCapacity < (1024 * 1024));

				int attempts = _massiveBuffers.Count;
				for (int i = 0; i < attempts; i++)
				{
					if (!_massiveBuffers.TryDequeue(out buffer))
					{
						break;
					}
					if (buffer.Count >= minCapacity)
					{
						return;
					}
					_massiveBuffers.Enqueue(buffer);
				}
				buffer = new ArraySegment<byte>(new byte[minCapacity], 0, minCapacity);
			}
		}

		/// <summary>
		/// Returns a buffer to the pool after it has finished being used.
		/// </summary>
		/// <param name="buffer">The buffer to add back into the pool</param>
		/// <returns></returns>
		public void FreeBuffer(ref ArraySegment<byte> buffer)
		{
			if (buffer == EmptyBuffer)
			{
				return;
			}

			if (buffer.Count == SmallMessageBufferSize)
			{
				_smallMessageBuffers.PutObject(buffer);
			}
			else if (buffer.Count == MediumMessageBufferSize)
			{
				_mediumMessageBuffers.PutObject(buffer);
			}
			else if (buffer.Count == LargeMessageBufferSize)
			{
				_largeMessageBuffers.PutObject(buffer);
			}
			else if (buffer.Count > LargeMessageBufferSize)
			{
				_massiveBuffers.Enqueue(buffer);
			}
			else
			{
				throw new TorrentException("That buffer wasn't created by this manager");
			}

			buffer = EmptyBuffer; // After recovering the buffer, we send the "EmptyBuffer" back as a placeholder
		}

		private void AllocateBuffers(int number, BufferType type)
		{
			Logger.Log(null, "BufferManager - Allocating {0} buffers of type {1}", number, type);
			if (type == BufferType.LargeMessageBuffer)
			{
				while (number-- > 0)
				{
					_largeMessageBuffers.PutObject(new ArraySegment<byte>(new byte[LargeMessageBufferSize], 0, LargeMessageBufferSize));
				}
			}
			else if (type == BufferType.MediumMessageBuffer)
			{
				while (number-- > 0)
				{
					_mediumMessageBuffers.PutObject(new ArraySegment<byte>(new byte[MediumMessageBufferSize], 0, MediumMessageBufferSize));
				}
			}
			else if (type == BufferType.SmallMessageBuffer)
			{
				while (number-- > 0)
				{
					_smallMessageBuffers.PutObject(new ArraySegment<byte>(new byte[SmallMessageBufferSize], 0, SmallMessageBufferSize));
				}
			}
			else
			{
				throw new ArgumentException("Unsupported BufferType detected");
			}
		}

		/// <summary>
		/// Allocates an existing buffer from the pool
		/// </summary>
		/// <param name="buffer">The byte[]you want the buffer to be assigned to</param>
		/// <param name="type">The type of buffer that is needed</param>
		private void GetBuffer(ref ArraySegment<byte> buffer, BufferType type)
		{
			if (type == BufferType.SmallMessageBuffer)
			{
				if (buffer.Count == SmallMessageBufferSize)
				{
					// Buffer is already correct size; return immediately
					return;
				}
				else if (buffer != EmptyBuffer)
				{
					// Free existing buffer
					FreeBuffer(ref buffer);
				}

				// Get buffer from object pool
				buffer = _smallMessageBuffers.GetObject();
			}
			else if (type == BufferType.MediumMessageBuffer)
			{
				if (buffer.Count == MediumMessageBufferSize)
				{
					// Buffer is already correct size; return immediately
					return;
				}
				else if (buffer != EmptyBuffer)
				{
					// Free existing buffer
					FreeBuffer(ref buffer);
				}

				// Get buffer from object pool
				buffer = _mediumMessageBuffers.GetObject();
			}
			else if (type == BufferType.LargeMessageBuffer)
			{
				if (buffer.Count == LargeMessageBufferSize)
				{
					// Buffer is already correct size; return immediately
					return;
				}
				else if (buffer != EmptyBuffer)
				{
					// Free existing buffer
					FreeBuffer(ref buffer);
				}

				// Get buffer from object pool
				buffer = _largeMessageBuffers.GetObject();
			}
			else
			{
				throw new TorrentException("You cannot directly request a massive buffer");
			}
		}
	}
}
