namespace Zen.Trunk.Torrent.Dht.Messages
{
	using Zen.Trunk.Torrent.Bencoding;

	internal class FindNode : QueryMessage
	{
		private static BEncodedString TargetKey = "target";
		private static BEncodedString QueryName = "find_node";
		private static ResponseCreator ResponseCreatorFunc =
			(d, m) => new FindNodeResponse(d, m);

		public NodeId Target
		{
			get
			{
				return new NodeId((BEncodedString)Parameters[TargetKey]);
			}
		}

		public FindNode(NodeId id, NodeId target)
			: base(id, QueryName, ResponseCreatorFunc)
		{
			Parameters.Add(TargetKey, target.BencodedString());
		}

		public FindNode(BEncodedDictionary d)
			: base(d, ResponseCreatorFunc)
		{
		}

		public override void Handle(DhtEngine engine, Node node)
		{
			base.Handle(engine, node);

			FindNodeResponse response = new FindNodeResponse(engine.RoutingTable.LocalNode.Id);
			response.TransactionId = TransactionId;

			Node targetNode = engine.RoutingTable.FindNode(Target);
			if (targetNode != null)
			{
				response.Nodes = targetNode.CompactNode();
			}
			else
			{
				response.Nodes = Node.CompactNode(engine.RoutingTable.GetClosest(Target));
			}

			engine.MessageLoop.EnqueueSend(response, node.EndPoint);
		}
	}
}