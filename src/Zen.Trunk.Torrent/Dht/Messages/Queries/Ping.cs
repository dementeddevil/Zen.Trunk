namespace Zen.Trunk.Torrent.Dht.Messages
{
	using Zen.Trunk.Torrent.Bencoding;

	internal class Ping : QueryMessage
	{
		private static readonly BEncodedString QueryName = "ping";
		private static readonly ResponseCreator ResponseCreatorFunc =
			(d, m) => new PingResponse(d, m);

		public Ping(NodeId id)
			: base(id, QueryName, ResponseCreatorFunc)
		{
		}

		public Ping(BEncodedDictionary d)
			: base(d, ResponseCreatorFunc)
		{
		}

		public override void Handle(DhtEngine engine, Node node)
		{
			base.Handle(engine, node);

			PingResponse m = new PingResponse(engine.RoutingTable.LocalNode.Id);
			m.TransactionId = TransactionId;
			engine.MessageLoop.EnqueueSend(m, node.EndPoint);
		}
	}
}
