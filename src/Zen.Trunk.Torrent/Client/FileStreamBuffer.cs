namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Zen.Trunk.Torrent.Common;

	internal class FileStreamBuffer : IDisposable
	{
		// A list of currently open filestreams. Note: The least recently used is at position 0
		// The most recently used is at the last position in the array
		private List<TorrentFileStream> _openStreams;
		private int _maxOpenStreams;

		public FileStreamBuffer(int maxStreams)
		{
			_maxOpenStreams = maxStreams;
			_openStreams = new List<TorrentFileStream>(maxStreams);
		}

		public int Count
		{
			get
			{
				return _openStreams.Count;
			}
		}

		internal TorrentFileStream GetStream(TorrentFile file, string filePath, FileAccess access)
		{
			TorrentFileStream stream = _openStreams.Find((candidate) => candidate.FilePath == filePath);
			if (stream != null)
			{
				// If we are requesting write access and the current stream does not have it
				if (((access & FileAccess.Write) == FileAccess.Write) && !stream.CanWrite)
				{
					Logger.Log(null, string.Format(
						"Reopening stream with write access\n\t{0}", filePath));
					CloseAndRemove(stream);
					stream = null;
				}
				else
				{
					// Place the filestream at the end so we know it's been recently used
					_openStreams.Remove(stream);
					_openStreams.Add(stream);
				}
			}

			if (stream == null)
			{
				// If file does not exist then attempt to create sparse file
				if (!File.Exists(filePath))
				{
					SparseFile.CreateSparse(filePath, file.Length);
				}

				// Create or open the torrent and add to list
				stream = new TorrentFileStream(filePath, FileMode.OpenOrCreate, access, FileShare.Read);
				Add(stream);
			}
			return stream;
		}

		internal bool CloseStream(string filePath)
		{
			bool result = false;
			TorrentFileStream stream = _openStreams.Find((candidate) => candidate.FilePath == filePath);
			if (stream != null)
			{
				CloseAndRemove(stream);
				result = true;
			}
			return result;
		}

		private void Add(TorrentFileStream stream)
		{
			Logger.Log(null, "Opening filestream: {0}", stream.FilePath);

			// If we have our maximum number of streams open, just dispose and dump the least recently used one
			if (_maxOpenStreams != 0 && _openStreams.Count >= _openStreams.Capacity)
			{
				Logger.Log(null, "We've reached capacity: {0}", _openStreams.Count);
				CloseAndRemove(_openStreams[0]);
			}
			_openStreams.Add(stream);
		}

		/// <summary>
		/// Closes the stream and removes it from the list.
		/// </summary>
		/// <param name="stream">The stream.</param>
		private void CloseAndRemove(TorrentFileStream stream)
		{
			Logger.Log(null, "Closing and removing: {0}", stream.FilePath);
			_openStreams.Remove(stream);
			stream.Dispose();
		}

		#region IDisposable Members
		public void Dispose()
		{
			DisposeManagedObjects();
		}

		protected virtual void DisposeManagedObjects()
		{
			_openStreams.ForEach((s) => s.Dispose());
			_openStreams.Clear();
		}
		#endregion
	}
}
