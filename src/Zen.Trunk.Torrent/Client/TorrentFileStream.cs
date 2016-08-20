namespace Zen.Trunk.Torrent.Client
{
	using System.IO;

	internal class TorrentFileStream : FileStream
	{
		public TorrentFileStream(string filePath, FileMode mode, FileAccess access, FileShare share)
			: base(filePath, mode, access, share)
		{
			FilePath = filePath;
		}

		public string FilePath
		{
			get;
			private set;
		}
	}
}
