namespace Zen.Trunk.Torrent.Client
{
	using System;
	using Zen.Trunk.Torrent.Common;

	public class Piece
	{
		internal static readonly int BlockSize = (1 << 14); // 16kB

		#region Private Fields
		private Block[] _blocks;
		private int _index;
		private int _totalReceived;
		private int _totalRequested;
		private int _totalWritten;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Piece"/> class.
		/// </summary>
		/// <param name="pieceIndex">Index of the piece.</param>
		/// <param name="torrent">The torrent.</param>
		internal Piece(int pieceIndex, TorrentObject torrent)
		{
			_index = pieceIndex;
			if (pieceIndex == torrent.Pieces.Count - 1)      // Request last piece. Special logic needed
			{
				InitLastPiece(torrent);
			}
			else
			{
				InitNormalPiece(torrent);
			}
		}
		#endregion

		#region Public Properties
		public Block this[int index]
		{
			get
			{
				return this._blocks[index];
			}
		}

		internal Block[] Blocks
		{
			get
			{
				return this._blocks;
			}
		}

		public bool AllBlocksRequested
		{
			get
			{
				return this._totalRequested == BlockCount;
			}
		}

		public bool AllBlocksReceived
		{
			get
			{
				return this._totalReceived == BlockCount;
			}
		}

		public bool AllBlocksWritten
		{
			get
			{
				return this._totalWritten == BlockCount;
			}
		}

		public int BlockCount
		{
			get
			{
				return this._blocks.Length;
			}
		}

		public int Index
		{
			get
			{
				return this._index;
			}
		}

		public bool NoBlocksRequested
		{
			get
			{
				return this._totalRequested == 0;
			}
		}

		public int TotalReceived
		{
			get
			{
				return this._totalReceived;
			}
			internal set
			{
				this._totalReceived = value;
			}
		}

		public int TotalRequested
		{
			get
			{
				return this._totalRequested;
			}
			internal set
			{
				this._totalRequested = value;
			}
		}

		public int TotalWritten
		{
			get
			{
				return _totalWritten;
			}
			internal set
			{
				this._totalWritten = value;
			}
		}
		#endregion

		#region Public Methods
		public override bool Equals(object obj)
		{
			Piece p = obj as Piece;
			return (p == null) ? false : this._index.Equals(p._index);
		}

		public System.Collections.IEnumerator GetEnumerator()
		{
			return this._blocks.GetEnumerator();
		}

		public override int GetHashCode()
		{
			return this._index;
		}
		#endregion

		#region Private Methods
		private void InitNormalPiece(TorrentObject torrent)
		{
			int numberOfPieces = (int)Math.Ceiling(
				((double)torrent.PieceLength / BlockSize));

			_blocks = new Block[numberOfPieces];
			for (int i = 0; i < numberOfPieces; ++i)
			{
				_blocks[i] = new Block(this, i * BlockSize, BlockSize);
			}

			if ((torrent.PieceLength % BlockSize) != 0)     // I don't think this would ever happen. But just in case
			{
				_blocks[_blocks.Length - 1] = new Block(
					this,
					_blocks[_blocks.Length - 1].StartOffset,
					(int)(torrent.PieceLength - _blocks[_blocks.Length - 1].StartOffset));
			}
		}

		private void InitLastPiece(TorrentObject torrent)
		{
			int bytesRemaining = Convert.ToInt32(torrent.Size - ((long)torrent.Pieces.Count - 1) * torrent.PieceLength);
			int numberOfBlocks = bytesRemaining / BlockSize;
			if (bytesRemaining % BlockSize != 0)
			{
				++numberOfBlocks;
			}

			_blocks = new Block[numberOfBlocks];
			int i = 0;
			while (bytesRemaining - BlockSize > 0)
			{
				_blocks[i] = new Block(this, i * BlockSize, BlockSize);
				bytesRemaining -= BlockSize;
				++i;
			}

			if (bytesRemaining > 0)
			{
				_blocks[i] = new Block(this, i * BlockSize, bytesRemaining);
			}
		}
		#endregion
	}
}