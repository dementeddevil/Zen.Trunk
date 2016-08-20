namespace Zen.Trunk.Torrent.Common
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Security.Cryptography;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Bencoding;
	using Zen.Trunk.Torrent.Client;
	using Zen.Trunk.Torrent.Client.PieceWriters;

	public class TorrentCreator
	{
		#region Private Fields
		/// <summary>
		/// The list of announce urls
		/// </summary>
		private List<List<string>> _announces;
		/// <summary>
		/// True if you want to ignore hidden files when making the torrent
		/// </summary>
		private bool _ignoreHiddenFiles;
		/// <summary>
		/// The path from which the torrent will be created (can be file or directory)
		/// </summary>
		private string _sourcePath = string.Empty;
		/// <summary>
		/// True if an MD5 hash of each file should be included
		/// </summary>
		private bool _storeMd5;
		/// <summary>
		/// The BencodedDictionary which contains the data to be written to the .torrent file
		/// </summary>
		private BEncodedDictionary _dict;
		private SHA1 _hasher = SHA1.Create();

		private CancellationToken _cancellationToken = CancellationToken.None;
		#endregion

		#region Public Events
		/// <summary>
		/// This event indicates the progress of the torrent creation and is fired every time a piece is hashed
		/// </summary>
		public event EventHandler<TorrentCreatorEventArgs> Hashed;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TorrentCreator"/> class.
		/// </summary>
		public TorrentCreator()
		{
			BEncodedDictionary info = new BEncodedDictionary();
			_announces = new List<List<string>>();
			_ignoreHiddenFiles = true;
			_dict = new BEncodedDictionary();
			_dict.Add("info", info);

			// Add in initial values for some of the torrent attributes
			PieceLength = 256 * 1024;   // 256kB default piece size
			this.Encoding = "UTF-8";
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the announces.
		/// </summary>
		/// <value>The announces.</value>
		public List<List<string>> Announces
		{
			get
			{
				return _announces;
			}
		}

		/// <summary>
		/// Gets or sets the comment.
		/// </summary>
		/// <value>The comment.</value>
		public string Comment
		{
			get
			{
				BEncodedValue val = Get(_dict, new BEncodedString("comment"));
				return val == null ? string.Empty : val.ToString();
			}
			set
			{
				Set(_dict, "comment", new BEncodedString(value));
			}
		}

		/// <summary>
		/// Gets or sets the created by.
		/// </summary>
		/// <value>The created by.</value>
		public string CreatedBy
		{
			get
			{
				BEncodedValue val = Get(_dict, new BEncodedString("created by"));
				return val == null ? string.Empty : val.ToString();
			}
			set
			{
				Set(_dict, "created by", new BEncodedString(value));
			}
		}

		/// <summary>
		/// Gets or sets the encoding.
		/// </summary>
		/// <value>The encoding.</value>
		public string Encoding
		{
			get
			{
				return Get(_dict, (BEncodedString)"encoding").ToString();
			}
			private set
			{
				Set(_dict, "encoding", (BEncodedString)value);
			}
		}

		/// <summary>
		/// Gets or sets the hasher.
		/// </summary>
		/// <value>The hasher.</value>
		internal SHA1 Hasher
		{
			get
			{
				return _hasher;
			}
			set
			{
				_hasher = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to ignore hidden files.
		/// </summary>
		/// <value>
		/// <c>true</c> to ignore hidden files; otherwise, <c>fale</c>.
		/// </value>
		public bool IgnoreHiddenFiles
		{
			get
			{
				return _ignoreHiddenFiles;
			}
			set
			{
				_ignoreHiddenFiles = value;
			}
		}

		/// <summary>
		/// The path from which the torrent can be created.
		/// </summary>
		/// <remarks>
		/// This can be either a file or a folder containing the files to hash.
		/// </remarks>
		public string SourcePath
		{
			get
			{
				return _sourcePath ?? string.Empty;
			}
			set
			{
				_sourcePath = value ?? string.Empty;
			}
		}

		/// <summary>
		/// The length of each piece in bytes (range 16384 bytes -> 4MB)
		/// </summary>
		public long PieceLength
		{
			get
			{
				BEncodedValue val = Get((BEncodedDictionary)_dict["info"], new BEncodedString("piece length"));
				return val == null ? -1 : ((BEncodedNumber)val).Number;
			}
			set
			{
				Set((BEncodedDictionary)_dict["info"], "piece length", new BEncodedNumber(value));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the torrent is private.
		/// </summary>
		/// <value><c>true</c> if private; otherwise, <c>false</c>.</value>
		/// <remarks>
		/// A private torrent can only accept peers from the tracker and will
		/// not share peer data through DHT.
		/// </remarks>
		public bool Private
		{
			get
			{
				BEncodedValue val = Get((BEncodedDictionary)_dict["info"], new BEncodedString("private"));
				return val == null ? false : ((BEncodedNumber)val).Number == 1;
			}
			set
			{
				Set((BEncodedDictionary)_dict["info"], "private", new BEncodedNumber(value ? 1 : 0));
			}
		}

		/// <summary>
		/// Gets or sets the publisher.
		/// </summary>
		/// <value>The publisher.</value>
		public string Publisher
		{
			get
			{
				BEncodedValue val = Get((BEncodedDictionary)_dict["info"], new BEncodedString("publisher"));
				return val == null ? string.Empty : val.ToString();
			}
			set
			{
				Set((BEncodedDictionary)_dict["info"], "publisher", new BEncodedString(value));
			}
		}

		/// <summary>
		/// Gets or sets the publisher URL.
		/// </summary>
		/// <value>The publisher URL.</value>
		public string PublisherUrl
		{
			get
			{
				BEncodedValue val = Get((BEncodedDictionary)_dict["info"], new BEncodedString("publisher-url"));
				return val == null ? string.Empty : val.ToString();
			}
			set
			{
				Set((BEncodedDictionary)_dict["info"], "publisher-url", new BEncodedString(value));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to store the MD5 hash.
		/// </summary>
		/// <value>
		/// <c>true</c> to store the MD5 hash; otherwise, <c>false</c>.
		/// </value>
		/// <remarks>
		/// The MD5 hash is only added to the torrent metadata for single-file
		/// torrents.
		/// </remarks>
		public bool StoreMD5
		{
			get
			{
				return _storeMd5;
			}
			set
			{
				_storeMd5 = value;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Adds a custom value to the main bencoded dictionary
		/// </summary>        
		public void AddCustom(BEncodedString key, BEncodedValue value)
		{
			_dict.Add(key, value);
		}

		/// <summary>
		/// Creates a Torrent and returns it in it's dictionary form
		/// </summary>
		/// <returns></returns>
		public BEncodedDictionary Create()
		{
			return Create(new DiskWriter());
		}

		///<summary>
		/// Creates a Torrent and writes it to disk in the specified location
		///<summary>
		///<param name="storagePath">The path (including filename) where the new Torrent will be written to</param>
		public void Create(string path)
		{
			Check.PathNotEmpty(path);

			using (FileStream stream = new FileStream(path, FileMode.Create))
			{
				Create(stream);
			}
		}

		/// <summary>
		/// Generates a Torrent and writes it to the supplied stream
		/// </summary>
		/// <param name="stream">The stream to write the torrent to</param>
		public void Create(Stream stream)
		{
			Check.Stream(stream);

			BEncodedDictionary torrentDict = Create();

			byte[] data = torrentDict.Encode();
			stream.Write(data, 0, data.Length);
		}

		public async Task<BEncodedDictionary> CreateAsync(CancellationToken token)
		{
			_cancellationToken = token;
			try
			{
				return await Task.Run(new Func<BEncodedDictionary>(Create));
			}
			finally
			{
				_cancellationToken = CancellationToken.None;
			}
		}

		public async Task CreateAsync(string path, CancellationToken token)
		{
			Check.PathNotEmpty(path);

			BEncodedDictionary dict = await CreateAsync(token);
			using (FileStream stream = new FileStream(path, FileMode.Create))
			{
				byte[] data = dict.Encode();
				stream.Write(data, 0, data.Length);
			}
		}

		public async Task CreateAsync(Stream stream, CancellationToken token)
		{
			Check.Stream(stream);

			BEncodedDictionary dict = await CreateAsync(token);
			byte[] data = dict.Encode();
			stream.Write(data, 0, data.Length);
		}

		///<summary>
		/// Calculates the approximate size of the final .torrent in bytes
		///</summary>
		public long GetSize()
		{
			CloneableList<string> paths = new CloneableList<string>();

			if (Directory.Exists(_sourcePath))
				DiscoverAllFiles(_sourcePath, paths);
			else if (File.Exists(_sourcePath))
				paths.Add(_sourcePath);
			else
				return 64 * 1024;

			long size = 0;
			for (int i = 0; i < paths.Count; i++)
				size += new FileInfo(paths[i]).Length;

			return size;
		}

		public int RecommendedPieceSize()
		{
			long totalSize = GetSize();

			// Check all piece sizes that are multiples of 32kB 
			for (int i = 32768; i < 4 * 1024 * 1024; i *= 2)
			{
				int pieces = (int)(totalSize / i) + 1;
				if ((pieces * 20) < (60 * 1024))
					return i;
			}

			// If we get here, we're hashing a massive file, so lets limit
			// to a max of 4MB pieces.
			return 4 * 1024 * 1024;
		}

		/// <summary>
		/// Removes a custom value from the main bencoded dictionary.
		/// </summary>
		public void RemoveCustom(BEncodedString key)
		{
			_dict.Remove(key);
		}
		#endregion

		#region Internal Methods
		internal BEncodedDictionary Create(PieceWriter writer)
		{
			if (!Directory.Exists(SourcePath) && !File.Exists(SourcePath))
			{
				throw new ArgumentException("no such file or directory", SourcePath);
			}

			List<TorrentFile> files = new List<TorrentFile>();
			LoadFiles(SourcePath, files);

			if (files.Count == 0)
			{
				throw new TorrentException("There were no files in the specified directory");
			}

			string[] parts = _sourcePath.Split(new char[] { System.IO.Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
			string name = files.Count == 1 ? System.IO.Path.GetFileName(_sourcePath) : parts[parts.Length - 1];
			return Create(files.ToArray(), writer, name);
		}

		internal BEncodedDictionary Create(TorrentFile[] files, PieceWriter writer, string name)
		{
			// Clone the base dictionary and fill the remaining data into the clone
			BEncodedDictionary torrentDict = BEncodedDictionary.Decode<BEncodedDictionary>(_dict.Encode());
			Array.Sort<TorrentFile>(files, delegate(TorrentFile a, TorrentFile b)
			{
				return String.CompareOrdinal(a.Path, b.Path);
			});

			if (files.Length > 1)
			{
				Logger.Log(null, "Creating multifile torrent from: {0}", SourcePath);
				CreateMultiFileTorrent(torrentDict, files, writer, name);
			}
			else
			{
				Logger.Log(null, "Creating singlefile torrent from: {0}", SourcePath);
				CreateSingleFileTorrent(torrentDict, files, writer, name);
			}

			return torrentDict;
		}
		#endregion

		///<summary>
		///used for creating multi file mode torrents.
		///</summary>
		///<returns>the dictionary representing which is stored in the torrent file</returns>
		protected void CreateMultiFileTorrent(
			BEncodedDictionary dictionary,
			TorrentFile[] files,
			PieceWriter writer,
			string name)
		{
			AddCommonStuff(dictionary);
			BEncodedDictionary info = (BEncodedDictionary)dictionary["info"];

			BEncodedList torrentFiles = new BEncodedList();//the dict which hold the file infos
			for (int i = 0; i < files.Length; i++)
			{
				torrentFiles.Add(GetFileInfoDict(files[i]));
			}

			info.Add("files", torrentFiles);

			Logger.Log(null, "Topmost directory: {0}", name);
			info.Add("name", new BEncodedString(name));

			info.Add("pieces", new BEncodedString(CalcPiecesHash(SourcePath, files, writer)));
		}

		///<summary>
		///used for creating a single file torrent file
		///<summary>
		///<returns>the dictionary representing which is stored in the torrent file</returns>
		protected void CreateSingleFileTorrent(
			BEncodedDictionary dictionary,
			TorrentFile[] files,
			PieceWriter writer,
			string name)
		{
			AddCommonStuff(dictionary);

			BEncodedDictionary infoDict = (BEncodedDictionary)dictionary["info"];
			infoDict.Add("length", new BEncodedNumber(files[0].Length));
			infoDict.Add("name", (BEncodedString)name);

			if (StoreMD5)
			{
				AddMD5(infoDict, SourcePath);
			}

			Logger.Log(null, "name == {0}", name);
			string path = System.IO.Path.GetDirectoryName(SourcePath);
			infoDict.Add("pieces", new BEncodedString(CalcPiecesHash(path, files, writer)));
		}

		private void LoadFiles(string path, List<TorrentFile> files)
		{
			// Check for cancellation
			_cancellationToken.ThrowIfCancellationRequested();

			if (Directory.Exists(path))
			{
				foreach (string subdir in System.IO.Directory.GetFileSystemEntries(path))
				{
					LoadFiles(subdir, files);
				}
			}
			else if (File.Exists(path))
			{
				// Get the filename or relative path
				string filePath;
				if (path == SourcePath)
				{
					filePath = Path.GetFileName(path);
				}
				else
				{
					filePath = path.Substring(SourcePath.Length);
					if (filePath[0] == System.IO.Path.DirectorySeparatorChar)
					{
						filePath = filePath.Substring(1);
					}
				}

				FileInfo info = new FileInfo(path);
				if (!((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden && IgnoreHiddenFiles))
				{
					files.Add(new TorrentFile(filePath, info.Length, 0, 0, null, null, null));
				}
			}
		}

		///<summary>
		///this adds stuff common to single and multi file torrents
		///</summary>
		private void AddCommonStuff(BEncodedDictionary torrent)
		{
			if (_announces.Count > 0 && _announces[0].Count > 0)
			{
				torrent.Add("announce", new BEncodedString(_announces[0][0]));
			}

			// If there is more than one tier or the first tier has more than 1 tracker
			if (_announces.Count > 1 || (_announces.Count > 0 && _announces[0].Count > 1))
			{
				BEncodedList announceList = new BEncodedList();
				for (int i = 0; i < _announces.Count; i++)
				{
					BEncodedList tier = new BEncodedList();
					for (int j = 0; j < _announces[i].Count; j++)
					{
						tier.Add(new BEncodedString(_announces[i][j]));
					}
					announceList.Add(tier);
				}
				torrent.Add("announce-list", announceList);
			}

			DateTime epocheStart = new DateTime(1970, 1, 1);
			TimeSpan span = DateTime.UtcNow - epocheStart;
			Logger.Log(null, "creation date: {0} - {1} = {2}:{3}", DateTime.UtcNow, epocheStart, span, span.TotalSeconds);
			torrent.Add("creation date", new BEncodedNumber((long)span.TotalSeconds));
		}

		///<summary>calculate md5sum of a file</summary>
		///<param name="fileName">the file to sum with md5</param>
		private void AddMD5(BEncodedDictionary dict, string fileName)
		{
			MD5 hasher = MD5.Create();
			StringBuilder sb = new StringBuilder();

			using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				byte[] hash = hasher.ComputeHash(stream);

				foreach (byte b in hash)
				{
					string hex = b.ToString("X");
					hex = hex.Length > 1 ? hex : "0" + hex;
					sb.Append(hex);
				}
				Logger.Log(null, "Sum for: '{0}' = {1}", fileName, sb.ToString());
			}
			dict.Add("md5sum", new BEncodedString(sb.ToString()));
		}

		///<summary>
		///calculates all hashes over the files which should be included in the torrent
		///</summmary>
		private byte[] CalcPiecesHash(string path, TorrentFile[] files, PieceWriter writer)
		{
			byte[] piecesBuffer = new byte[GetPieceCount(files) * 20]; //holds all the pieces hashes
			int piecesBufferOffset = 0;

			long totalLength = files.Sum((item) => item.Length);
			ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[PieceLength]);

			while (totalLength > 0)
			{
				// Check for cancellation...
				_cancellationToken.ThrowIfCancellationRequested();

				// Read next piece
				int bytesToRead = (int)Math.Min(totalLength, PieceLength);
				BufferedIO io = new BufferedIO(
					buffer,
					(piecesBufferOffset / 20) * PieceLength,
					bytesToRead,
					bytesToRead,
					files,
					path);
				totalLength -= writer.ReadChunk(io);

				// Check for cancellation...
				_cancellationToken.ThrowIfCancellationRequested();

				// Compute piece hash
				byte[] currentHash =
					_hasher.ComputeHash(buffer.Array, 0, io.ActualCount);
				RaiseHashed(new TorrentCreatorEventArgs(
					0, 0,//reader.CurrentFile.Position, reader.CurrentFile.Length,
					piecesBufferOffset * PieceLength, (piecesBuffer.Length - 20) * PieceLength));

				// Copy piece hash into our piece memory and advance cursor
				Buffer.BlockCopy(currentHash, 0, piecesBuffer, piecesBufferOffset, currentHash.Length);
				piecesBufferOffset += currentHash.Length;
			}
			return piecesBuffer;
		}

		private void DiscoverAllFiles(string directory, CloneableList<string> fileList)
		{
			Queue<string> pendingDirectories = new Queue<string>();
			pendingDirectories.Enqueue(directory);
			while (pendingDirectories.Count > 0)
			{
				// Get next directory to process
				directory = pendingDirectories.Dequeue();

				// Add sub-directories to the pending queue
				string[] subs = Directory.GetDirectories(directory);
				foreach (string path in subs)
				{
					// Check for cancellation
					_cancellationToken.ThrowIfCancellationRequested();

					// Skip hidden folders as required.
					if (_ignoreHiddenFiles)
					{
						DirectoryInfo info = new DirectoryInfo(path);
						if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
						{
							continue;
						}
					}
					pendingDirectories.Enqueue(path);
				}

				// Process files in this folder
				subs = Directory.GetFiles(directory);
				foreach (string path in subs)
				{
					// Check for cancellation
					_cancellationToken.ThrowIfCancellationRequested();

					if (_ignoreHiddenFiles)
					{
						FileInfo info = new FileInfo(path);
						if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
						{
							continue;
						}
					}

					fileList.Add(path);
				}
			}
		}

		///<summary>
		///this method is used for multi file mode torrents to return a dictionary with
		///file relevant informations. 
		///<param name="file">the file to report the informations for</param>
		///<param name="basePath">used to subtract the absolut path information</param>
		///</summary>
		private BEncodedDictionary GetFileInfoDict(TorrentFile file)
		{
			BEncodedDictionary fileDict = new BEncodedDictionary();
			fileDict.Add("length", new BEncodedNumber(file.Length));

#warning Implement this again
			//if (StoreMD5)
			//AddMD5(fileDict, file);

			Logger.Log(null, "Without base: {0}", file.Path);
			BEncodedList filePath = new BEncodedList();
			string[] splitPath = file.Path.Split(System.IO.Path.DirectorySeparatorChar);
			foreach (string s in splitPath)
			{
				if (s.Length > 0)//exclude empties
				{
					filePath.Add(new BEncodedString(s));
				}
			}
			fileDict.Add("path", filePath);
			return fileDict;
		}

		private long GetPieceCount(TorrentFile[] files)
		{
			long size = 0;
			foreach (TorrentFile file in files)
				size += file.Length;

			//double count = (double)size/PieceLength;
			long pieceCount = size / PieceLength + (((size % PieceLength) != 0) ? 1 : 0);
			Logger.Log(null, "Piece Count: {0}", pieceCount);
			return pieceCount;
		}

		private void RaiseHashed(TorrentCreatorEventArgs e)
		{
			Toolbox.RaiseAsyncEvent<TorrentCreatorEventArgs>(Hashed, this, e);
		}

		private static BEncodedValue Get(
			BEncodedDictionary dictionary,
			BEncodedString key)
		{
			return dictionary.ContainsKey(key) ? dictionary[key] : null;
		}

		private static void Set(
			BEncodedDictionary dictionary,
			BEncodedString key,
			BEncodedValue value)
		{
			if (dictionary.ContainsKey(key))
			{
				dictionary[key] = value;
			}
			else
			{
				dictionary.Add(key, value);
			}
		}
	}
}
