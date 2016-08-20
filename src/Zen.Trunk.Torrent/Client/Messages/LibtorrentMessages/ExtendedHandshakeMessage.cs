namespace Zen.Trunk.Torrent.Client.Messages.Libtorrent
{
	using System.Collections.Generic;
	using Zen.Trunk.Torrent.Bencoding;
	using Zen.Trunk.Torrent.Common;

	public class ExtendedHandshakeMessage : ExtensionMessage
	{
		private static readonly BEncodedString MaxRequestKey = "reqq";
		private static readonly BEncodedString PortKey = "p";
		private static readonly BEncodedString SupportsKey = "m";
		private static readonly BEncodedString VersionKey = "v";
		private static readonly BEncodedString MetadataSizeKey = "metadata_size";

		internal static readonly ExtensionSupport Support = new ExtensionSupport("LT_handshake", 0);

		private int localPort;
		private int maxRequests;
		private CloneableList<ExtensionSupport> supports;
		private string version;
		private int metadataSize;

		public override int ByteLength
		{
			get
			{
				// FIXME Implement this properly

				// The length of the payload, 4 byte length prefix, 1 byte BT message id, 1 byte LT message id
				return Create().LengthInBytes() + 4 + 1 + 1;
			}
		}

		public int MaxRequests
		{
			get
			{
				return maxRequests;
			}
		}

		public int LocalPort
		{
			get
			{
				return localPort;
			}
		}

		public CloneableList<ExtensionSupport> Supports
		{
			get
			{
				return supports;
			}
		}

		public string Version
		{
			get
			{
				return version ?? "";
			}
		}

		public int MetadataSize
		{
			get
			{
				return metadataSize;
			}
		}

		#region Constructors
		public ExtendedHandshakeMessage()
		{
			supports = new CloneableList<ExtensionSupport>(ExtensionMessage.SupportedMessages);
		}
		#endregion

		#region Methods

		public override void Decode(byte[] buffer, int offset, int length)
		{
			BEncodedValue val;
			BEncodedDictionary d = BEncodedDictionary.Decode<BEncodedDictionary>(buffer, offset, length, false);

			if (d.TryGetValue(MaxRequestKey, out val))
				maxRequests = (int)((BEncodedNumber)val).Number;
			if (d.TryGetValue(VersionKey, out val))
				version = ((BEncodedString)val).Text;
			if (d.TryGetValue(PortKey, out val))
				localPort = (int)((BEncodedNumber)val).Number;

			LoadSupports((BEncodedDictionary)d[SupportsKey]);

			if (d.TryGetValue(MetadataSizeKey, out val))
				metadataSize = (int)((BEncodedNumber)val).Number;
		}

		private void LoadSupports(BEncodedDictionary supports)
		{
			CloneableList<ExtensionSupport> list = new CloneableList<ExtensionSupport>();
			foreach (KeyValuePair<BEncodedString, BEncodedValue> k in supports)
				list.Add(new ExtensionSupport(k.Key.Text, (byte)((BEncodedNumber)k.Value).Number));

			this.supports = list;
		}

		public override int Encode(byte[] buffer, int offset)
		{
			int written = offset;
			BEncodedDictionary dict = Create();

			written += Write(buffer, written, dict.LengthInBytes() + 1 + 1);
			written += Write(buffer, written, PeerMessage.LibTorrentMessageId);
			written += Write(buffer, written, ExtendedHandshakeMessage.Support.MessageId);
			written += dict.Encode(buffer, written);

			CheckWritten(written - offset);
			return written - offset;
		}

		private BEncodedDictionary Create()
		{
			if (!ClientEngine.SupportsFastPeer)
				throw new MessageException("Libtorrent extension messages not supported");

			BEncodedDictionary mainDict = new BEncodedDictionary();
			BEncodedDictionary supportsDict = new BEncodedDictionary();

			mainDict.Add(MaxRequestKey, (BEncodedNumber)maxRequests);
			mainDict.Add(VersionKey, (BEncodedString)Version);
			mainDict.Add(PortKey, (BEncodedNumber)localPort);

			SupportedMessages.ForEach(delegate(ExtensionSupport s)
			{
				supportsDict.Add(s.Name, (BEncodedNumber)s.MessageId);
			});
			mainDict.Add(SupportsKey, supportsDict);

			mainDict.Add(MetadataSizeKey, (BEncodedNumber)metadataSize);

			return mainDict;
		}


		internal override void Handle(PeerId id)
		{
			if (!ClientEngine.SupportsFastPeer)
				throw new MessageException("Libtorrent extension messages not supported");

			// FIXME: Use the 'version' information
			// FIXME: Recreate the uri? Give warning?
			if (localPort > 0)
				id.Peer.LocalPort = localPort;
			id.MaxPendingRequests = maxRequests;
			id.ExtensionSupports = supports;

			// FIXME : Find a way to be more elegant!
			foreach (ExtensionSupport support in supports)
			{
				if (support.Name == "ut_pex" && id.PeerExchangeManager == null)
				{
					id.PeerExchangeManager = new PeerExchangeManager(id);
					break;
				}
			}
		}
		#endregion
	}
}
