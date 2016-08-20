namespace Zen.Trunk.Torrent.Client.Messages.Libtorrent
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Concurrent;

	public abstract class ExtensionMessage : PeerMessage
	{
		private static readonly byte _handshakeMessageId = 0;
		private static ConcurrentDictionary<byte, Func<TorrentManager, PeerMessage>> _messageDict;
		private static byte _nextId;
		internal static readonly List<ExtensionSupport> SupportedMessages = new List<ExtensionSupport>();

		static ExtensionMessage()
		{
			_handshakeMessageId = 0;
			_nextId = 1;

			_messageDict = new ConcurrentDictionary<byte, Func<TorrentManager, PeerMessage>>();

			Register(_handshakeMessageId, (manager) => new ExtendedHandshakeMessage());

			Register(_nextId, (manager) => new LTChat());
			SupportedMessages.Add(new ExtensionSupport("LT_chat", _nextId++));

			Register(_nextId, (manager) => new LTMetadata());
			SupportedMessages.Add(new ExtensionSupport("LT_metadata", _nextId++));

			Register(_nextId, (manager) => new PeerExchangeMessage());
			SupportedMessages.Add(new ExtensionSupport("ut_pex", _nextId++));
		}

		public byte MessageId
		{
			get;
			set;
		}

		public static void Register(byte identifier, Func<TorrentManager, PeerMessage> creator)
		{
			if (creator == null)
			{
				throw new ArgumentNullException("creator");
			}

			_messageDict.TryAdd(identifier, creator);
		}

		public new static PeerMessage DecodeMessage(ArraySegment<byte> buffer, int offset, int count, TorrentManager manager)
		{
			return DecodeMessage(buffer.Array, buffer.Offset + offset, count, manager);
		}

		public new static PeerMessage DecodeMessage(byte[] buffer, int offset, int count, TorrentManager manager)
		{
			Func<TorrentManager, PeerMessage> creator;
			if (!_messageDict.TryGetValue(buffer[offset], out creator))
			{
				return new UnknownMessage();
			}

			PeerMessage message = creator(manager);
			message.Decode(buffer, offset + 1, count - 1);
			return message;
		}

		protected static ExtensionSupport CreateSupport(string name)
		{
			return SupportedMessages.Find((s) => s.Name == name);
		}
	}
}
