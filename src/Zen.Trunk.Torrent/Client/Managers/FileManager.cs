namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Security.Cryptography;
	using System.Threading;
	using Zen.Trunk.Torrent.Common;
	using System.Threading.Tasks;

	/// <summary>
	/// This class manages writing and reading of pieces from the disk
	/// </summary>
	public class FileManager
	{
		#region Private Fields
		private long _fileSize;                                  // The combined length of all the files
		private string _savePath;                                // The path where the base directory will be put
		private TorrentManager _manager;
		private TorrentFile[] _files;
		private int _pieceLength;
		#endregion

		#region Public Events

		public event EventHandler<BlockEventArgs> BlockWritten;

		#endregion Public Events

		#region Constructors

		/// <summary>
		/// Creates a new FileManager with the supplied FileAccess
		/// </summary>
		/// <param name="files">The TorrentFiles you want to create/open on the disk</param>
		/// <param name="baseDirectory">The name of the directory that the files are contained in</param>
		/// <param name="savePath">The path to the directory that contains the baseDirectory</param>
		/// <param name="pieceLength">The length of a "piece" for this file</param>
		/// <param name="fileAccess">The access level for the files</param>
		internal FileManager(TorrentManager manager, TorrentFile[] files, int pieceLength, string savePath, string baseDirectory)
		{
			_manager = manager;
			_savePath = Path.Combine(savePath, baseDirectory);
			_files = files;
			_pieceLength = pieceLength;

			_fileSize = files.Sum((file) => file.Length);
		}
		#endregion

		#region Public Properties
		public TorrentFile[] Files
		{
			get
			{
				return _files;
			}
		}

		public long FileSize
		{
			get
			{
				return _fileSize;
			}
		}

		/// <summary>
		/// The length of a piece in bytes
		/// </summary>
		internal int PieceLength
		{
			get
			{
				return _pieceLength;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		internal string SavePath
		{
			get
			{
				return this._savePath;
			}
		}
		#endregion

		#region Methods
		internal bool CheckFilesExist()
		{
			return Array.Exists<TorrentFile>(
				_files,
				(f) =>
				{
					return File.Exists(GenerateFilePath(f, _savePath));
				});
		}

		/// <summary>
		/// Generates the full path to the supplied TorrentFile
		/// </summary>
		/// <param name="file">The TorrentFile to generate the full path to</param>
		/// <param name="baseDirectory">The name of the directory that the files are contained in</param>
		/// <param name="savePath">The path to the directory that contains the BaseDirectory</param>
		/// <returns>The full path to the TorrentFile</returns>
		private static string GenerateFilePath(TorrentFile file, string path)
		{
			path = Path.Combine(path, file.Path);

			if (!Directory.Exists(Path.GetDirectoryName(path)) &&
				!string.IsNullOrEmpty(Path.GetDirectoryName(path)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path));
			}

			return path;
		}

		/// <summary>
		/// Generates the hash for the given piece
		/// </summary>
		/// <param name="pieceIndex">The piece to generate the hash for</param>
		/// <returns>The 20 byte SHA1 hash of the supplied piece</returns>
		internal async Task<byte[]> GetHash(int pieceIndex, bool asynchronous)
		{
			int bytesToRead = 0;
			long pieceStartOffset = (long)_pieceLength * pieceIndex;
			BufferedIO io = null;
			ArraySegment<byte> hashBuffer = BufferManager.EmptyBuffer;

			using (SHA1 hasher = SHA1.Create())
			{
				hasher.Initialize();

				List<Task> pendingReads = new List<Task>();
				for (long pieceOffset = pieceStartOffset;
					pieceOffset < (pieceStartOffset + _pieceLength);
					pieceOffset += Piece.BlockSize)
				{
					hashBuffer = BufferManager.EmptyBuffer;
					ClientEngine.BufferManager.GetBuffer(ref hashBuffer, Piece.BlockSize);

					bytesToRead = Math.Min(Piece.BlockSize, (int)(_fileSize - pieceOffset));
					io = new BufferedIO(
						hashBuffer,
						pieceOffset,
						bytesToRead,
						_manager.Torrent.PieceLength,
						_manager.Torrent.Files,
						SavePath);
					pendingReads.Add(ReadAndHash(io, hasher));

					if (pendingReads.Count > 16)
					{
						await Task.WhenAll(pendingReads.ToArray());
						pendingReads.Clear();
					}

					if (bytesToRead != Piece.BlockSize)
					{
						break;
					}
				}

				if (pendingReads.Count > 0)
				{
					await Task.WhenAll(pendingReads.ToArray());
					pendingReads.Clear();
				}

				hasher.TransformFinalBlock(hashBuffer.Array, hashBuffer.Offset, 0);
				return hasher.Hash;
			}
		}

		private async Task ReadAndHash(BufferedIO io, SHA1 hasher)
		{
			// Queue the read and wait for it
			_manager.Engine.DiskManager.QueueRead(io);
			await io.Task;

			// Add the block to the hash
			ArraySegment<byte> hashBuffer = io.Buffer;
			hasher.TransformBlock(
				hashBuffer.Array,
				hashBuffer.Offset,
				io.ActualCount,
				hashBuffer.Array,
				hashBuffer.Offset);
			io.FreeBuffer();
		}


		/// <summary>
		/// Loads fast resume data if it exists
		/// </summary>
		/// <param name="manager">The manager to load fastresume data for</param>
		/// <returns></returns>
		/*internal static bool LoadFastResume(TorrentManager manager)
		{
			try
			{
				// FIXME: #warning Store all the fast resume in a 'data' file in a known location instead?
				// If we don't know where the .torrent is on disk, then don't save
				// fast resume data.
				if (!manager.Settings.FastResumeEnabled || string.IsNullOrEmpty(manager.Torrent.TorrentPath))
					return false;

				string fastResumePath = manager.Torrent.TorrentPath + ".fresume";
				// We can't load fast resume data if we don't have a filepath
				if (!manager.Settings.FastResumeEnabled || !File.Exists(fastResumePath))
					return false;

				XmlSerializer fastResume = new XmlSerializer(typeof(int[]));
				using (FileStream file = File.OpenRead(fastResumePath))
					manager.PieceManager.MyBitField.FromArray((int[])fastResume.Deserialize(file), manager.Torrent.Pieces.Count);

				// We need to delete the old fast resume data so in the event of a crash we don't 
				// accidently reload it and think we've downloaded less data than we actually have
				File.Delete(fastResumePath);
				return true;
			}
			catch
			{
				manager.PieceManager.MyBitField.SetAll(false);
				return false;
			}
		}*/


		/// <summary>
		/// Moves all files from the current path to the new path. The existing directory structure is maintained
		/// </summary>
		/// <param name="path"></param>
		public async Task MoveFiles(string path, bool overWriteExisting)
		{
			if (_manager.State != TorrentState.Stopped)
			{
				throw new TorrentException("Cannot move the files when the torrent is active");
			}

			// Wait for disk manager to close file streams
			await _manager.Engine.DiskManager.CloseFileStreams(SavePath, _files);

			// Move each file in the torrent
			for (int i = 0; i < this._files.Length; i++)
			{
				string oldPath = GenerateFilePath(_files[i], _savePath);
				string newPath = GenerateFilePath(_files[i], path);

				if (!File.Exists(oldPath))
				{
					continue;
				}

				bool fileExists = File.Exists(newPath);
				if (!overWriteExisting && fileExists)
				{
					throw new TorrentException("File already exists and overwriting is disabled");
				}

				if (fileExists)
				{
					File.Delete(newPath);
				}

				File.Move(oldPath, newPath);
			}

			// Update save path for the torrent
			_savePath = path;
		}

		/// <summary>
		/// Queues a block of data to be written asynchronously
		/// </summary>
		/// <param name="id">The peer who sent the block</param>
		/// <param name="recieveBuffer">The array containing the block</param>
		/// <param name="message">The PieceMessage</param>
		/// <param name="piece">The piece that the block to be written is part of</param>
		internal void QueueWrite(BufferedIO data)
		{
			_manager.Engine.DiskManager.QueueWrite(data);
		}

		internal void RaiseBlockWritten(BlockEventArgs args)
		{
			Toolbox.RaiseAsyncEvent<BlockEventArgs>(BlockWritten, this._manager, args);
		}

		/// <summary>
		/// This method reads 'count' number of bytes from the filestream starting at index 'offset'.
		/// The bytes are read into the buffer starting at index 'bufferOffset'.
		/// </summary>
		/// <param name="buffer">The byte[] containing the bytes to write</param>
		/// <param name="bufferOffset">The offset in the byte[] at which to save the data</param>
		/// <param name="offset">The offset in the file at which to start reading the data</param>
		/// <param name="count">The number of bytes to read</param>
		/// <returns>The number of bytes successfully read</returns>
		internal Task<int> ReadAsync(byte[] buffer, int bufferOffset, long offset, int count)
		{
			return _manager.Engine.DiskManager.ReadAsync(this._manager, buffer, bufferOffset, offset, count);
		}
		#endregion
	}
}
