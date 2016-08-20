namespace Zen.Trunk.Torrent.Client.PieceWriters
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Zen.Trunk.Torrent.Common;
	using System.Threading;

	public interface IPieceWriter : IDisposable
	{
		void Close(string path, TorrentFile[] files);
		void Flush(string path, TorrentFile[] files);
		void Flush(string path, TorrentFile[] files, int pieceIndex);
		int Read(BufferedIO data);
		void Write(BufferedIO data);
	}

	public abstract class PieceWriter : IPieceWriter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PieceWriter"/> class.
		/// </summary>
		protected PieceWriter()
		{
		}

		public void Dispose()
		{
			DisposeManagedObjects();
		}

		public int ReadChunk(BufferedIO data)
		{
			// Copy the inital buffer, offset and count so the values won't
			// be lost when doing the reading.
			ArraySegment<byte> orig = data.Buffer;
			long origOffset = data.Offset;
			int origCount = data.Count;

			int read = 0;
			int totalRead = 0;

			// Read the data in chunks. For every chunk we read,
			// advance the offset and subtract from the count. This
			// way we can keep filling in the buffer correctly.
			while (totalRead != data.Count)
			{
				read = Read(data);
				if (read > 0)
				{
					data.Buffer = new ArraySegment<byte>(
						data.Buffer.Array, 
						data.Buffer.Offset + read, 
						data.Buffer.Count - read);
					data.Offset += read;
					data.Count -= read;
					totalRead += read;
				}

				if (read == 0 || data.Count == 0)
				{
					break;
				}
			}

			// Restore the original values so the object remains unchanged
			// as compared to when the user passed it in.
			data.Buffer = orig;
			data.Offset = origOffset;
			data.Count = origCount;

			// Set the actual count to the total number of bytes read
			data.ActualCount = totalRead;
			return totalRead;
		}

		public abstract void Close(string path, TorrentFile[] files);

		public abstract void Flush(string path, TorrentFile[] files);

		public abstract void Flush(string path, TorrentFile[] files, int pieceIndex);

		public abstract int Read(BufferedIO data);

		public abstract void Write(BufferedIO data);

		protected virtual void DisposeManagedObjects()
		{
		}
	}
}