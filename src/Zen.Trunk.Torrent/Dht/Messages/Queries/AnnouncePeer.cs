namespace Zen.Trunk.Torrent.Dht.Messages
{
	using System.Collections.Generic;
	using Zen.Trunk.Torrent.Bencoding;

	class AnnouncePeer : QueryMessage
	{
		private static BEncodedString InfoHashKey = "info_hash";
		private static BEncodedString QueryName = "announce_peer";
		private static BEncodedString PortKey = "port";
		private static BEncodedString TokenKey = "token";
		private static ResponseCreator ResponseCreatorFunc =
			(d, m) => new AnnouncePeerResponse(d, m);

		public AnnouncePeer(NodeId id, NodeId infoHash, BEncodedNumber port, BEncodedString token)
			: base(id, QueryName, ResponseCreatorFunc)
		{
			Parameters.Add(InfoHashKey, infoHash.BencodedString());
			Parameters.Add(PortKey, port);
			Parameters.Add(TokenKey, token);
		}

		public AnnouncePeer(BEncodedDictionary d)
			: base(d, ResponseCreatorFunc)
		{
		}

		internal NodeId InfoHash
		{
			get
			{
				return new NodeId((BEncodedString)Parameters[InfoHashKey]);
			}
		}

		internal BEncodedNumber Port
		{
			get
			{
				return (BEncodedNumber)Parameters[PortKey];
			}
		}

		internal BEncodedString Token
		{
			get
			{
				return (BEncodedString)Parameters[TokenKey];
			}
		}

		public override void Handle(DhtEngine engine, Node node)
		{
			base.Handle(engine, node);

			if (!engine.Torrents.ContainsKey(InfoHash))
			{
				engine.Torrents.Add(InfoHash, new List<Node>());
			}

			DhtMessage response;
			if (engine.TokenManager.VerifyToken(node, Token))
			{
				engine.Torrents[InfoHash].Add(node);
				response = new AnnouncePeerResponse(engine.RoutingTable.LocalNode.Id);
			}
			else
			{
				response = new ErrorMessage(ErrorCode.ProtocolError, "Invalid or expired token received");
			}

			engine.MessageLoop.EnqueueSend(response, node.EndPoint);
		}
	}
}
