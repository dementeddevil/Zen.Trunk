namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Generic;
	using Zen.Trunk.Torrent.Client.Messages.Standard;
	using Zen.Trunk.Torrent.Common;

	/// <summary>
	/// Generates a sliding window with high, medium, and low priority sets. The high priority set is downloaded first and in order.
	/// The medium and low priority sets are downloaded rarest first.
	/// 
	/// This is intended to be used with a BitTorrent streaming application.
	/// 
	/// The high priority set represents pieces that are needed SOON. This set is updated by calling code, to adapt for events
	/// (e.g. user fast-forwards or seeks, etc.)
	/// </summary>
	public class SlidingWindowPicker : StandardPicker
	{
		#region Member Variables

		private int highPrioritySetSize;            // size of high priority set, in pieces
		private int ratio = 4;                      // ratio from medium priority to high priority set size

		private int highPrioritySetStart;           // gets updated by calling code, or as pieces get downloaded
		// this represents the last byte played in a video player, as the high priority
		// set designates pieces that are needed VERY SOON

		/// <summary>
		/// Gets or sets first "high priority" piece. The n pieces after this will be requested in-order,
		/// the rest of the file will be treated rarest-first
		/// </summary>
		public int HighPrioritySetStart
		{
			get
			{
				return this.highPrioritySetStart;
			}
			set
			{
				if (this.highPrioritySetStart < value)
					this.highPrioritySetStart = value;
			}
		}

		/// <summary>
		/// Gets or sets the size, in pieces, of the high priority set.
		/// </summary>
		public int HighPrioritySetSize
		{
			get
			{
				return this.highPrioritySetSize;
			}
			set
			{
				this.highPrioritySetSize = value;
			}
		}

		/// <summary>
		/// This is the size ratio between the medium and high priority sets. Equivalent to mu in Tribler's Give-to-get paper.
		/// Default value is 4.
		/// </summary>
		public int MediumToHighRatio
		{
			get
			{
				return ratio;
			}
			set
			{
				ratio = value;
			}
		}

		/// <summary>
		/// Read-only value for size of the medium priority set. To set the medium priority size, use MediumToHighRatio.
		/// </summary>
		public int MediumPrioritySetSize
		{
			get
			{
				return this.highPrioritySetSize * ratio;
			}
		}

		#endregion Member Variables

		#region Constructors

		/// <summary>
		/// Empty constructor for changing piece pickers
		/// </summary>
		public SlidingWindowPicker()
		{
		}


		/// <summary>
		/// Creates a new piece picker with support for prioritization of files. The sliding window will be positioned to the start
		/// of the first file to be downloaded
		/// </summary>
		/// <param name="bitField">The bitfield associated with the torrent</param>
		/// <param name="torrentFiles">The files that are available in this torrent</param>
		/// <param name="highPrioritySetSize">Size of high priority set</param>
		internal SlidingWindowPicker(int highPrioritySetSize)
			: this(highPrioritySetSize, 4)
		{
		}


		/// <summary>
		/// Create a new SlidingWindowPicker with the given set sizes. The sliding window will be positioned to the start
		/// of the first file to be downloaded
		/// </summary>
		/// <param name="bitField">The bitfield associated with the torrent</param>
		/// <param name="torrentFiles">The files that are available in this torrent</param>
		/// <param name="highPrioritySetSize">Size of high priority set</param>
		/// <param name="mediumToHighRatio">Size of medium priority set as a multiple of the high priority set size</param>
		internal SlidingWindowPicker(int highPrioritySetSize, int mediumToHighRatio)
			: base()
		{
			this.LinearPickingEnabled = false;

			this.highPrioritySetSize = highPrioritySetSize;
			this.ratio = mediumToHighRatio;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="ownBitfield"></param>
		/// <param name="files"></param>
		/// <param name="requests"></param>
		/// <param name="unhashedPieces"></param>
		public override void Initialise(BitField ownBitfield, TorrentFile[] files, IEnumerable<Piece> requests, BitField unhashedPieces)
		{
			base.Initialise(ownBitfield, files, requests, unhashedPieces);

			// set the high priority set start to the beginning of the first file that we have to download
			foreach (TorrentFile file in torrentFiles)
			{
				if (file.Priority == Priority.DoNotDownload)
					this.highPrioritySetStart = file.EndPieceIndex;
				else
					break;
			}
		}

		#endregion

		#region Methods
		/// <summary>
		/// Picks the first piece in the high priority window that the peer has.
		/// If the peer has no pieces in this window, get rarest piece in the 
		/// </summary>
		/// <param name="id">The id of the peer to request a piece off of</param>
		/// <param name="otherPeers">The other peers that are also downloading the same torrent</param>
		/// <returns></returns>
		public override RequestMessage PickPiece(PeerId id, List<PeerId> otherPeers)
		{
			RequestMessage message = null;
			try
			{
				// all other methods are synchronized because they only get called from within this synchronized block
				if (MyBitField.AllTrue)        // quick check to skip if we already have complete file
				{
					return null;
				}

				// If there is already a request on this peer, try to request the next block. If the peer is choking us, then the only
				// requests that could be continued would be existing "Fast" pieces.
				if ((message = ContinueExistingRequest(id)) != null)
				{
					return message;
				}

				// Then we check if there are any allowed "Fast" pieces to download
				if (id.IsChoking && (message = GetFastPiece(id)) != null)
				{
					return message;
				}

				// If the peer is choking, then we can't download from them as they had no "fast" pieces for us to download
				if (id.IsChoking)
				{
					return null;
				}

				// try to get a high priority piece from him
				if ((message = GetHighPriority(id)) != null)
				{
					return message;
				}

				// try continuing a piece that someone else has started
				if ((message = ContinueAnyExisting(id)) != null)
				{
					return message;
				}

				// otherwise, do getrarestfirst for medium priority set
				int mediumPrioritySetStart = this.highPrioritySetStart + this.highPrioritySetSize;

				// if the medium priority set start is off the end of the file, no need to do anything.
				if (mediumPrioritySetStart >= MyBitField.Length)
				{
					return null;
				}

				if ((message = GetStandardRequest(id, otherPeers, mediumPrioritySetStart,
					Math.Min(MyBitField.Length - 1, mediumPrioritySetStart + this.MediumPrioritySetSize - 1))) != null)
				{
					return message;
				}

				// We see if the peer has suggested any pieces we should request
				if ((message = GetSuggestedPiece(id)) != null)
				{
					return message;
				}

				// if there is room, GetRarestFirst over the rest of the file
				if (mediumPrioritySetStart + this.MediumPrioritySetSize < MyBitField.Length)
				{
					return GetStandardRequest(
						id, 
						otherPeers, 
						mediumPrioritySetStart + this.MediumPrioritySetSize,
						MyBitField.Length - 1);
				}
				return null;
			}
			finally
			{
				CancelTimedOutRequests();

				if (message != null)
				{
					foreach (Piece p in requests)
					{
						if (p.Index != message.PieceIndex)
							continue;

						int index = Block.IndexOf(p.Blocks, message.StartOffset, message.RequestLength);
						id.TorrentManager.PieceManager.RaiseBlockRequested(new BlockEventArgs(id.TorrentManager, p.Blocks[index], p, id));
						break;
					}
				}
			}
		}


		/// <summary>
		/// Get the first piece from the high priority set that hasn't finished downloading that the peer has.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private RequestMessage GetHighPriority(PeerId id)
		{
			Piece p = null;

			for (int i = this.highPrioritySetStart; 
				i < this.highPrioritySetStart + this.highPrioritySetSize && i < MyBitField.Length; i++)
			{
				// if we still need this piece and they do not have it
				if (!MyBitField[i] && id.BitField[i])
				{
					// try to find an existing request
					foreach (Piece piece in this.requests)
					{
						if (piece.Index == i)
						{
							p = piece;
							break;
						}
					}

					// no request existing for it, create new one
					if (p == null)
					{
						p = new Piece(i, id.TorrentManager.Torrent);
						requests.Add(p);
					}

					// look through the blocks, find the first one that hasn't been received or requested
					for (int j = 0; j < p.Blocks.Length; j++)
					{
						if (!p.Blocks[j].Received && !p.Blocks[j].Requested)
						{
							p.Blocks[j].Requested = true;
							return p.Blocks[j].CreateRequest(id);
						}
					}
				}
			}

			// no high priority piece to get, return null
			return null;
		}

		#endregion

	}
}
