namespace Zen.Trunk.Torrent.Dht.Tasks
{
	using Zen.Trunk.Torrent.Dht.Messages;
	using System.Collections.Generic;
	using Zen.Trunk.Torrent.Bencoding;
	using System;
	using System.Threading.Tasks;

	internal class GetPeersTask
	{
		private DhtEngine _engine;
		private NodeId _infoHash;
		private SortedList<NodeId, NodeId> _closestNodes;
		private SortedList<NodeId, Node> _queriedNodes;

		public GetPeersTask(DhtEngine engine, byte[] infohash)
			: this(engine, new NodeId(infohash))
		{

		}

		public GetPeersTask(DhtEngine engine, NodeId infohash)
		{
			_engine = engine;
			_infoHash = infohash;
			_closestNodes = new SortedList<NodeId, NodeId>(Bucket.MaxCapacity);
			_queriedNodes = new SortedList<NodeId, Node>(Bucket.MaxCapacity * 2);
		}

		internal SortedList<NodeId, Node> ClosestActiveNodes
		{
			get
			{
				return _queriedNodes;
			}
		}

		public Task Execute()
		{
			List<Task> subTasks = new List<Task>();
			IEnumerable<Node> newNodes = _engine.RoutingTable.GetClosest(_infoHash);
			foreach (Node n in Node.CloserNodes(_infoHash, _closestNodes, newNodes, Bucket.MaxCapacity))
			{
				subTasks.Add(SendGetPeers(n));
			}
			return TaskExtra.WhenAllOrEmpty(subTasks.ToArray());
		}

		private async Task SendGetPeers(Node target)
		{
			NodeId distance = target.Id.Xor(_infoHash);
			_queriedNodes.Add(distance, target);

			GetPeers m = new GetPeers(_engine.LocalId, _infoHash);
			SendQueryTask task = new SendQueryTask(_engine, m, target);
			SendQueryEventArgs args = await task.Execute();

			// We want to keep a list of the top (K) closest nodes which have responded
			int index = _queriedNodes.Values.IndexOf(target);
			if (index >= Bucket.MaxCapacity || args.TimedOut)
			{
				_queriedNodes.RemoveAt(index);
			}

			if (args.TimedOut)
			{
				return;
			}

			GetPeersResponse response = (GetPeersResponse)args.Response;

			// Ensure that the local Node object has the token. There may/may not be
			// an additional copy in the routing table depending on whether or not
			// it was able to fit into the table.
			target.Token = response.Token;
			if (response.Values != null)
			{
				// We have actual peers!
				_engine.RaisePeersFound(_infoHash, Zen.Trunk.Torrent.Client.Peer.Decode(response.Values));
			}
			else if (response.Nodes != null)
			{
				// We got a list of nodes which are closer
				List<Task> subTasks = new List<Task>();
				IEnumerable<Node> newNodes = Node.FromCompactNode(response.Nodes);
				foreach (Node n in Node.CloserNodes(_infoHash, _closestNodes, newNodes, Bucket.MaxCapacity))
				{
					subTasks.Add(SendGetPeers(n));
				}
				await TaskExtra.WhenAllOrEmpty(subTasks.ToArray());
			}
		}
	}
}
