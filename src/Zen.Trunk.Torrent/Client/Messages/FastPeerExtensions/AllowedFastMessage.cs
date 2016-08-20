namespace Zen.Trunk.Torrent.Client.Messages.FastPeer
{
	using System.Text;

	public class AllowedFastMessage : PeerMessage
	{
		internal static readonly byte MessageId = 0x11;
		private readonly int messageLength = 5;

		#region Member Variables
		public int PieceIndex
		{
			get { return this.pieceIndex; }
		}
		private int pieceIndex;
		#endregion


		#region Constructors
		internal AllowedFastMessage()
		{
		}

		internal AllowedFastMessage(int pieceIndex)
		{
			this.pieceIndex = pieceIndex;
		}
		#endregion


		#region Methods
		public override int Encode(byte[] buffer, int offset)
		{
			if (!ClientEngine.SupportsFastPeer)
				throw new ProtocolException("Message encoding not supported");

			int written = offset;

			written += Write(buffer, written, messageLength);
			written += Write(buffer, written, MessageId);
			written += Write(buffer, written, pieceIndex);

			CheckWritten(written - offset);
			return written - offset;
		}

		public override void Decode(byte[] buffer, int offset, int length)
		{
			if (!ClientEngine.SupportsFastPeer)
				throw new ProtocolException("Message decoding not supported");

			this.pieceIndex = ReadInt(buffer, offset);
		}

		internal override void Handle(PeerId id)
		{
			if (!id.SupportsFastPeer)
				throw new MessageException("Peer shouldn't support fast peer messages");

			id.IsAllowedFastPieces.Add(this.pieceIndex);
		}


		public override int ByteLength
		{
			get { return this.messageLength + 4; }
		}
		#endregion


		#region Overidden Methods
		public override bool Equals(object obj)
		{
			AllowedFastMessage msg = obj as AllowedFastMessage;
			if (msg == null)
				return false;

			return this.pieceIndex == msg.pieceIndex;
		}


		public override int GetHashCode()
		{
			return this.pieceIndex.GetHashCode();
		}


		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(24);
			sb.Append("AllowedFast");
			sb.Append(" Index: ");
			sb.Append(this.pieceIndex);
			return sb.ToString();
		}
		#endregion
	}
}
