namespace Zen.Trunk.Torrent.Client.Messages.Libtorrent
{
	using System;
	using System.Security.Cryptography;
	using Zen.Trunk.Torrent.Bencoding;
	using Zen.Trunk.Torrent.Common;

	public class LTMetadata : ExtensionMessage
	{
		public static readonly ExtensionSupport Support = CreateSupport("LT_metadata");
		private static readonly BEncodedString MessageTypeKey = "msg_type";
		private static readonly BEncodedString PieceKey = "piece";
		private static readonly BEncodedString TotalSizeKey = "total_size";
		private static readonly int BLOCK_SIZE = 16000;

		private enum eMessageType
		{
			Request = 0,
			Data = 1,
			Reject = 2
		}

		private int _piece;
		private eMessageType _messageType;
		private int _offset;
		private byte[] _metadata;
		private byte _messageId;

		public LTMetadata()
		{
			_piece = 0;
			_messageType = eMessageType.Request;
		}

		internal override void Handle(PeerId id)
		{
			if (!ClientEngine.SupportsFastPeer)
			{
				throw new MessageException("Libtorrent extension messages not supported");
			}

			// Extract the message id
			_messageId = id.ExtensionSupports.Find(
				(l) => l.Name == Support.Name).MessageId;

			// Process according to current message type
			switch (_messageType)
			{
				case eMessageType.Request:
					if (id.TorrentManager.PieceManager.MyBitField[_piece])
					{
						_messageType = eMessageType.Data;
					}
					else
					{
						_messageType = eMessageType.Reject;
					}
					id.Enqueue(this);//only send the piece requested
					break;
				case eMessageType.Data:
					if ((_piece + 1) * BLOCK_SIZE < _metadata.Length) // if not last piece request another
					{
						_messageType = eMessageType.Request;
						_piece++;
						id.Enqueue(this);
					}
					else
					{
						using (SHA1 s = SHA1.Create())
						{
							// Check the metadata hash is the same as infohash
							if (!Toolbox.ByteMatch(id.TorrentManager.Torrent.InfoHash, s.ComputeHash(_metadata)))
							{
								return;
							}
						}

						BEncodedDictionary d = (BEncodedDictionary)BEncodedDictionary.Decode(_metadata);
						//id.TorrentManager.Torrent.ProcessInfo (d);
						//id.TorrentManager.Torrent.haveAll = true;
						id.TorrentManager.Start();
					}
					break;
				case eMessageType.Reject:
					break;//do nothing when rejected or flood until other peer send the missing piece? 
				default:
					throw new MessageException(string.Format("Invalid messagetype in LTMetadata: {0}", _messageType));
			}

		}

		public override int ByteLength
		{
			// 4 byte length, 1 byte BT id, 1 byte LT id, 1 byte payload
			get
			{ //TODO depend of message type and of value
				return 4 + 1 + 1 + 1;
			}
		}

		public override void Decode(byte[] buffer, int offset, int length)
		{
			BEncodedValue val;
			BEncodedDictionary d = BEncodedDictionary.Decode<BEncodedDictionary>(buffer, offset, length, true);
			int totalSize = 0;

			if (d.TryGetValue(MessageTypeKey, out val))
			{
				_messageType = (eMessageType)((BEncodedNumber)val).Number;
			}
			if (d.TryGetValue(PieceKey, out val))
			{
				_piece = (int)((BEncodedNumber)val).Number;
			}
			if (d.TryGetValue(TotalSizeKey, out val))
			{
				totalSize = (int)((BEncodedNumber)val).Number;
				if (_metadata == null)
				{
					_metadata = new byte[totalSize];//create empty buffer
				}
				if (offset + d.LengthInBytes() < length)
				{
					Buffer.BlockCopy(
						buffer,
						offset + d.LengthInBytes(),
						_metadata,
						_piece * BLOCK_SIZE,
						Math.Min(totalSize - _piece * BLOCK_SIZE, BLOCK_SIZE));
				}
			}
		}

		public override int Encode(byte[] buffer, int offset)
		{
			if (!ClientEngine.SupportsFastPeer)
			{
				throw new MessageException("Libtorrent extension messages not supported");
			}

			int written = offset;

			written += Write(buffer, written, PeerMessage.LibTorrentMessageId);
			written += Write(buffer, written, _messageId);

			BEncodedDictionary dict = new BEncodedDictionary();
			dict.Add(MessageTypeKey, (BEncodedNumber)(int)_messageType);
			dict.Add(PieceKey, (BEncodedNumber)_piece);

			if (_messageType == eMessageType.Data)
			{
				dict.Add(TotalSizeKey, (BEncodedNumber)_metadata.Length);
				written += dict.Encode(buffer, written);
				written += Write(
					buffer,
					written,
					_metadata,
					_piece * BLOCK_SIZE,
					Math.Min(_metadata.Length - _piece * BLOCK_SIZE, BLOCK_SIZE));
			}
			else
			{
				written += dict.Encode(buffer, written);
			}

			CheckWritten(written - offset);
			return written - offset;
		}
	}
}
