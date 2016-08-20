namespace Zen.Trunk.Torrent.Client.Messages
{
	using System;
	using System.Net;

	public abstract class Message : IMessage
	{
		public abstract int ByteLength
		{
			get;
		}

		protected virtual void CheckWritten(int written)
		{
			if (written != ByteLength)
				throw new MessageException("Message encoded incorrectly. Incorrect number of bytes written");
		}

		public abstract void Decode(byte[] buffer, int offset, int length);

		public void Decode(ArraySegment<byte> buffer, int offset, int length)
		{
			Decode(buffer.Array, buffer.Offset + offset, length);
		}

		public byte[] Encode()
		{
			byte[] buffer = new byte[ByteLength];
			Encode(buffer, 0);
			return buffer;
		}

		public abstract int Encode(byte[] buffer, int offset);

		public int Encode(ArraySegment<byte> buffer, int offset)
		{
			return Encode(buffer.Array, buffer.Offset + offset);
		}

		static public short ReadShort(byte[] buffer, int offset)
		{
			return ReadShort(buffer, ref offset);
		}

		static public short ReadShort(byte[] buffer, ref int offset)
		{
			short ret = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, offset));
			offset += 2;
			return ret;
		}

		static public int ReadInt(byte[] buffer, int offset)
		{
			return ReadInt(buffer, ref offset);
		}

		static public int ReadInt(byte[] buffer, ref int offset)
		{
			int ret = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, offset));
			offset += 4;
			return ret;
		}

		static public long ReadLong(byte[] buffer, int offset)
		{
			return ReadLong(buffer, ref offset);
		}

		static public long ReadLong(byte[] buffer, ref int offset)
		{
			long ret = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, offset));
			offset += 8;
			return ret;
		}

		static public int Write(byte[] buffer, int offset, byte value)
		{
			buffer[offset] = value;
			return 1;
		}

		static public int Write(byte[] dest, int destOffset, byte[] src, int srcOffset, int count)
		{
			Buffer.BlockCopy(src, srcOffset, dest, destOffset, count);
			return count;
		}

		static public int Write(byte[] buffer, int offset, ushort value)
		{
			return Write(buffer, offset, (short)value);
		}

		static public int Write(byte[] buffer, int offset, short value)
		{
			return Write(buffer, offset, BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
		}

		static public int Write(byte[] buffer, int offset, int value)
		{
			return Write(buffer, offset, BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
		}

		static public int Write(byte[] buffer, int offset, uint value)
		{
			return Write(buffer, offset, (int)value);
		}

		static public int Write(byte[] buffer, int offset, long value)
		{
			return Write(buffer, offset, BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
		}

		static public int Write(byte[] buffer, int offset, ulong value)
		{
			return Write(buffer, offset, (long)value);
		}

		static public int Write(byte[] buffer, int offset, byte[] value)
		{
			return Write(buffer, offset, value, 0, value.Length);
		}
	}
}