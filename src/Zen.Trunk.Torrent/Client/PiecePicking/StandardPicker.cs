namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Zen.Trunk.Torrent.Client.Messages;
	using Zen.Trunk.Torrent.Client.Messages.FastPeer;
	using Zen.Trunk.Torrent.Client.Messages.Standard;
	using Zen.Trunk.Torrent.Common;

	/// <summary>
	/// 
	/// </summary>
	public class StandardPicker : PiecePickerBase
	{
		#region Member Variables
		// This list is used to store the temporary bitfields created when calculating the rarest pieces
		private List<BitField> previousBitfields;

		// This is used to store the numerical representation of the priorities
		private int[] priorities;

		// A random number generator used to choose the starting index when downloading a piece randomly
		protected Random random = new Random();

		// The list of pieces that are currently being requested
		protected CloneableList<Piece> requests;

		// The list of files in the torrent being requested
		protected TorrentFile[] torrentFiles;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="StandardPicker"/> class.
		/// </summary>
		public StandardPicker()
		{
		}
		#endregion

		#region Public Properties
		public override int CurrentRequestCount()
		{
			return requests.Sum((piece) => piece.TotalRequested - piece.TotalReceived);

			/*return (int)ClientEngine.MainLoop.QueueWait((Func<object>)delegate
			{
				int result = 0;

				foreach (Piece p in this.requests)
					result += p.TotalRequested - p.TotalReceived;

				return result;
			});*/
		}

		internal CloneableList<Piece> Requests
		{
			get
			{
				return this.requests;
			}
		}
		#endregion

		#region Methods
		protected bool AlreadyHaveOrRequested(int index)
		{
			if (MyBitField[index])
				return true;

			for (int i = 0; i < requests.Count; i++)
				if (requests[i].Index == index)
					return true;

			return UnhashedPieces[index];
		}

		/// <summary>
		/// Continue requesting any existing piece
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		protected RequestMessage ContinueAnyExisting(PeerId id)
		{
			// If this peer is currently a 'dodgy' peer, then don't allow him to help with someone else's
			// piece request.
			if (id.Peer.RepeatedHashFails != 0)
			{
				return null;
			}

			// Otherwise, if this peer has any of the pieces that are currently being requested, try to
			// request a block from one of those pieces
			foreach (Piece p in this.requests)
			{
				// If the peer who this piece is assigned to is dodgy or if the blocks are all request or
				// the peer doesn't have this piece, we don't want to help download the piece.
				if (p.AllBlocksRequested || !id.BitField[p.Index] ||
					(p.Blocks[0].RequestedOff != null && p.Blocks[0].RequestedOff.Peer.RepeatedHashFails != 0))
				{
					continue;
				}

				for (int i = 0; i < p.Blocks.Length; i++)
				{
					if (!p.Blocks[i].Requested)
					{
						p.Blocks[i].Requested = true;
						return p.Blocks[i].CreateRequest(id);
					}
				}
			}

			return null;
		}

		protected RequestMessage ContinueExistingRequest(PeerId id)
		{
			foreach (Piece p in requests)
			{
				// For each piece that was assigned to this peer, try to request a block from it
				// A piece is 'assigned' to a peer if he is the first person to request a block from that piece
				if (p.AllBlocksRequested || !id.Equals(p.Blocks[0].RequestedOff))
				{
					continue;
				}

				for (int i = 0; i < p.BlockCount; i++)
				{
					if (p.Blocks[i].Requested)
					{
						continue;
					}

					p.Blocks[i].Requested = true;
					return p.Blocks[i].CreateRequest(id);
				}
			}

			// If we get here it means all the blocks in the pieces being downloaded by the peer are already requested
			return null;
		}

		protected RequestMessage GetFastPiece(PeerId id)
		{
			int requestIndex;

			// If fast peers isn't supported on both sides, then return null
			if (!id.SupportsFastPeer || !ClientEngine.SupportsFastPeer)
			{
				return null;
			}

			// Remove pieces in the list that we already have
			RemoveOwnedPieces(id.IsAllowedFastPieces);

			// For all the remaining fast pieces
			for (int i = 0; i < id.IsAllowedFastPieces.Count; i++)
			{
				// The peer may not always have the piece that is marked as 'allowed fast'
				if (!id.BitField[(int)id.IsAllowedFastPieces[i]])
				{
					continue;
				}

				// We request that piece and remove it from the list
				requestIndex = (int)id.IsAllowedFastPieces[i];
				id.IsAllowedFastPieces.RemoveAt(i);

				Piece p = new Piece(requestIndex, id.TorrentManager.Torrent);
				requests.Add(p);
				p.Blocks[0].Requested = true;
				return p.Blocks[0].CreateRequest(id);
			}

			return null;
		}

		protected void RemoveOwnedPieces(CloneableList<int> list)
		{
			while (true)
			{
				bool removed = false;

				for (int i = 0; i < list.Count; i++)
				{
					if (list[i] >= MyBitField.Length || AlreadyHaveOrRequested((int)list[i]))
					{
						list.RemoveAt(i);
						removed = true;
						break;
					}
				}

				if (!removed)
				{
					break;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="otherPeers"></param>
		/// <returns></returns>
		protected RequestMessage GetStandardRequest(PeerId id, List<PeerId> otherPeers)
		{
			return (RequestMessage)GetStandardRequest(id, otherPeers, 1).Messages[0];
		}

		protected MessageBundle GetStandardRequest(PeerId id, List<PeerId> otherPeers, int count)
		{
			return GetStandardRequest(id, otherPeers, 0, MyBitField.Length - 1, count);
		}

		/// <summary>
		/// When picking a piece, request a new piece normally
		/// </summary>
		/// <param name="id"></param>
		/// <param name="otherPeers"></param>
		/// <param name="startIndex">Starting point of allowed request piece range</param>
		/// <param name="endIndex">Ending point of allowed piece range</param>
		/// <returns></returns>
		protected RequestMessage GetStandardRequest(PeerId id, List<PeerId> otherPeers, int startIndex, int endIndex)
		{
			MessageBundle bundle = GetStandardRequest(id, otherPeers, startIndex, endIndex, 1);
			return (RequestMessage)(bundle == null ? bundle : bundle.Messages[0]);
		}

		private int CanRequest(BitField bitfield, int pieceStartIndex, int pieceEndIndex, ref int pieceCount)
		{
			int count = -1;
			int start = -1;

			while ((pieceStartIndex = bitfield.FirstTrue(pieceStartIndex, pieceEndIndex)) != -1)
			{
				if (AlreadyHaveOrRequested(pieceStartIndex) || !bitfield[pieceStartIndex])
				{
					pieceStartIndex++;
				}
				else
				{
					for (int i = 1; i < pieceCount; i++)
					{
						if ((pieceStartIndex + i) != bitfield.Length && !AlreadyHaveOrRequested(pieceStartIndex + i) && bitfield[pieceStartIndex + i])
							continue;

						// The peer does NOT have the piece or we already have it. Store the largest range
						// found so far.
						if (i > count)
						{
							count = i;
							start = pieceStartIndex;
						}

						break;
					}

					if (count == -1)
						return pieceStartIndex;

					pieceStartIndex++;
				}
			}

			pieceCount = count;
			return start;
		}

		protected virtual MessageBundle GetStandardRequest(PeerId id, List<PeerId> otherPeers, int startIndex, int endIndex, int count)
		{
			BitField current = null;
			Stack<BitField> rarestFirstBitfields = GenerateRarestFirst(id, otherPeers, startIndex, endIndex);

			try
			{
				while (rarestFirstBitfields.Count > 0)
				{
					current = rarestFirstBitfields.Pop();

					if (count > 1)
					{
						return PickRegularPiece(id, current, startIndex, endIndex, count);
					}
					else
					{
						return PickRandomPiece(id, current, startIndex, endIndex, count);
					}
				}

				return null;
			}

			finally
			{
				if (current != null)
					ClientEngine.BufferManager.FreeBitField(ref current);

				while (rarestFirstBitfields.Count > 0)
				{
					BitField popped = rarestFirstBitfields.Pop();
					ClientEngine.BufferManager.FreeBitField(ref popped);
				}
			}
		}

		private MessageBundle PickRegularPiece(PeerId id, BitField current, int startIndex, int endIndex, int count)
		{
			int piecesNeeded = 1 + (count * Piece.BlockSize) / id.TorrentManager.Torrent.PieceLength;
			int checkIndex = CanRequest(current, startIndex, endIndex, ref piecesNeeded);

			// Nothing to request.
			if (checkIndex == -1)
				return null;

			MessageBundle bundle = new MessageBundle();
			for (int i = 0; bundle.Messages.Count < count && i < piecesNeeded; i++)
			{
				// Request the piece
				Piece p = new Piece(checkIndex + i, id.TorrentManager.Torrent);
				requests.Add(p);

				for (int j = 0; j < p.Blocks.Length && bundle.Messages.Count < count; j++)
				{
					p.Blocks[j].Requested = true;
					bundle.Messages.Add(p.Blocks[j].CreateRequest(id));
				}
			}
			return bundle;
		}

		private MessageBundle PickRandomPiece(PeerId id, BitField current, int startIndex, int endIndex, int count)
		{
			MessageBundle bundle;
			int midPoint = random.Next(startIndex, endIndex + 1);

			if ((bundle = PickRegularPiece(id, current, startIndex, midPoint, count)) != null)
				return bundle;

			return PickRegularPiece(id, current, midPoint, endIndex, count);
		}

		/// <summary>
		/// Return a stack of bitfields corresponding to rarest pieces found in the region between startindex and endindex
		/// </summary>
		/// <param name="id"></param>
		/// <param name="otherPeers"></param>
		/// <param name="startIndex">Starting index of constrained piece region.</param>
		/// <param name="endIndex">Ending index of constrained piece region</param>
		/// <returns></returns>
		protected Stack<BitField> GenerateRarestFirst(PeerId id, List<PeerId> otherPeers, int startIndex, int endIndex)
		{
			Priority highestPriority = Priority.DoNotDownload;
			Stack<BitField> bitfields = new Stack<BitField>();
			BitField current = ClientEngine.BufferManager.GetBitField(MyBitField.Length);

			try
			{
				// Copy my bitfield into the buffer and invert it so it contains a list of pieces i want
				Buffer.BlockCopy(MyBitField.Array, 0, current.Array, 0, current.Array.Length * 4);
				current.Not();

				// For every file set to DoNotDownload, set it's indices to 'false'
				foreach (TorrentFile file in torrentFiles)
					if (file.Priority == Priority.DoNotDownload)
						for (int i = file.StartPieceIndex; i <= file.EndPieceIndex; i++)
							current[i] = false;

				// If a 'DoNotDownload' file shares a piece with a file we're supposed to download
				// ensure the index remains set correctly
				foreach (TorrentFile file in torrentFiles)
				{
					if (file.Priority != Priority.DoNotDownload)
					{
						current[file.StartPieceIndex] = !MyBitField[file.StartPieceIndex];
						current[file.EndPieceIndex] = !MyBitField[file.EndPieceIndex];
					}
				}

				// set all pieces outside of what we want to false, meaning that we don't want them
				for (int i = 0; i < startIndex; i++)
					current[i] = false;

				for (int i = endIndex + 1; i < current.Length; i++)
					current[i] = false;

				// Fastpath - If he's a seeder, there's no point in AND'ing his bitfield as nothing will be set false
				if (!id.Peer.IsSeeder)
					current.AndFast(id.BitField);

				// Check the priority of the availabe pieces and record the highest one found
				highestPriority = HighestPriorityAvailable(current);

				// If true, then there are no pieces to download from this peer
				if (highestPriority == Priority.DoNotDownload)
					return bitfields;

				// Store this bitfield as the first iteration of the Rarest First algorithm.
				bitfields.Push(current);

				// If we're doing linear picking, then we don't want to calculate the rarest pieces
				if (base.LinearPickingEnabled)
				{
					return bitfields;
				}

				// Get a cloned copy of the bitfield and begin iterating to find the rarest pieces
				current = ClientEngine.BufferManager.GetClonedBitField(current);

				for (int i = 0; i < otherPeers.Count; i++)
				{
					if (otherPeers[i].Connection == null || otherPeers[i].Peer.IsSeeder)
						continue;

					// currentBitfield = currentBitfield & (!otherBitfield)
					// This calculation finds the pieces this peer has that other peers *do not* have.
					// i.e. the rarest piece.
					current.AndNotFast(otherPeers[i].BitField);

					// If the bitfield now has no pieces or we've knocked out a file which is at
					// a high priority then we've completed our task
					if (current.AllFalseSecure() || highestPriority != HighestPriorityAvailable(current))
						break;

					// Otherwise push the bitfield on the stack and clone it and iterate again.
					bitfields.Push(current);
					current = ClientEngine.BufferManager.GetClonedBitField(current);
				}

				return bitfields;
			}
			finally
			{
				ClientEngine.BufferManager.FreeBitField(ref current);
			}
		}

		protected RequestMessage GetSuggestedPiece(PeerId id)
		{
			int requestIndex;
			// Remove any pieces that we already have
			RemoveOwnedPieces(id.SuggestedPieces);

			for (int i = 0; i < id.SuggestedPieces.Count; i++)
			{
				// A peer should only suggest a piece he has, but just in case.
				if (!id.BitField[id.SuggestedPieces[i]])
					continue;

				requestIndex = id.SuggestedPieces[i];
				id.SuggestedPieces.RemoveAt(i);
				Piece p = new Piece(requestIndex, id.TorrentManager.Torrent);
				this.requests.Add(p);
				p.Blocks[0].Requested = true;
				return p.Blocks[0].CreateRequest(id);
			}


			return null;
		}

		protected Priority HighestPriorityAvailable(BitField bitField)
		{
			Priority highestFound = Priority.DoNotDownload;

			// Find the Highest priority file that is in this torrent
			for (int i = 0; i < this.torrentFiles.Length; i++)
				if ((this.torrentFiles[i].Priority > highestFound) &&
					(bitField.FirstTrue(this.torrentFiles[i].StartPieceIndex, this.torrentFiles[i].EndPieceIndex) != -1))
					highestFound = this.torrentFiles[i].Priority;

			return highestFound;
		}

		public override bool IsInteresting(PeerId id)
		{
			BitField bitfield = ClientEngine.BufferManager.GetBitField(MyBitField.Length);
			try
			{
				Buffer.BlockCopy(MyBitField.Array, 0, bitfield.Array, 0, MyBitField.Array.Length * 4);

				bitfield.Not();
				bitfield.AndFast(id.BitField);
				if (!bitfield.AllFalseSecure())
					return true;                            // He's interesting if he has a piece we want

				return false;
			}
			finally
			{
				ClientEngine.BufferManager.FreeBitField(ref bitfield);
			}
		}

		public override RequestMessage PickPiece(PeerId id, List<PeerId> otherPeers)
		{
			MessageBundle bundle = PickPiece(id, otherPeers, 1);
			return (RequestMessage)(bundle == null ? bundle : bundle.Messages[0]);
		}

		public override MessageBundle PickPiece(PeerId id, List<PeerId> otherPeers, int count)
		{
			RequestMessage message;
			MessageBundle bundle = null;
			try
			{
				// If there is already a request on this peer, try to request the next block. If the peer is choking us, then the only
				// requests that could be continued would be existing "Fast" pieces.
				if ((message = ContinueExistingRequest(id)) != null)
					return (bundle = new MessageBundle(message));

				// Then we check if there are any allowed "Fast" pieces to download
				if (id.IsChoking && (message = GetFastPiece(id)) != null)
					return (bundle = new MessageBundle(message));

				// If the peer is choking, then we can't download from them as they had no "fast" pieces for us to download
				if (id.IsChoking)
					return null;

				// If we are only requesting 1 piece, then we can continue any existing. Otherwise we should try
				// to request the full amount first, then try to continue any existing.
				if (count == 1 && (message = ContinueAnyExisting(id)) != null)
					return (bundle = new MessageBundle(message));

				// We see if the peer has suggested any pieces we should request
				if ((message = GetSuggestedPiece(id)) != null)
					return (bundle = new MessageBundle(message));

				// Now we see what pieces the peer has that we don't have and try and request one
				if ((bundle = GetStandardRequest(id, otherPeers, count)) != null)
					return bundle;

				// If all else fails, ignore how many we're requesting and try to continue any existing
				if ((message = ContinueAnyExisting(id)) != null)
					return (bundle = new MessageBundle(message));

				return null;
			}
			finally
			{
				if (bundle != null)
				{
					foreach (RequestMessage m in bundle.Messages)
					{
						foreach (Piece p in requests)
						{
							if (p.Index != m.PieceIndex)
								continue;

							int index = Block.IndexOf(p.Blocks, m.StartOffset, m.RequestLength);
							id.TorrentManager.PieceManager.RaiseBlockRequested(new BlockEventArgs(id.TorrentManager, p.Blocks[index], p, id));
							break;
						}
					}
				}
			}
		}

		public override void CancelTimedOutRequests()
		{
			foreach (Piece p in requests)
				for (int i = 0; i < p.BlockCount; i++)
					if (p[i].RequestTimedOut)
						RemoveRequests(p[i].RequestedOff, new RequestMessage(p[i].PieceIndex, p[i].StartOffset, p[i].RequestLength));
		}

		public override void RemoveRequests(PeerId id)
		{
			foreach (Piece p in requests)
			{
				for (int i = 0; i < p.Blocks.Length; i++)
				{
					if (p.Blocks[i].Requested && !p.Blocks[i].Received && id.Equals(p.Blocks[i].RequestedOff))
					{
						p.Blocks[i].CancelRequest();
						id.AmRequestingPiecesCount--;
						id.TorrentManager.PieceManager.RaiseBlockRequestCancelled(new BlockEventArgs(id.TorrentManager, p.Blocks[i], p, id));
					}
				}
			}
			requests.RemoveAll(delegate(Piece p)
			{
				return p.NoBlocksRequested;
			});
		}

		protected void RemoveRequests(PeerId id, RequestMessage message)
		{
			foreach (Piece p in requests)
			{
				if (p.Index != message.PieceIndex)
					continue;

				int blockIndex = Block.IndexOf(p.Blocks, message.StartOffset, message.RequestLength);
				if (blockIndex != -1)
				{
					if (p.Blocks[blockIndex].Requested && !p.Blocks[blockIndex].Received && id.Equals(p.Blocks[blockIndex].RequestedOff))
					{
						p.Blocks[blockIndex].CancelRequest();
						id.AmRequestingPiecesCount--;
						id.TorrentManager.PieceManager.RaiseBlockRequestCancelled(new BlockEventArgs(id.TorrentManager, p.Blocks[blockIndex], p, id));
						return;
					}
				}
			}
		}

		public override PieceEvent ReceivedPieceMessage(BufferedIO data)
		{
			PeerId id = data.Id;
			Piece piece = requests.Find(delegate(Piece p)
			{
				return p.Index == data.PieceIndex;
			});
			data.Piece = piece;
			if (piece == null)
			{
				Logger.Log(data.Id.Connection, "Received block from unrequested piece");
				return PieceEvent.BlockNotRequested;
			}

			// Pick out the block that this piece message belongs to
			int blockIndex = Block.IndexOf(piece.Blocks, data.PieceOffset, data.Count);
			if (blockIndex == -1 || !id.Equals(piece.Blocks[blockIndex].RequestedOff))
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

			piece.Blocks[blockIndex].Received = true;
			id.AmRequestingPiecesCount--;
			id.TorrentManager.PieceManager.RaiseBlockReceived(new BlockEventArgs(data));
			id.TorrentManager.FileManager.QueueWrite(data);

			if (piece.AllBlocksReceived)
			{
				// FIXME review usage of the unhashedpieces variable
				if (!MyBitField[piece.Index])
					UnhashedPieces[piece.Index] = true;
				requests.Remove(piece);
			}

			return PieceEvent.BlockWriteQueued;
		}

		public override void ReceivedChokeMessage(PeerId id)
		{
			// If fast peer peers extensions are not supported on both sides, all pending requests are implicitly rejected
			if (!(id.SupportsFastPeer && ClientEngine.SupportsFastPeer))
			{
				this.RemoveRequests(id);
			}
			else
			{
				// Cleanly remove any pending request messages from the send queue as there's no point in sending them
				PeerMessage message;
				int length = id.QueueLength;
				for (int i = 0; i < length; i++)
					if ((message = id.Dequeue()) is RequestMessage)
						RemoveRequests(id, (RequestMessage)message);
					else
						id.Enqueue(message);
			}
		}

		public override void ReceivedRejectRequest(PeerId id, RejectRequestMessage rejectRequestMessage)
		{
			foreach (Piece p in requests)
			{
				if (p.Index != rejectRequestMessage.PieceIndex)
					continue;

				int blockIndex = Block.IndexOf(p.Blocks, rejectRequestMessage.StartOffset, rejectRequestMessage.RequestLength);
				if (blockIndex == -1)
					return;

				if (!p.Blocks[blockIndex].Received && id.Equals(p.Blocks[blockIndex].RequestedOff))
				{
					p.Blocks[blockIndex].CancelRequest();
					id.AmRequestingPiecesCount--;
					id.TorrentManager.PieceManager.RaiseBlockRequestCancelled(new BlockEventArgs(id.TorrentManager, p.Blocks[blockIndex], p, id));
				}
				break;
			}
		}

		public override void Reset()
		{
			UnhashedPieces.SetAll(false);
			this.requests.Clear();
		}

		#endregion

		public override List<Piece> ExportActiveRequests()
		{
			return new List<Piece>(requests);
		}

		public override void Initialise(BitField ownBitfield, TorrentFile[] files, IEnumerable<Piece> requests, BitField unhashedPieces)
		{
			MyBitField = ownBitfield;
			this.torrentFiles = files;

			this.previousBitfields = new List<BitField>();
			this.previousBitfields.Add(new BitField(MyBitField.Length));
			this.priorities = (int[])Enum.GetValues(typeof(Priority));
			this.requests = new CloneableList<Piece>(16);

			// Order the priorities in decending order of priority. i.e. Immediate is first, and DoNotDownload is last
			Array.Sort<int>(this.priorities);
			Array.Reverse(this.priorities);

			this.requests.AddRange(requests);
			UnhashedPieces = unhashedPieces.Clone();
		}
	}
}
