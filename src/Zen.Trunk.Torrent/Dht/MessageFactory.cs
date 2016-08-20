namespace Zen.Trunk.Torrent.Dht.Messages
{
	using System.Collections.Generic;
	using Zen.Trunk.Torrent.Bencoding;

	delegate DhtMessage Creator(BEncodedDictionary dictionary);
	delegate DhtMessage ResponseCreator(BEncodedDictionary dictionary, QueryMessage message);

	internal static class MessageFactory
	{
		private static readonly string QueryNameKey = "q";
		private static BEncodedString MessageTypeKey = "y";
		private static BEncodedString TransactionIdKey = "t";
		private static Dictionary<BEncodedString, QueryMessage> _messages = new Dictionary<BEncodedString, QueryMessage>();
		private static Dictionary<BEncodedString, Creator> _queryDecoders = new Dictionary<BEncodedString, Creator>();

		static MessageFactory()
		{
			_queryDecoders.Add("announce_peer", (d) => new AnnouncePeer(d));
			_queryDecoders.Add("find_node", (d) => new FindNode(d));
			_queryDecoders.Add("get_peers", (d) => new GetPeers(d));
			_queryDecoders.Add("ping", (d) => new Ping(d));
		}

		public static int RegisteredMessages
		{
			get
			{
				return _messages.Count;
			}
		}

		internal static bool IsRegistered(BEncodedString transactionId)
		{
			return _messages.ContainsKey(transactionId);
		}

		public static void RegisterSend(QueryMessage message)
		{
			_messages.Add(message.TransactionId, message);
		}

		public static bool UnregisterSend(QueryMessage message)
		{
			return _messages.Remove(message.TransactionId);
		}

		public static DhtMessage DecodeMessage(BEncodedDictionary dictionary)
		{
			QueryMessage msg = null;

			if (dictionary[MessageTypeKey].Equals(QueryMessage.QueryType))
			{
				return _queryDecoders[(BEncodedString)dictionary[QueryNameKey]](dictionary);
			}
			else if (dictionary[MessageTypeKey].Equals(ErrorMessage.ErrorType))
			{
				return new ErrorMessage(dictionary);
			}
			else
			{
				BEncodedString key = (BEncodedString)dictionary[TransactionIdKey];
				if (_messages.TryGetValue(key, out msg))
				{
					_messages.Remove(key);
					return msg.ResponseCreator(dictionary, msg);
				}
				else
				{
					throw new MessageException(ErrorCode.GenericError, string.Format("{0}: {1}. {2}: {3}",
						"Response message with bad transaction:", key, "full message:", dictionary));
				}
			}
		}
	}
}
