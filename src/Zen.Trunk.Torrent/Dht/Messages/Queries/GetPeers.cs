namespace Zen.Trunk.Torrent.Dht.Messages
{
	using Zen.Trunk.Torrent.Bencoding;

	internal class GetPeers : QueryMessage
	{
		private static BEncodedString InfoHashKey = "info_hash";
		private static BEncodedString QueryName = "get_peers";
		private static ResponseCreator responseCreator =
			(d, m) => new GetPeersResponse(d, m);

		public GetPeers(NodeId id, NodeId infohash)
			: base(id, QueryName, responseCreator)
		{
			Parameters.Add(InfoHashKey, infohash.BencodedString());
		}

		public GetPeers(BEncodedDictionary d)
			: base(d, responseCreator)
		{
		}

		public NodeId InfoHash
		{
			get
			{
				return new NodeId((BEncodedString)Parameters[InfoHashKey]);
			}
		}

		public override void Handle(DhtEngine engine, Node node)
		{
			base.Handle(engine, node);

			BEncodedString token = engine.TokenManager.GenerateToken(node);
			GetPeersResponse response = new GetPeersResponse(engine.RoutingTable.LocalNode.Id, token);
			response.TransactionId = TransactionId;
			if (engine.Torrents.ContainsKey(InfoHash))
			{
				BEncodedList list = new BEncodedList();
				foreach (Node n in engine.Torrents[InfoHash])
				{
					list.Add(n.CompactPort());
				}
				response.Values = list;
			}
			else
			{
				// Is this right?
				response.Nodes = Node.CompactNode(engine.RoutingTable.GetClosest(InfoHash));
			}

			engine.MessageLoop.EnqueueSend(response, node.EndPoint);
		}
	}
}