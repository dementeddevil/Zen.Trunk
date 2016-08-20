namespace Zen.Trunk.Torrent.Dht
{
	using System;
	using Zen.Trunk.Torrent.Bencoding;

	internal class NodeId : IEquatable<NodeId>, IComparable<NodeId>, IComparable
	{
		static readonly Random random = new Random();

		BigInteger value;
		private byte[] bytes;

		internal byte[] Bytes
		{
			get
			{
				return bytes;
			}
		}

		internal NodeId(byte[] value)
			: this(new BigInteger(value))
		{
			this.bytes = value;
		}

		private NodeId(BigInteger value)
		{
			this.value = value;
		}

		internal NodeId(BEncodedString value)
			: this(new BigInteger(value.TextBytes))
		{
			this.bytes = value.TextBytes;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as NodeId);
		}

		public bool Equals(NodeId other)
		{
			if ((object)other == null)
				return false;

			return value.Equals(other.value);
		}

		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		public override string ToString()
		{
			return value.ToString();
		}

		public int CompareTo(object obj)
		{
			return CompareTo(obj as NodeId);
		}

		public int CompareTo(NodeId other)
		{
			if ((object)other == null)
				return 1;

			BigInteger.Sign s = value.Compare(other.value);
			if (s == BigInteger.Sign.Zero)
				return 0;
			else if (s == BigInteger.Sign.Positive)
				return 1;
			else
				return -1;
		}

		internal NodeId Xor(NodeId right)
		{
			return new NodeId(value.Xor(right.value));
		}

		public static implicit operator NodeId(int value)
		{
			return new NodeId(new BigInteger((uint)value));
		}

		public static NodeId operator -(NodeId first)
		{
			CheckArguments(first);
			return new NodeId(first.value);
		}

		public static NodeId operator -(NodeId first, NodeId second)
		{
			CheckArguments(first, second);
			return new NodeId(first.value - second.value);
		}

		public static bool operator >(NodeId first, NodeId second)
		{
			CheckArguments(first, second);
			return first.value > second.value;
		}

		public static bool operator <(NodeId first, NodeId second)
		{
			CheckArguments(first, second);
			return first.value < second.value;
		}

		public static bool operator <=(NodeId first, NodeId second)
		{
			CheckArguments(first, second);
			return first < second || first == second;
		}

		public static bool operator >=(NodeId first, NodeId second)
		{
			CheckArguments(first, second);
			return first > second || first == second;
		}

		public static NodeId operator +(NodeId first, NodeId second)
		{
			CheckArguments(first, second);
			return new NodeId(first.value + second.value);
		}

		public static NodeId operator *(NodeId first, NodeId second)
		{
			CheckArguments(first, second);
			return new NodeId(first.value * second.value);
		}

		public static NodeId operator /(NodeId first, NodeId second)
		{
			CheckArguments(first, second);
			return new NodeId(first.value / second.value);
		}

		public static NodeId operator %(NodeId first, NodeId second)
		{
			CheckArguments(first, second);
			return new NodeId(first.value % second.value);
		}

		private static void CheckArguments(NodeId first)
		{
			if (first == null)
				throw new ArgumentNullException("first");
		}

		private static void CheckArguments(NodeId first, NodeId second)
		{
			if (first == null)
				throw new ArgumentNullException("first");
			if (second == null)
				throw new ArgumentNullException("second");
		}

		public static bool operator ==(NodeId first, NodeId second)
		{
			if ((object)first == null)
				return (object)second == null;
			if ((object)second == null)
				return false;
			return first.value == second.value;
		}

		public static bool operator !=(NodeId first, NodeId second)
		{
			return first.value != second.value;
		}

		internal BEncodedString BencodedString()
		{
			return new BEncodedString(value.GetBytes());
		}

		internal NodeId Pow(uint p)
		{
			value = BigInteger.Pow(value, p);
			return this;
		}

		public static NodeId Create()
		{
			byte[] b = new byte[20];
			lock (random)
				random.NextBytes(b);
			return new NodeId(b);
		}
	}
}