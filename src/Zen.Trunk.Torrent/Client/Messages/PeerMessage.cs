namespace Zen.Trunk.Torrent.Client.Messages
{
	using System;
	using System.Collections.Concurrent;
	using System.Net;
	using Zen.Trunk.Torrent.Client.Messages.FastPeer;
	using Zen.Trunk.Torrent.Client.Messages.Libtorrent;
	using Zen.Trunk.Torrent.Client.Messages.Standard;

	public abstract class PeerMessage : Message
	{
		internal const byte LibTorrentMessageId = 20;
		private static ConcurrentDictionary<byte, Func<TorrentManager, PeerMessage>> messageDict;

		static PeerMessage()
		{
			messageDict = new ConcurrentDictionary<byte, Func<TorrentManager, PeerMessage>>();

			// Note - KeepAlive messages aren't registered as they have no payload or ID and are never 'decoded'
			//      - Handshake messages aren't registered as they are always the first message sent/received
			Register(AllowedFastMessage.MessageId, (manager) => new AllowedFastMessage());
			Register(BitfieldMessage.MessageId, (manager) => new BitfieldMessage(manager.Torrent.Pieces.Count));
			Register(CancelMessage.MessageId, (manager) => new CancelMessage());
			Register(ChokeMessage.MessageId, (manager) => new ChokeMessage());
			Register(HaveAllMessage.MessageId, (manager) => new HaveAllMessage());
			Register(HaveMessage.MessageId, (manager) => new HaveMessage());
			Register(HaveNoneMessage.MessageId, (manager) => new HaveNoneMessage());
			Register(InterestedMessage.MessageId, (manager) => new InterestedMessage());
			Register(NotInterestedMessage.MessageId, (manager) => new NotInterestedMessage());
			Register(PieceMessage.MessageId, (manager) => new PieceMessage(manager));
			Register(PortMessage.MessageId, (manager) => new PortMessage());
			Register(RejectRequestMessage.MessageId, (manager) => new RejectRequestMessage());
			Register(RequestMessage.MessageId, (manager) => new RequestMessage());
			Register(SuggestPieceMessage.MessageId, (manager) => new SuggestPieceMessage());
			Register(UnchokeMessage.MessageId, (manager) => new UnchokeMessage());

			// We register this solely so that the user cannot register their own message with this ID.
			// Actual decoding is handled with manual detection
			Register(LibTorrentMessageId, (manager) => new UnknownMessage());
		}

		public static PeerMessage DecodeMessage(ArraySegment<byte> buffer, int offset, int count, TorrentManager manager)
		{
			return DecodeMessage(buffer.Array, buffer.Offset + offset, count, manager);
		}

		public static PeerMessage DecodeMessage(byte[] buffer, int offset, int count, TorrentManager manager)
		{
			if (count < 4)
			{
				throw new ArgumentOutOfRangeException("A message must contain a 4 byte length prefix");
			}

			int messageLength = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(buffer, offset));
			if (messageLength > (count - 4))
			{
				throw new ArgumentException("Incomplete message detected");
			}

			if (buffer[offset + 4] == LibTorrentMessageId)
			{
				return ExtensionMessage.DecodeMessage(buffer, offset + 4 + 1, count - 4 - 1, manager);
			}

			Func<TorrentManager, PeerMessage> creator;
			if (!messageDict.TryGetValue(buffer[offset + 4], out creator))
			{
				return new UnknownMessage();
			}

			// The message length is given in the second byte and the message body follows directly after that
			// We decode up to the number of bytes Received. If the message isn't complete, throw an exception
			PeerMessage message = creator(manager);
			message.Decode(buffer, offset + 4 + 1, count - 4 - 1);
			return message;
		}

		internal abstract void Handle(PeerId id);

		private static void Register(byte identifier, Func<TorrentManager, PeerMessage> creator)
		{
			if (creator == null)
			{
				throw new ArgumentNullException("creator");
			}

			messageDict.TryAdd(identifier, creator);
		}
	}
}
