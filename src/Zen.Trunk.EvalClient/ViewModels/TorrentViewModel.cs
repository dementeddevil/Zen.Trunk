namespace Zen.Trunk.EvalClient.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.ComponentModel;
	using Zen.Trunk.Torrent.Client;
	using Zen.Trunk.EvalClient.Models;

	public class TorrentViewModel : ViewModel
	{
		private TorrentManagerAndLongTermStats _torrentAndStats;

		public TorrentViewModel(TorrentManagerAndLongTermStats torrent)
		{
			_torrentAndStats = torrent;
		}

		public string Name
		{
			get
			{
				return _torrentAndStats.TorrentManager.Torrent.Name;
			}
		}

		public long SizeInBytes
		{
			get
			{
				return _torrentAndStats.TorrentManager.Torrent.Size;
			}
		}

		public string SizeText
		{
			get
			{
				double size = _torrentAndStats.TorrentManager.Torrent.Size;
				if (size < 1024)
				{
					return string.Format("{0:F2} bytes", size);
				}
				if (size < 700 * 1024)
				{
					return string.Format("{0:F2} Kb", size / 1024.0);
				}
				if (size < 700 * 1024 * 1024)
				{
					return string.Format("{0:F2} Mb", size / (1024.0 * 1024.0));
				}
				return string.Format("{0:F2} Gb", size / (1024.0 * 1024.0 * 1024.0));
			}
		}

		public double PercentComplete
		{
			get
			{
				return _torrentAndStats.TorrentManager.Progress;
			}
		}

		public string StatusText
		{
			get
			{
				return _torrentAndStats.TorrentManager.State.ToString();
			}
		}

		public string Source
		{
			get
			{
				return _torrentAndStats.TorrentManager.Torrent.Source;
			}
		}

		public string Seeders
		{
			get
			{
				return string.Format(
					"{0} ({1})",
					_torrentAndStats.TorrentManager.Peers.ActiveSeeders,
					_torrentAndStats.TorrentManager.Peers.AvailableSeeders);
			}
		}

		public string Leechers
		{
			get
			{
				return string.Format(
					"{0} ({1})",
					_torrentAndStats.TorrentManager.Peers.ActiveLeechers,
					_torrentAndStats.TorrentManager.Peers.AvailableLeechers);
			}
		}

		public int DownloadSpeed
		{
			get
			{
				return _torrentAndStats.TorrentManager.Monitor.DownloadSpeed;
			}
		}

		public int UploadSpeed
		{
			get
			{
				return _torrentAndStats.TorrentManager.Monitor.UploadSpeed;
			}
		}

		public DateTime? ETA
		{
			get
			{
				if (!_torrentAndStats.TorrentManager.IsComplete &&
					_torrentAndStats.TorrentManager.State == Torrent.Common.TorrentState.Downloading &&
					_torrentAndStats.TotalDataDownloadedBytes > 0)
				{
					long totalDownloaded = _torrentAndStats.TotalDataDownloadedBytes;
					double avgSpeed = (double)totalDownloaded / (double)_torrentAndStats.TotalElapsedTime.Seconds;
					double remainingTime = (double)(_torrentAndStats.TorrentManager.Torrent.Size - totalDownloaded) / avgSpeed;
					return (DateTime.UtcNow + TimeSpan.FromSeconds(remainingTime)).ToLocalTime();
				}
				return null;
			}
		}

		public string Label
		{
			get
			{
				return _torrentAndStats.TorrentManager.Torrent.Comment;
			}
		}

		public string UploadedText
		{
			get
			{
				double size = _torrentAndStats.TotalDataUploadedBytes;
				if (size < 1024)
				{
					return string.Format("{0:F2} bytes", size);
				}
				if (size < 700 * 1024)
				{
					return string.Format("{0:F2} Kb", size / 1024.0);
				}
				if (size < 700 * 1024 * 1024)
				{
					return string.Format("{0:F2} Mb", size / (1024.0 * 1024.0));
				}
				return string.Format("{0:F2} Gb", size / (1024.0 * 1024.0 * 1024.0));
			}
		}

		public double? Ratio
		{
			get
			{
				if (_torrentAndStats.TorrentManager.IsComplete)
				{
					// Use the full size of the torrent...
					return (double)_torrentAndStats.TotalDataUploadedBytes / (double)SizeInBytes;
				}
				else
				{
					if (_torrentAndStats.TotalDataDownloadedBytes == 0)
					{
						return null;
					}
					return (double)_torrentAndStats.TotalDataUploadedBytes / (double)_torrentAndStats.TotalDataDownloadedBytes;
				}
			}
		}

		public DateTime AddedOn
		{
			get
			{
				return _torrentAndStats.LongTermStats.AddedOn;
			}
		}

		public DateTime? CompletedOn
		{
			get
			{
				return _torrentAndStats.LongTermStats.CompletedOn;
			}
		}

		public TimeSpan TotalElapsedTime
		{
			get
			{
				return _torrentAndStats.TotalElapsedTime;
			}
		}

		public string TotalElapsedTimeText
		{
			get
			{
				TimeSpan totalElapsedTime = TotalElapsedTime;
				if (totalElapsedTime.TotalMinutes < 1)
				{
					return string.Format("{0:%s}s", totalElapsedTime);
				}
				if (TotalElapsedTime.TotalHours < 1)
				{
					return string.Format("{1:%m}m {0:%s}s", totalElapsedTime, totalElapsedTime);
				}
				if (totalElapsedTime.TotalDays < 1)
				{
					return string.Format("{2:%h}h {1:%m}m {0:%s}s", totalElapsedTime, totalElapsedTime, totalElapsedTime);
				}
				return string.Format("{3:%d}d {2:%h}h {1:%m}m {0:%s}s", totalElapsedTime, totalElapsedTime, totalElapsedTime, totalElapsedTime);
			}
		}

		public void Refresh()
		{
			RaisePropertyChanged(
				"PercentComplete",
				"StatusText",
				"Seeders",
				"Leechers",
				"DownloadSpeed",
				"UploadSpeed",
				"ETA",
				"UploadedText",
				"Ratio",
				"CompletedOn",
				"TotalElapsedTime",
				"TotalElapsedTimeText");
		}
	}
}
