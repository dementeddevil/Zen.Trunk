using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Zen.Trunk.Torrent.Common;
using System.Threading.Tasks;

namespace Zen.Trunk.Torrent.Client
{
	public class BufferedIO : TaskCompletionSource<object>//, ICloneable
	{
		private ArraySegment<byte> _innerBuffer;
		//private ManualResetEvent _waitHandle;

		public int ActualCount
		{
			get;
			set;
		}
		
		public int BlockIndex
		{
			get
			{
				return PieceOffset / Zen.Trunk.Torrent.Client.Piece.BlockSize;
			}
		}
		
		public ArraySegment<byte> Buffer
		{
			get
			{
				return _innerBuffer;
			}
			internal set
			{
				_innerBuffer = value;
			}
		}

		public int Count
		{
			get;
			set;
		}

		internal Piece Piece
		{
			get;
			set;
		}

		internal PeerId Id
		{
			get;
			set;
		}

		public string Path
		{
			get;
			private set;
		}

		public int PieceIndex
		{
			get
			{
				return (int)(Offset / PieceLength);
			}
		}
		
		public int PieceOffset
		{
			get
			{
				return (int)(Offset % PieceLength);
			}
		}

		public int PieceLength
		{
			get;
			private set;
		}

		public long Offset
		{
			get;
			set;
		}

		public TorrentFile[] Files
		{
			get;
			private set;
		}

		/*public ManualResetEvent WaitHandle
		{
			get
			{
				return _waitHandle;
			}
			set
			{
				_waitHandle = value;
			}
		}*/

		internal BufferedIO(ArraySegment<byte> buffer, long offset, int count, int pieceLength, TorrentFile[] files, string path)
		{
			Path = path;
			Files = files;
			PieceLength = pieceLength;
			Initialise(buffer, offset, count);
		}

		public BufferedIO(ArraySegment<byte> buffer, int pieceIndex, int blockIndex, int count, int pieceLength, TorrentFile[] files, string path)
		{
			Path = path;
			Files = files;
			PieceLength = pieceLength;
			Initialise(buffer, (long)pieceIndex * pieceLength + blockIndex * Zen.Trunk.Torrent.Client.Piece.BlockSize, count);
		}

		private void Initialise(ArraySegment<byte> buffer, long offset, int count)
		{
			Buffer = buffer;
			Offset = offset;
			Count = count;
		}

		public void FreeBuffer()
		{
			ClientEngine.BufferManager.FreeBuffer(ref _innerBuffer);
		}

		public override string ToString()
		{
			return string.Format("Piece: {0} Block: {1} Count: {2}", PieceIndex, BlockIndex, Count);
		}

		/*object ICloneable.Clone()
		{
			return MemberwiseClone();
		}*/
	}
}
