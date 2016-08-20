namespace Zen.Trunk.Torrent.Dht
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Bucket contains the compact list of peers.
	/// </summary>
	/// <remarks>
	/// Each bucket holds a maximum of 8 nodes.
	/// The range of node id values the bucket can contain is defined by the
	/// range; <see cref="MinNodeId"/> &lt;= id &lt; <see cref="MaxNodeId"/>.
	/// </remarks>
	internal class Bucket : IComparable<Bucket>, IEquatable<Bucket>
	{
		public const int MaxCapacity = 8;

		#region Private Fields
		private static NodeId MinimumNodeId = new NodeId(new byte[20]);
		private static NodeId MaximumNodeId = new NodeId(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 });
		private NodeId _minNodeId;
		private NodeId _maxNodeId;
		private List<Node> _nodes = new List<Node>(MaxCapacity);
		private DateTime _lastChanged = DateTime.UtcNow;
		private Node _replacement;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Bucket"/> class.
		/// </summary>
		public Bucket()
			: this(MinimumNodeId, MaximumNodeId)
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Bucket"/> class.
		/// </summary>
		/// <param name="minNodeId">The min node id.</param>
		/// <param name="maxNodeId">The max node id.</param>
		public Bucket(NodeId minNodeId, NodeId maxNodeId)
		{
			_minNodeId = minNodeId;
			_maxNodeId = maxNodeId;
		}
		#endregion

		#region Public Properties
		public DateTime LastChanged
		{
			get
			{
				return _lastChanged;
			}
		}

		public NodeId MinNodeId
		{
			get
			{
				return _minNodeId;
			}
		}

		public NodeId MaxNodeId
		{
			get
			{
				return _maxNodeId;
			}
		}

		public List<Node> Nodes
		{
			get
			{
				return _nodes;
			}
		}
		#endregion

		#region Internal Properties
		internal Node Replacement
		{
			get
			{
				return _replacement;
			}
			set
			{
				_replacement = value;
			}
		}
		#endregion

		public bool Add(Node node)
		{
			// if the current bucket is not full we directly add the Node
			if (_nodes.Count < MaxCapacity)
			{
				_nodes.Add(node);
				_lastChanged = DateTime.UtcNow;
				return true;
			}

			// Try to replace an existing node
			for (int i = _nodes.Count - 1; i >= 0; i--)
			{
				if (_nodes[i].State != NodeState.Bad)
				{
					continue;
				}

				_nodes.RemoveAt(i);
				_nodes.Add(node);
				_lastChanged = DateTime.UtcNow;
				return true;
			}

			// This bucket is full
			return false;
		}

		public bool CanContain(Node node)
		{
			if (node == null)
			{
				throw new ArgumentNullException("node");
			}
			return CanContain(node.Id);
		}

		public bool CanContain(NodeId id)
		{
			if (id == null)
			{
				throw new ArgumentNullException("id");
			}
			return MinNodeId <= id && MaxNodeId > id;
		}

		public int CompareTo(Bucket other)
		{
			return _minNodeId.CompareTo(other._minNodeId);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as Bucket);
		}

		public bool Equals(Bucket other)
		{
			if (other == null)
			{
				return false;
			}
			return _minNodeId.Equals(other._minNodeId) && _maxNodeId.Equals(other._maxNodeId);
		}

		public override int GetHashCode()
		{
			return _minNodeId.GetHashCode() ^ _maxNodeId.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("Count: {2} Min: {0}  Max: {1}", _minNodeId, _maxNodeId, _nodes.Count);
		}

		internal void SortBySeen()
		{
			_nodes.Sort();
			_lastChanged = DateTime.UtcNow;
		}
	}
}
