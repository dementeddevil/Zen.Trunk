namespace Zen.Trunk.Torrent.Dht.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using Zen.Trunk.Torrent.Bencoding;
	using Zen.Trunk.Torrent.Common;
	using Zen.Trunk.Torrent.Client.Messages;

	internal abstract class DhtMessage : Message
	{
		internal static bool UseVersionKey = true;
		protected static readonly BEncodedString IdKey = "id";

		private static BEncodedString DhtVersion = VersionInfo.DhtClientVersion;
		private static BEncodedString TransactionIdKey = "t";
		private static BEncodedString VersionKey = "v";
		private static BEncodedString MessageTypeKey = "y";
		private static BEncodedString EmptyString = string.Empty;

		private BEncodedDictionary _properties = new BEncodedDictionary();

		/// <summary>
		/// Initializes a new instance of the <see cref="DhtMessage"/> class.
		/// </summary>
		/// <param name="messageType">Type of the message.</param>
		protected DhtMessage(BEncodedString messageType)
		{
			_properties.Add(TransactionIdKey, null);
			_properties.Add(MessageTypeKey, messageType);
			if (UseVersionKey)
			{
				_properties.Add(VersionKey, DhtVersion);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DhtMessage"/> class.
		/// </summary>
		/// <param name="dictionary">The dictionary.</param>
		protected DhtMessage(BEncodedDictionary dictionary)
		{
			_properties = dictionary;
		}

		public BEncodedString ClientVersion
		{
			get
			{
				BEncodedValue val;
				if (_properties.TryGetValue(VersionKey, out val))
					return (BEncodedString)val;
				return EmptyString;
			}
		}

		public BEncodedString MessageType
		{
			get
			{
				return (BEncodedString)_properties[MessageTypeKey];
			}
		}

		public BEncodedString TransactionId
		{
			get
			{
				return (BEncodedString)_properties[TransactionIdKey];
			}
			set
			{
				_properties[TransactionIdKey] = value;
			}
		}

		public override int ByteLength
		{
			get
			{
				return _properties.LengthInBytes();
			}
		}

		protected BEncodedDictionary Properties
		{
			get
			{
				return _properties;
			}
		}

		internal abstract NodeId Id
		{
			get;
		}

		public override void Decode(byte[] buffer, int offset, int length)
		{
			_properties = BEncodedValue.Decode<BEncodedDictionary>(buffer, offset, length, false);
		}

		public override int Encode(byte[] buffer, int offset)
		{
			return _properties.Encode(buffer, offset);
		}

		public virtual void Handle(DhtEngine engine, Node node)
		{
			node.Seen();
		}
	}
}
