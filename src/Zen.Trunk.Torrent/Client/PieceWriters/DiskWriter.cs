namespace Zen.Trunk.Torrent.Client.PieceWriters
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Zen.Trunk.Torrent.Common;

	public class DiskWriter : PieceWriter
	{
		private Dictionary<TorrentFile, string> _paths;
		private FileStreamBuffer _streamsBuffer;

		public DiskWriter()
			: this(10)
		{
		}

		public DiskWriter(int maxOpenFiles)
		{
			_paths = new Dictionary<TorrentFile, string>();
			_streamsBuffer = new FileStreamBuffer(maxOpenFiles);
		}

		public int OpenFiles
		{
			get
			{
				return _streamsBuffer.Count;
			}
		}

		public override void Close(string path, TorrentFile[] files)
		{
			foreach (TorrentFile file in files)
			{
				string filePath = GenerateFilePath(path, file);
				_streamsBuffer.CloseStream(filePath);
			}
		}

		public override int Read(BufferedIO data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("buffer");
			}

			long offset = data.Offset;
			int count = data.Count;
			TorrentFile[] files = data.Files;
			long fileSize = files.Sum((item) => item.Length);
			if (offset < 0 || offset + count > fileSize)
			{
				throw new ArgumentOutOfRangeException("offset");
			}

			int fileIndex = 0;
			int bytesRead = 0;
			int totalRead = 0;

			// Find the file that contains the start of the data
			for (fileIndex = 0; fileIndex < files.Length; fileIndex++)
			{
				if (offset < files[fileIndex].Length)
				{
					break;
				}
				offset -= files[fileIndex].Length;
			}

			while (totalRead < count && fileIndex < files.Length)
			{
				TorrentFileStream stream = GetStream(data.Path, files[fileIndex], FileAccess.Read);
				stream.Seek(offset, SeekOrigin.Begin);
				offset = 0; // Any further files need to be read from the beginning
				bytesRead = stream.Read(data.Buffer.Array, data.Buffer.Offset + totalRead, count - totalRead);
				totalRead += bytesRead;
				fileIndex++;
			}
			//monitor.BytesSent(totalRead, TransferType.Data);
			data.ActualCount += totalRead;
			return totalRead;
		}

		public override void Write(BufferedIO data)
		{
			byte[] buffer = data.Buffer.Array;
			long offset = data.Offset;
			int count = data.Count;

			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}

			TorrentFile[] files = data.Files;
			long fileSize = files.Sum((item) => item.Length);
			if (offset < 0 || offset + count > fileSize)
			{
				throw new ArgumentOutOfRangeException("offset");
			}

			int fileIndex = 0;
			long bytesWritten = 0;
			long totalWritten = 0;
			long bytesWeCanWrite = 0;

			// Find the file that contains the start of the data
			for (fileIndex = 0; fileIndex < files.Length; fileIndex++)
			{
				if (offset < files[fileIndex].Length)
				{
					break;
				}
				offset -= files[fileIndex].Length;
			}

			while (totalWritten < count && fileIndex < files.Length)
			{
				TorrentFileStream stream = GetStream(data.Path, files[fileIndex], FileAccess.ReadWrite);
				stream.Seek(offset, SeekOrigin.Begin);

				// Find the maximum number of bytes we can write before we 
				//	reach the end of the file
				bytesWeCanWrite = files[fileIndex].Length - offset;

				// Any further files need to be written from the beginning of 
				//	the file
				offset = 0;

				// Determine the maximum amount of data we can write here
				bytesWritten = Math.Min(bytesWeCanWrite, (count - totalWritten));

				// Write the data
				stream.Write(buffer, data.Buffer.Offset + (int)totalWritten, (int)bytesWritten);

				// Any further data should be written to the next available file
				totalWritten += bytesWritten;
				fileIndex++;
			}

			data.FreeBuffer();
			//monitor.BytesReceived((int)totalWritten, TransferType.Data);
		}

		public override void Flush(string path, TorrentFile[] files)
		{
			// No buffering done here
		}

		public override void Flush(string path, TorrentFile[] files, int pieceIndex)
		{
			// No buffering done here
		}

		internal TorrentFileStream GetStream(string path, TorrentFile file, FileAccess access)
		{
			path = GenerateFilePath(path, file);
			return _streamsBuffer.GetStream(file, path, access);
		}

		protected override void DisposeManagedObjects()
		{
			_streamsBuffer.Dispose();
			base.DisposeManagedObjects();
		}

		protected virtual string GenerateFilePath(string path, TorrentFile file)
		{
			if (_paths.ContainsKey(file))
				return _paths[file];

			path = Path.Combine(path, file.Path);

			if (!Directory.Exists(Path.GetDirectoryName(path)) && !string.IsNullOrEmpty(Path.GetDirectoryName(path)))
				Directory.CreateDirectory(Path.GetDirectoryName(path));

			_paths[file] = path;

			return path;
		}
	}
}
