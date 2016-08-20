namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Generic;
	using Zen.Trunk.Torrent.Client.Connections;
	using Zen.Trunk.Torrent.Client.Messages;
	using Zen.Trunk.Torrent.Client.Messages.FastPeer;
	using Zen.Trunk.Torrent.Client.Messages.Standard;
	using Zen.Trunk.Torrent.Common;

	/// <summary>
	/// Contains the logic for choosing what piece to download next
	/// </summary>
	public class PieceManager : IDisposable
	{
		#region Internal Fields
		/// <summary>
		/// For every 10 kB/sec upload a peer has, we request one extra piece
		/// above the standard amount from the peer.
		/// </summary>
		internal const int BonusRequestPerKb = 10;
		
		/// <summary>
		/// Normal number of requests made from a peer
		/// </summary>
		internal const int NormalRequestAmount = 2;
		
		/// <summary>
		/// Maximum number of requests sent per peer during end-game-mode
		/// </summary>
		internal const int MaxEndGameRequests = 2;
		#endregion

		#region Private Fields
		private PiecePickerBase _piecePicker;
		#endregion

		#region Public Events
		public event EventHandler<BlockEventArgs> BlockReceived;
		public event EventHandler<BlockEventArgs> BlockRequested;
		public event EventHandler<BlockEventArgs> BlockRequestCancelled;
		#endregion

		#region Constructors
		internal PieceManager(BitField bitfield, TorrentFile[] files)
		{
			_piecePicker = new StandardPicker();
			_piecePicker.Initialise(bitfield, files, new List<Piece>(), new BitField(bitfield.Length));
		}
		#endregion

		#region Properties
		/// <summary>
		/// This option changes the picking algorithm from rarest first to linear. This should
		/// only be enabled if the content being downloaded is streaming audio/video. It degrades
		/// overall performance of the swarm.
		/// </summary>
		public bool LinearPickingEnabled
		{
			get
			{
				return _piecePicker.LinearPickingEnabled;
			}
			set
			{
				_piecePicker.LinearPickingEnabled = value;
			}
		}

		/// <summary>
		/// Get the PiecePicker instance that is currently being used by the PieceManager
		/// </summary>
		public PiecePickerBase PiecePicker
		{
			get
			{
				return _piecePicker;
			}
		}

		public bool InEndGameMode
		{
			get
			{
				return _piecePicker is EndGamePicker;
			}
		}

		internal BitField MyBitField
		{
			get
			{
				return _piecePicker.MyBitField;
			}
		}

		internal BitField UnhashedPieces
		{
			get
			{
				return ((StandardPicker)_piecePicker).UnhashedPieces;
			}
		}
		#endregion

		#region Internal Methods
		/// <summary>
		/// Tries to add a piece request to the peers message queue.
		/// </summary>
		/// <param name="id">The peer to add the request too</param>
		/// <returns>True if the request was added</returns>
		internal bool AddPieceRequest(PeerId id)
		{
			PeerMessage msg;

			// If someone can upload to us fast, queue more pieces from them.
			//	But no more than 50 blocks.
			int maxRequests = PieceManager.NormalRequestAmount +
				(int)(id.Monitor.DownloadSpeed / 1024.0 / BonusRequestPerKb);
			maxRequests = Math.Min(maxRequests, 50);

			if (id.AmRequestingPiecesCount >= maxRequests)
			{
				return false;
			}

			if (this.InEndGameMode)// In endgame we only want to queue 2 pieces
			{
				if (id.AmRequestingPiecesCount > PieceManager.MaxEndGameRequests)
				{
					return false;
				}
			}

			if (id.Connection is HttpConnection)
			{
				// Number of pieces which fit into 1 MB *or* 1 piece, whichever is bigger
				int count = (1 * 1024 * 1024) / id.TorrentManager.Torrent.PieceLength;
				count = Math.Max(count, id.TorrentManager.Torrent.PieceLength);
				int blocksPerPiece = id.TorrentManager.Torrent.PieceLength / Piece.BlockSize;
				msg = this.PickPiece(id, id.TorrentManager.Peers.ConnectedPeers, count * blocksPerPiece);
			}
			else
			{
				msg = this.PickPiece(id, id.TorrentManager.Peers.ConnectedPeers);
			}
			if (msg == null)
			{
				return false;
			}

			id.Enqueue(msg);

			if (msg is RequestMessage)
			{
				id.AmRequestingPiecesCount++;
			}
			else
			{
				id.AmRequestingPiecesCount += ((MessageBundle)msg).Messages.Count;
			}

			return true;
		}

		internal bool IsInteresting(PeerId id)
		{
			// If torrent is complete then no peer is interesting
			if (id.TorrentManager.IsComplete)
			{
				return false;
			}

			// Always interested in seeders
			if (id.Peer.IsSeeder)
			{
				return true;
			}

			// Otherwise we need to do a full check
			return _piecePicker.IsInteresting(id);
		}

		internal int CurrentRequestCount()
		{
			return _piecePicker.CurrentRequestCount();
		}

		internal PeerMessage PickPiece(PeerId id, List<PeerId> otherPeers)
		{
			// Switch to end-game piece picker as required
			if ((this.MyBitField.Length - this.MyBitField.TrueCount < 15) &&
				(_piecePicker is StandardPicker))
			{
				StandardPicker picker = _piecePicker as StandardPicker;
				_piecePicker = new EndGamePicker(
					MyBitField,
					id.TorrentManager.Torrent,
					picker.Requests);
				picker.Dispose();
			}

			return _piecePicker.PickPiece(id, otherPeers);
		}

		internal MessageBundle PickPiece(PeerId id, List<PeerId> otherPeers, int count)
		{
			return _piecePicker.PickPiece(id, otherPeers, count);
		}

		internal void ReceivedChokeMessage(PeerId id)
		{
			_piecePicker.ReceivedChokeMessage(id);
		}

		internal void ReceivedRejectRequest(PeerId id, RejectRequestMessage msg)
		{
			_piecePicker.ReceivedRejectRequest(id, msg);
		}

		internal void RemoveRequests(PeerId id)
		{
			_piecePicker.RemoveRequests(id);
		}

		internal PieceEvent ReceivedPieceMessage(BufferedIO data)
		{
			return _piecePicker.ReceivedPieceMessage(data);
		}

		internal void Reset()
		{
			_piecePicker.Reset();
		}

		internal void ChangePicker(PiecePickerBase picker, TorrentFile[] files)
		{
			picker.Initialise(_piecePicker.MyBitField, files, _piecePicker.ExportActiveRequests(), _piecePicker.UnhashedPieces);
			_piecePicker.Dispose();
			_piecePicker = picker;
		}
		#endregion

		#region Event Firing Code
		internal void RaiseBlockReceived(BlockEventArgs args)
		{
			Toolbox.RaiseAsyncEvent<BlockEventArgs>(BlockReceived, args.TorrentManager, args);
		}

		internal void RaiseBlockRequested(BlockEventArgs args)
		{
			Toolbox.RaiseAsyncEvent<BlockEventArgs>(BlockRequested, args.TorrentManager, args);
		}

		internal void RaiseBlockRequestCancelled(BlockEventArgs args)
		{
			Toolbox.RaiseAsyncEvent<BlockEventArgs>(BlockRequestCancelled, args.TorrentManager, args);
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			_piecePicker.Dispose();
		}

		#endregion
	}
}
