namespace Zen.Trunk.Torrent.Dht
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Text;
	using Zen.Trunk.Torrent.Bencoding;
	using Zen.Trunk.Torrent.Dht.Messages;

	internal class Node : IComparable<Node>, IEquatable<Node>
	{
		public static readonly int MaxFailures = 4;

		IPEndPoint endpoint;
		NodeId id;
		int failedCount;
		DateTime lastSeen;
		BEncodedString token;

		public IPEndPoint EndPoint
		{
			get
			{
				return endpoint;
			}
		}

		public int FailedCount
		{
			get
			{
				return failedCount;
			}
			internal set
			{
				failedCount = value;
			}
		}

		public NodeId Id
		{
			get
			{
				return id;
			}
		}

		public DateTime LastSeen
		{
			get
			{
				return lastSeen;
			}
			internal set
			{
				lastSeen = value;
			}
		}

		// FIXME: State should be set properly as per specification.
		// i.e. it needs to take into account the 'LastSeen' property.
		// and must take into account when a node does not send us messages etc
		public NodeState State
		{
			get
			{
				if (failedCount >= MaxFailures)
					return NodeState.Bad;

				else if (lastSeen == DateTime.MinValue)
					return NodeState.Unknown;

				return (DateTime.UtcNow - lastSeen).TotalMinutes < 15 ? NodeState.Good : NodeState.Questionable;
			}
		}

		public BEncodedString Token
		{
			get
			{
				return token;
			}
			set
			{
				token = value;
			}
		}

		public Node(NodeId id, IPEndPoint endpoint)
		{
			this.endpoint = endpoint;
			this.id = id;
		}

		internal void Seen()
		{
			failedCount = 0;
			lastSeen = DateTime.UtcNow;
		}

		internal BEncodedString CompactPort()
		{
			byte[] buffer = new byte[6];
			CompactPort(buffer, 0);
			return buffer;
		}

		internal void CompactPort(byte[] buffer, int offset)
		{
			DhtMessage.Write(buffer, offset, endpoint.Address.GetAddressBytes());
			DhtMessage.Write(buffer, offset + 4, (ushort)endpoint.Port);
		}

		internal static BEncodedString CompactPort(IList<Node> peers)
		{
			byte[] buffer = new byte[peers.Count * 6];
			for (int i = 0; i < peers.Count; i++)
				peers[i].CompactPort(buffer, i * 6);

			return new BEncodedString(buffer);
		}

		internal BEncodedString CompactNode()
		{
			byte[] buffer = new byte[26];
			CompactNode(buffer, 0);
			return buffer;
		}

		private void CompactNode(byte[] buffer, int offset)
		{
			DhtMessage.Write(buffer, offset, id.Bytes);
			CompactPort(buffer, offset + 20);
		}

		internal static BEncodedString CompactNode(IList<Node> nodes)
		{
			byte[] buffer = new byte[nodes.Count * 26];
			for (int i = 0; i < nodes.Count; i++)
				nodes[i].CompactNode(buffer, i * 26);

			return new BEncodedString(buffer);
		}

		internal static Node FromCompactNode(byte[] buffer, int offset)
		{
			byte[] id = new byte[20];
			Buffer.BlockCopy(buffer, offset, id, 0, 20);
			IPAddress address = new IPAddress((uint)BitConverter.ToInt32(buffer, offset + 20));
			int port = (int)(ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(buffer, offset + 24));
			return new Node(new NodeId(id), new IPEndPoint(address, port));
		}

		internal static IEnumerable<Node> FromCompactNode(byte[] buffer)
		{
			for (int i = 0; (i + 26) <= buffer.Length; i += 26)
				yield return FromCompactNode(buffer, i);
		}

		internal static IEnumerable<Node> FromCompactNode(BEncodedString nodes)
		{
			return FromCompactNode(nodes.TextBytes);
		}

		//To order by last seen in bucket
		public int CompareTo(Node other)
		{
			if (other == null)
				return 1;

			return lastSeen.CompareTo(other.lastSeen);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as Node);
		}

		public bool Equals(Node other)
		{
			if (other == null)
				return false;

			return id.Equals(other.id);
		}

		public override int GetHashCode()
		{
			return id.GetHashCode();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(48);
			for (int i = 0; i < id.Bytes.Length; i++)
			{
				sb.Append(id.Bytes[i]);
				sb.Append("-");
			}
			return sb.ToString(0, sb.Length - 1);
		}

		internal static IEnumerable<Node> CloserNodes(NodeId target, SortedList<NodeId, NodeId> currentNodes, IEnumerable<Node> newNodes, int maxNodes)
		{
			foreach (Node node in newNodes)
			{
				if (currentNodes.ContainsValue(node.Id))
					continue;

				NodeId distance = node.Id.Xor(target);
				if (currentNodes.Count < maxNodes)
				{
					currentNodes.Add(distance, node.Id);
				}
				else if (distance < currentNodes.Keys[currentNodes.Count - 1])
				{
					currentNodes.RemoveAt(currentNodes.Count - 1);
					currentNodes.Add(distance, node.Id);
				}
				else
				{
					continue;
				}
				yield return node;
			}
		}
	}
}