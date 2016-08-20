namespace Zen.Trunk.EvalClient.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Windows.Threading;
	using Zen.Trunk.Torrent.Client;
	using Zen.Trunk.EvalClient.Models;
	using Microsoft.Practices.ServiceLocation;

	public class MainWindowViewModel : ViewModel
	{
		private ObservableCollection<TorrentViewModel> _torrents;
		private DispatcherTimer _refreshTimer;
		private ClientEngine _engine = ServiceLocator.Current.GetInstance<ClientEngine>();

		public MainWindowViewModel()
		{
			_refreshTimer = new DispatcherTimer();
			_refreshTimer.Interval = TimeSpan.FromSeconds(0.5);
			_refreshTimer.Tick +=
				(sender, args) =>
				{
					try
					{
						foreach (TorrentViewModel torrent in Torrents)
						{
							torrent.Refresh();
						}
						RaisePropertyChanged(
							"DhtStatus");
					}
					catch
					{
					}
				};
			_refreshTimer.IsEnabled = true;

			_engine = ServiceLocator.Current.GetInstance<ClientEngine>();
		}

		public IEnumerable<TorrentViewModel> Torrents
		{
			get
			{
				if (_torrents == null)
				{
					_torrents = new ObservableCollection<TorrentViewModel>();

					TorrentEngineModel model = ServiceLocator.Current
						.GetInstance<TorrentEngineModel>();
					foreach (TorrentManagerAndLongTermStats torrentManager in model.Torrents)
					{
						TorrentViewModel wrappedTorrent = new TorrentViewModel(torrentManager);
						_torrents.Add(wrappedTorrent);
					}
				}
				return _torrents;
			}
		}

		public string DhtStatus
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("Dht: {0}", _engine.DhtEngine.State.ToString());
				if (_engine.DhtEngine.State == Torrent.Dht.State.Ready)
				{
					sb.AppendFormat(", {0} peers", _engine.DhtEngine.NodeCount);
				}
				return sb.ToString();
			}
		}

		public TorrentViewModel OpenTorrentFile(
			string pathName, TorrentSettings settings)
		{
			TorrentEngineModel model = ServiceLocator.Current
				.GetInstance<TorrentEngineModel>();

			TorrentManagerAndLongTermStats torrentAndStats = 
				model.OpenTorrentFile(pathName, settings);
			if (torrentAndStats == null)
			{
				return null;
			}

			TorrentViewModel wrappedTorrent = new TorrentViewModel(torrentAndStats);
			_torrents.Add(wrappedTorrent);
			return wrappedTorrent;
		}
	}
}
