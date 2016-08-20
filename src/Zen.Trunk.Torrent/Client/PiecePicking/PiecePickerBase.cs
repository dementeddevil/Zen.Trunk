using System;
using System.Collections.Generic;
using System.Text;
using Zen.Trunk.Torrent.Common;
using Zen.Trunk.Torrent.Client.Messages.Standard;
using Zen.Trunk.Torrent.Client.Messages.FastPeer;
using Zen.Trunk.Torrent.Client.Messages;

namespace Zen.Trunk.Torrent.Client
{
	public abstract class PiecePickerBase : IDisposable
	{
		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="PiecePickerBase"/> class.
		/// </summary>
		protected PiecePickerBase()
		{
		}
		#endregion

		#region Public Properties
		public bool Disposed
		{
			get;
			private set;
		}

		public bool LinearPickingEnabled
		{
			get;
			set;
		}

		public BitField MyBitField
		{
			get;
			protected set;
		}

		public BitField UnhashedPieces
		{
			get;
			protected set;
		}

		#endregion Properties

		#region Public Methods
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			DisposeManagedObjects();
		}

		public abstract int CurrentRequestCount();

		public abstract void CancelTimedOutRequests();
		public abstract List<Piece> ExportActiveRequests();
		public abstract bool IsInteresting(PeerId id);
		public abstract void Initialise(BitField ownBitfield, TorrentFile[] files, IEnumerable<Piece> requests, BitField unhashedPieces);
		public abstract RequestMessage PickPiece(PeerId id, List<PeerId> otherPeers);
		public abstract MessageBundle PickPiece(PeerId id, List<PeerId> otherPeers, int count);
		public abstract void ReceivedChokeMessage(PeerId id);
		public abstract void ReceivedRejectRequest(PeerId id, RejectRequestMessage message);
		public abstract PieceEvent ReceivedPieceMessage(BufferedIO data);
		public abstract void RemoveRequests(PeerId id);
		public abstract void Reset();

		#endregion

		#region Protected Methods
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		protected virtual void DisposeManagedObjects()
		{
			Disposed = true;
		}
		#endregion
	}
}
