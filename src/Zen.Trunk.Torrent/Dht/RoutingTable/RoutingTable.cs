namespace Zen.Trunk.Torrent.Dht
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;

	internal class RoutingTable
	{
		private readonly Node _localNode;
		private List<Bucket> _buckets = new List<Bucket>();

		public event EventHandler<NodeAddedEventArgs> NodeAdded;

		public RoutingTable()
			: this(new Node(NodeId.Create(), new IPEndPoint(IPAddress.Any, 0)))
		{
		}

		public RoutingTable(Node localNode)
		{
			if (localNode == null)
			{
				throw new ArgumentNullException("localNode");
			}

			_localNode = localNode;
			localNode.Seen();
			Add(new Bucket());
		}

		internal List<Bucket> Buckets
		{
			get
			{
				return _buckets;
			}
		}

		public Node LocalNode
		{
			get
			{
				return _localNode;
			}
		}

		public bool Add(Node node)
		{
			return Add(node, true);
		}

		private bool Add(Node node, bool raiseNodeAdded)
		{
			if (node == null)
				throw new ArgumentNullException("node");

			Bucket bucket = _buckets.Find((b) => b.CanContain(node));
			if (bucket.Nodes.Contains(node))
			{
				return false;
			}

			bool added = bucket.Add(node);
			if (added && raiseNodeAdded)
			{
				RaiseNodeAdded(node);
			}

			if (!added && bucket.CanContain(LocalNode) && Split(bucket))
			{
				added = Add(node, raiseNodeAdded);
			}

			return added;
		}

		private void RaiseNodeAdded(Node node)
		{
			EventHandler<NodeAddedEventArgs> handler = NodeAdded;
			if (handler != null)
			{
				handler(this, new NodeAddedEventArgs(node));
			}
		}

		private void Add(Bucket bucket)
		{
			_buckets.Add(bucket);
			_buckets.Sort();
		}

		internal Node FindNode(NodeId id)
		{
			foreach (Bucket b in _buckets)
			{
				foreach (Node n in b.Nodes)
				{
					if (n.Id.Equals(id))
					{
						return n;
					}
				}
			}

			return null;
		}

		private void Remove(Bucket bucket)
		{
			_buckets.Remove(bucket);
		}

		private bool Split(Bucket bucket)
		{
			// Sanity check to avoid infinite loop when adding same node
			if (bucket.MaxNodeId - bucket.MinNodeId < Bucket.MaxCapacity)
			{
				return false;
			}

			NodeId median = (bucket.MinNodeId + bucket.MaxNodeId) / 2;
			Bucket left = new Bucket(bucket.MinNodeId, median);
			Bucket right = new Bucket(median, bucket.MaxNodeId);

			Remove(bucket);
			Add(left);
			Add(right);

			foreach (Node n in bucket.Nodes)
			{
				Add(n, false);
			}

			if (bucket.Replacement != null)
			{
				Add(bucket.Replacement, false);
			}

			return true;
		}

		public int CountNodes()
		{
			return _buckets.Sum((b) => b.Nodes.Count);
		}

		public IList<Node> GetClosest(NodeId target)
		{
			SortedList<NodeId, Node> sortedNodes = new SortedList<NodeId, Node>(Bucket.MaxCapacity);

			foreach (Node candidateNode in _buckets.SelectMany((b) => b.Nodes))
			{
				// Determine the distance of the candidate to the target
				NodeId distance = candidateNode.Id.Xor(target);

				// If node list is at capacity then see if we can replace one
				if (sortedNodes.Count == Bucket.MaxCapacity)
				{
					// If candidate is further away than last node then skip
					if (distance > sortedNodes.Keys[sortedNodes.Count - 1])
					{
						continue;
					}

					// Remove the last node to make room
					sortedNodes.RemoveAt(sortedNodes.Count - 1);
				}

				// Add candidate to the list
				sortedNodes.Add(distance, candidateNode);
			}

			return new List<Node>(sortedNodes.Values);
		}

		internal void Clear()
		{
			_buckets.Clear();
			Add(new Bucket());
		}
	}
}
