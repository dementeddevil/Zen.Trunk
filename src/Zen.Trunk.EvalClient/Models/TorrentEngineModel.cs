namespace Zen.Trunk.EvalClient.Models
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.ComponentModel;
	using Zen.Trunk.Torrent.Client;
	using Microsoft.Practices.Unity;
	using Zen.Trunk.Torrent.Common;
	using System.Runtime.Serialization;
	using System.IO;
	using System.Threading.Tasks;
	using System.Collections.ObjectModel;

	public class TorrentManagerAndLongTermStats
	{
		public TorrentManagerAndLongTermStats(TorrentManager torrentManager)
		{
			TorrentManager = torrentManager;
			LongTermStats = new TorrentLongTermStats
				{
					AddedOn = DateTime.UtcNow,
					CompletedOn = null,
					CumulativeElapsedTime = new TimeSpan()
				};
		}

		public TorrentManagerAndLongTermStats(
			TorrentManager torrentManager,
			TorrentLongTermStats longTermStats)
		{
			TorrentManager = torrentManager;
			LongTermStats = longTermStats;
		}

		public TorrentManager TorrentManager
		{
			get;
			private set;
		}

		public TorrentLongTermStats LongTermStats
		{
			get;
			private set;
		}

		public TimeSpan TotalElapsedTime
		{
			get
			{
				TimeSpan time = LongTermStats.CumulativeElapsedTime;
				if (TorrentManager.StartTime.HasValue && !TorrentManager.StopTime.HasValue)
				{
					time += (DateTime.UtcNow - TorrentManager.StartTime.Value);
				}
				return time;
			}
		}

		public long TotalDataUploadedBytes
		{
			get
			{
				return LongTermStats.CumulativeDataUploadedBytes +
					TorrentManager.Monitor.DataBytesUploaded;
			}
		}

		public long TotalProtocolUploadedBytes
		{
			get
			{
				return LongTermStats.CumulativeProtocolUploadedBytes +
					TorrentManager.Monitor.ProtocolBytesUploaded;
			}
		}

		public long TotalDataDownloadedBytes
		{
			get
			{
				return LongTermStats.CumulativeDataDownloadedBytes +
					TorrentManager.Monitor.DataBytesDownloaded;
			}
		}

		public long TotalProtocolDownloadedBytes
		{
			get
			{
				return LongTermStats.CumulativeProtocolDownloadedBytes +
					TorrentManager.Monitor.ProtocolBytesDownloaded;
			}
		}
	}

	public class TorrentLongTermStats
	{
		public DateTime AddedOn
		{
			get;
			set;
		}

		public TimeSpan CumulativeElapsedTime
		{
			get;
			set;
		}

		public long CumulativeDataUploadedBytes
		{
			get;
			set;
		}

		public long CumulativeProtocolUploadedBytes
		{
			get;
			set;
		}

		public long CumulativeDataDownloadedBytes
		{
			get;
			set;
		}

		public long CumulativeProtocolDownloadedBytes
		{
			get;
			set;
		}

		public DateTime? CompletedOn
		{
			get;
			set;
		}
	}

	public class TorrentEngineState
	{
		public List<TorrentData> Torrents
		{
			get;
			set;
		}
	}

	public class TorrentData
	{
		public TorrentObject Torrent
		{
			get;
			set;
		}

		public TorrentSettings TorrentSettings
		{
			get;
			set;
		}

		public TorrentState TorrentState
		{
			get;
			set;
		}

		public FastResume ResumeData
		{
			get;
			set;
		}

		public TorrentLongTermStats LongTermStats
		{
			get;
			set;
		}
	}

	public class TorrentEngineModel : Model
	{
		private ClientEngine _engine;
		private ObservableCollection<TorrentManagerAndLongTermStats> _torrents;

		public TorrentEngineModel(
			[Dependency]ClientEngine engine)
		{
			_engine = engine;
		}

		public IList<TorrentManagerAndLongTermStats> Torrents
		{
			get
			{
				if (_torrents == null)
				{
					_torrents = new ObservableCollection<TorrentManagerAndLongTermStats>();
				}
				return _torrents;
			}
		}

		public async void LoadState()
		{
			string statePathName = GetEngineStatePathName(false);
			if (File.Exists(statePathName))
			{
				try
				{
					TorrentEngineState state;
					using (FileStream stream = new FileStream(statePathName, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						// Use DataContract serializer to serialize the state data
						DataContractSerializer serializer = CreateEngineStateSerializer();
						state = (TorrentEngineState)serializer.ReadObject(stream);
					}

					if (state != null && state.Torrents != null)
					{
						foreach (TorrentData torrentData in state.Torrents)
						{
							TorrentManager torrentManager;
							if (torrentData.ResumeData != null)
							{
								torrentManager = new TorrentManager(
									torrentData.Torrent,
									torrentData.TorrentSettings,
									torrentData.ResumeData);
							}
							else
							{
								torrentManager = new TorrentManager(
									torrentData.Torrent,
									torrentData.TorrentSettings);
							}
							torrentManager.TorrentStateChanged += TorrentStateChanged;

							Torrents.Add(
								new TorrentManagerAndLongTermStats(
									torrentManager,
									torrentData.LongTermStats));
							await _engine.Register(torrentManager);
						}
					}
				}
				catch (Exception)
				{
					// TODO: Log error...
				}
			}
		}

		public async Task SaveState()
		{
			TorrentEngineState state = new TorrentEngineState();

			List<Task<TorrentData>> dataTasks = new List<Task<TorrentData>>();
			foreach (TorrentManagerAndLongTermStats torrentManager in _torrents)
			{
				dataTasks.Add(GetTorrentState(torrentManager));
			}
			if (dataTasks.Count > 0)
			{
				state.Torrents = new List<TorrentData>(
					await Task.WhenAll<TorrentData>(
						dataTasks.ToArray()));
			}

			string statePathName = GetEngineStatePathName(true);
			using (FileStream stream = new FileStream(statePathName, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				// Use DataContract serializer to serialize the state data
				DataContractSerializer serializer = CreateEngineStateSerializer();
				serializer.WriteObject(stream, state);
			}
		}

		public TorrentManagerAndLongTermStats OpenTorrentFile(
			string pathName, TorrentSettings settings)
		{
			TorrentObject torrent;
			if (TorrentObject.TryLoad(pathName, out torrent))
			{
				// Default the save path to that defined for engine
				if (string.IsNullOrEmpty(settings.SavePath))
				{
					settings.SavePath = _engine.Settings.SavePath;
				}

				TorrentManager torrentManager = 
					new TorrentManager(torrent, settings);
				torrentManager.TorrentStateChanged += TorrentStateChanged;

				TorrentManagerAndLongTermStats torrentAndStats =
					new TorrentManagerAndLongTermStats(torrentManager);
				Torrents.Add(torrentAndStats);

				var taskNoWait = _engine.Register(torrentManager);

				return torrentAndStats;
			}
			return null;
		}

		public string GetTorrentEngineAppDataPathName()
		{
			return GetTorrentEngineAppDataPathName(false);
		}

		public string GetTorrentEngineAppDataPathName(bool ensureDirectoryExists)
		{
			string appSettings = Environment.GetFolderPath(
				Environment.SpecialFolder.LocalApplicationData);
			string torrentAppFolder = Path.Combine(appSettings, "Zen Design Corp", "Zen Torrent");
			if (ensureDirectoryExists && !Directory.Exists(torrentAppFolder))
			{
				Directory.CreateDirectory(torrentAppFolder);
			}
			return torrentAppFolder;
		}

		private string GetEngineStatePathName()
		{
			return GetEngineStatePathName(false);
		}

		private string GetEngineStatePathName(bool ensureDirectoryExists)
		{
			string torrentAppFolder = GetTorrentEngineAppDataPathName(
				ensureDirectoryExists);
			return Path.Combine(torrentAppFolder, "TorrentEngine.state");
		}

		private void TorrentStateChanged(object sender, TorrentStateChangedEventArgs e)
		{
			if (e.NewState == TorrentState.Stopped)
			{
				// Update long term cumulative elapsed time
				TorrentManagerAndLongTermStats tms =
					Torrents.First((item) => item.TorrentManager == e.TorrentManager);
				TimeSpan runTime = e.TorrentManager.StopTime.Value - e.TorrentManager.StartTime.Value;
				tms.LongTermStats.CumulativeElapsedTime += runTime;
			}
		}

		private DataContractSerializer CreateEngineStateSerializer()
		{
			return new DataContractSerializer(
				typeof(TorrentEngineState),
				"ZenTorrentState",
				"http://schemas.zendesigncorp.com/Torrent/2011/02/EngineState.xsd",
				new Type[]
				{
					typeof(TorrentData),
					typeof(TorrentObject),
					typeof(TorrentSettings),
					typeof(TorrentState),
					typeof(TorrentLongTermStats),
					typeof(FastResume)
				});
		}

		private async Task<TorrentData> GetTorrentState(TorrentManagerAndLongTermStats manager)
		{
			bool startImmediately = false;
			TorrentState state = manager.TorrentManager.State;
			if (state != TorrentState.Stopping &&
				state != TorrentState.Stopped &&
				state != TorrentState.Paused)
			{
				startImmediately = true;
				await manager.TorrentManager.Stop();
			}

			TorrentSettings settings = manager.TorrentManager.Settings.Clone();
			settings.StartImmediately = startImmediately;

			return new TorrentData
				{
					Torrent = manager.TorrentManager.Torrent,
					TorrentSettings = settings,
					TorrentState = state,
					ResumeData = manager.TorrentManager.SaveFastResume(),
					LongTermStats = manager.LongTermStats
				};
		}
	}
}
