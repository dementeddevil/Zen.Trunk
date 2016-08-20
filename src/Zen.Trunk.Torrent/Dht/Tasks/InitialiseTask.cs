namespace Zen.Trunk.Torrent.Dht.Tasks
{
	using Zen.Trunk.Torrent.Dht.Messages;
	using System;
	using System.Net;
	using System.Collections.Generic;
	using Zen.Trunk.Torrent.Bencoding;
	using System.Threading.Tasks;

	internal class InitialiseTask
	{
		private DhtEngine _engine;
		private byte[] _initialNodes;
		private SortedList<NodeId, NodeId> _nodes =
			new SortedList<NodeId, NodeId>();

		public InitialiseTask(DhtEngine engine, byte[] initialNodes)
		{
			_engine = engine;
			_initialNodes = initialNodes;
		}

		public async Task Execute()
		{
			bool canRetry = true;
			while (canRetry)
			{
				// If we were given a list of nodes to load at the start, use them
				if (_initialNodes != null)
				{
					BEncodedList list = (BEncodedList)BEncodedValue.Decode(_initialNodes);
					foreach (BEncodedString s in list)
					{
						_engine.Add(Node.FromCompactNode(s.TextBytes, 0));
					}
					canRetry = false;
				}
				else
				{
					Node utorrent = new Node(NodeId.Create(), new IPEndPoint(
						Dns.GetHostEntry("router.bittorrent.com").AddressList[0], 6881));
					await SendFindNode(new Node[] { utorrent });
					canRetry = false;
				}

				// If we were given a list of initial nodes and they were all dead,
				// initialise again except use the utorrent router.
				if (_initialNodes != null && _engine.RoutingTable.CountNodes() < 10)
				{
					_initialNodes = null;
					canRetry = true;
				}
			}

			_engine.RaiseStateChanged(State.Ready);
		}

		private Task SendFindNode(IEnumerable<Node> newNodes)
		{
			List<Task> subTasks = new List<Task>();
			foreach (Node node in Node.CloserNodes(_engine.LocalId, _nodes, newNodes, Bucket.MaxCapacity))
			{
				subTasks.Add(SendFindNodeCore(node));
			}
			return TaskExtra.WhenAllOrEmpty(subTasks.ToArray());
		}

		private async Task SendFindNodeCore(Node node)
		{
			FindNode request = new FindNode(_engine.LocalId, _engine.LocalId);
			SendQueryTask task = new SendQueryTask(_engine, request, node);
			SendQueryEventArgs args = await task.Execute();
			if (!args.TimedOut)
			{
				FindNodeResponse response = (FindNodeResponse)args.Response;
				await SendFindNode(Node.FromCompactNode(response.Nodes));
			}
		}
	}
}