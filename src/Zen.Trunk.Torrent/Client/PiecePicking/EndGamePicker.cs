namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Text;
	using System.Collections.Generic;
	using Zen.Trunk.Torrent.Client.Messages;
	using Zen.Trunk.Torrent.Common;
	using Zen.Trunk.Torrent.Client.Messages.Standard;
	using Zen.Trunk.Torrent.Client.Messages.FastPeer;

	internal class EndGamePicker : PiecePickerBase
	{
		#region Member Variables
		/// <summary>
		/// Used to synchronise access to the lists
		/// </summary>
		private object _requestsLocker = new object();

		/// <summary>
		/// A list of all the remaining pieces to download
		/// </summary>
		private CloneableList<Piece> _pieces;

		/// <summary>
		/// A list of all the blocks in the remaining pieces to download
		/// </summary>
		private List<Block> _blocks;

		/// <summary>
		/// Used to remember which blocks each peer is downloading
		/// </summary>
		private Dictionary<PeerId, List<Block>> _requests;

		/// <summary>
		/// Used to remember which peers are getting each block so i can issue cancel messages
		/// </summary>
		private Dictionary<Block, List<PeerId>> _blockRequestees;
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="EndGamePicker"/> class.
		/// </summary>
		/// <param name="myBitfield">My bitfield.</param>
		/// <param name="torrent">The torrent.</param>
		/// <param name="existingRequests">The existing requests.</param>
		public EndGamePicker(
			BitField myBitfield,
			TorrentObject torrent,
			CloneableList<Piece> existingRequests)
		{
			MyBitField = myBitfield;
			_requests = new Dictionary<PeerId, List<Block>>();
			_blockRequestees = new Dictionary<Block, List<PeerId>>();
			_pieces = new CloneableList<Piece>();

			// For all the pieces that we have *not* requested yet, add them into our list of pieces
			for (int i = 0; i < MyBitField.Length; i++)
			{
				if (!MyBitField[i])
				{
					_pieces.Add(new Piece(i, torrent));
				}
			}

			// Then take the dictionary of existing requests and put them into the list of pieces (overwriting as necessary)
			AddExistingRequests(existingRequests);

			_blocks = new List<Block>(_pieces.Count * _pieces[0].Blocks.Length);
			for (int i = 0; i < _pieces.Count; i++)
			{
				for (int j = 0; j < _pieces[i].Blocks.Length; j++)
				{
					_blocks.Add(_pieces[i].Blocks[j]);
				}
			}
		}
		#endregion

		#region Public Methods
		public override void Initialise(BitField ownBitfield, TorrentFile[] files, IEnumerable<Piece> requests, BitField unhashedPieces)
		{
			// Nothing to do - we initialised in our constructor...
		}

		public override List<Piece> ExportActiveRequests()
		{
			return new List<Piece>(_pieces);
		}

		public override void CancelTimedOutRequests()
		{
			// During endgame we never cancel timed out requests
		}

		public override int CurrentRequestCount()
		{
			return _blockRequestees.Count;
		}

		public override bool IsInteresting(PeerId id)
		{
			lock (_requestsLocker)
			{
				// See if the peer has any of the pieces in our list of "To Be Requested" pieces
				for (int i = 0; i < _pieces.Count; i++)
				{
					if (id.BitField[_pieces[i].Index])
					{
						return true;
					}
				}
				return false;
			}
		}

		public override RequestMessage PickPiece(PeerId id, List<PeerId> otherPeers)
		{
			MessageBundle bundle = PickPiece(id, otherPeers, 1);
			if (bundle == null)
			{
				return null;
			}
			return (RequestMessage)bundle.Messages[0];
		}

		public override MessageBundle PickPiece(PeerId id, List<PeerId> otherPeers, int count)
		{
			MessageBundle bundle = null;
			lock (_requestsLocker)
			{
				// For each block, see if the peer has that piece, and if so, request the block
				for (int blockIndex = 0; blockIndex < _blocks.Count; blockIndex++)
				{
					// Skip if peer doesn't have this block or we have it already
					if (!id.BitField[_blocks[blockIndex].PieceIndex] ||
						_blocks[blockIndex].Received)
					{
						continue;
					}

					// Create bundle if not already done
					if (bundle == null)
					{
						bundle = new MessageBundle();
					}

					// Get the block and move to end of the block list
					Block block = _blocks[blockIndex];
					_blocks.RemoveAt(blockIndex);
					_blocks.Add(block);
					block.Requested = true; // "Requested" isn't important for endgame picker. All that matters is if we have the piece or not.

					// Add the block to the list of blocks that we are 
					//	downloading from this peer
					if (!_requests.ContainsKey(id))
					{
						_requests.Add(id, new List<Block>());
					}
					_requests[id].Add(block);

					// Add the peer to the list of peers downloading this block
					if (!_blockRequestees.ContainsKey(block))
					{
						_blockRequestees.Add(block, new List<PeerId>());
					}
					_blockRequestees[block].Add(id);

					// Raise notification for this block
					Piece piece = _pieces.Find((p) => p.Index == block.PieceIndex);
					id.TorrentManager.PieceManager.RaiseBlockRequested(
						new BlockEventArgs(
							id.TorrentManager,
							block,
							piece,
							id));

					// Add request to the message bundle and stop if we have
					//	satisfied the caller's count.
					bundle.Messages.Add(block.CreateRequest(id));
					--count;
					if (count == 0)
					{
						break;
					}
				}

				return bundle;
			}
		}

		public override void ReceivedChokeMessage(PeerId id)
		{
			lock (_requestsLocker)
			{
				if (!(id.SupportsFastPeer && ClientEngine.SupportsFastPeer))
				{
					RemoveRequests(id);
				}
				else
				{
					// Cleanly remove any pending request messages from the send queue as there's no point in sending them
					PeerMessage message;
					int length = id.QueueLength;
					for (int i = 0; i < length; i++)
					{
						if ((message = id.Dequeue()) is RequestMessage)
						{
							RemoveRequests(id, (RequestMessage)message);
						}
						else
						{
							id.Enqueue(message);
						}
					}
				}
			}
			return;
		}

		public override PieceEvent ReceivedPieceMessage(BufferedIO data)
		{
			PeerId id = data.Id;
			Piece piece = _pieces.Find((item) => item.Index == data.PieceIndex);
			data.Piece = piece;
			if (data.Piece == null)
			{
				return PieceEvent.BlockNotRequested;
			}

			int blockIndex = Block.IndexOf(piece.Blocks, data.PieceOffset, data.Count);
			if (blockIndex == -1)
			{
				Logger.Log(id.Connection, "Invalid block start offset returned");
				return PieceEvent.BlockNotRequested;
			}

			// Was this block requested from this peer
			Block block = piece.Blocks[blockIndex];
			if (!_blockRequestees[block].Contains(id))
			{
				Logger.Log(id.Connection, "Invalid block start offset returned");
				return PieceEvent.BlockNotRequested;
			}

			if (piece.Blocks[blockIndex].Received)
			{
				Logger.Log(id.Connection, "Block already received");
				return PieceEvent.BlockNotRequested;
			}
			//throw new MessageException("Block already received");

			if (!piece.Blocks[blockIndex].Requested)
			{
				Logger.Log(id.Connection, "Block was not requested");
				return PieceEvent.BlockNotRequested;
			}
			//throw new MessageException("Block was not requested");

			if (!piece.Blocks[blockIndex].Received)
			{
				id.TorrentManager.FileManager.QueueWrite(data);
			}

			// Mark block as received
			piece.Blocks[blockIndex].Received = true;

			// Remove block from active list for this peer
			List<Block> activeRequests = _requests[id];
			activeRequests.Remove(block);

			// Remove peer from entities downloading this block
			List<PeerId> activeRequestees = _blockRequestees[block];
			activeRequestees.Remove(id);

			// Cancel remaining entities downloading this block
			for (int i = 0; i < activeRequestees.Count; i++)
			{
				lock (activeRequestees[i])
				{
					activeRequestees[i].EnqueueAt(
						new CancelMessage(
							data.PieceIndex, 
							block.StartOffset, 
							block.RequestLength), 0);
				}
			}

			// Remove peer list for this block
			activeRequestees.Clear();
			_blockRequestees.Remove(block);

			return PieceEvent.BlockWrittenToDisk;
		}

		public override void ReceivedRejectRequest(PeerId id, RejectRequestMessage message)
		{
			lock (_requestsLocker)
			{
				if (!_requests.ContainsKey(id))
				{
					throw new MessageException("Received reject request for a piece i'm not requesting");
				}

				List<Block> pieces = _requests[id];
				Piece piece = _pieces.Find((p) => p.Index == message.PieceIndex);
				int blockIndex = message.StartOffset / Piece.BlockSize;
				Block block = piece[blockIndex];

				if (_requests[id].Contains(block))
				{
					_requests[id].Remove(block);
					_blockRequestees[block].Remove(id);
					id.AmRequestingPiecesCount--;
					id.TorrentManager.PieceManager.RaiseBlockRequestCancelled(
						new BlockEventArgs(
							id.TorrentManager, 
							block, 
							piece, 
							id));
				}
			}
		}

		public override void RemoveRequests(PeerId id)
		{
			if (!_requests.ContainsKey(id))
				return;

			List<Block> blocks = _requests[id];
			for (int i = 0; i < blocks.Count; i++)
			{
				Block block = blocks[i];
				id.AmRequestingPiecesCount--;

				Piece piece = _pieces.Find((p) => p.Index == block.PieceIndex);
				id.TorrentManager.PieceManager.RaiseBlockRequestCancelled(
					new BlockEventArgs(
						id.TorrentManager,
						blocks[i], 
						piece,
						id));

				if (_blockRequestees.ContainsKey(block))
				{
					List<PeerId> requestees = _blockRequestees[block];
					requestees.Remove(id);
					if (requestees.Count == 0)
					{
						_blockRequestees.Remove(block);
					}
				}
			}
			blocks.Clear();
		}

		public override void Reset()
		{
			UnhashedPieces.SetAll(false);
			_requests.Clear();
		}
		#endregion

		#region Private Methods
		private void AddExistingRequests(CloneableList<Piece> existingRequests)
		{
			foreach (Piece p in existingRequests)
			{
				// If the piece has already been put into the list of pieces, we want to overwrite that
				// entry with this one. Otherwise we just add this piece in.
				int index = _pieces.IndexOf(p);
				if (index == -1)
				{
					_pieces.Add(p);
				}
				else
				{
					_pieces[index] = p;
				}

				// For each block in that piece that has been requested and not received
				// we put that block in the peers list of 'requested' blocks.
				// We also add the peer to the list of people who we are requesting that block off.
				foreach (Block b in p)
				{
					if (b.Requested && !b.Received)
					{
						List<PeerId> activePeers;
						if (!_blockRequestees.TryGetValue(b, out activePeers))
						{
							activePeers = new List<PeerId> ();
							_blockRequestees.Add(b, activePeers);
						}
						if (!activePeers.Contains(b.RequestedOff))
						{
							activePeers.Add(b.RequestedOff);
						}

						List<Block> activeBlocks;
						if (!_requests.TryGetValue(b.RequestedOff, out activeBlocks))
						{
							activeBlocks = new List<Block>();
							_requests.Add(b.RequestedOff, activeBlocks);
						}
						if (!activeBlocks.Contains(b))
						{
							activeBlocks.Add(b);
						}
					}
				}
			}
		}

		private void RemoveRequests(PeerId id, RequestMessage requestMessage)
		{
			Piece piece = _pieces.Find((p) => p.Index == requestMessage.PieceIndex);
			int blockIndex = requestMessage.StartOffset / Piece.BlockSize;
			Block block = piece[blockIndex];

			_requests[id].Remove(block);
			_blockRequestees[block].Remove(id);
			id.TorrentManager.PieceManager.RaiseBlockRequestCancelled(
				new BlockEventArgs(
					id.TorrentManager,
					block,
					piece,
					id));
		}
		#endregion
	}
}
