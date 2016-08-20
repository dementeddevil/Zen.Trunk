using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.IO;
using Zen.Trunk.Torrent.Common;
using Zen.Trunk.Torrent.Client;
using Microsoft.Practices.ServiceLocation;

namespace Zen.Trunk.Torrent.Comms.V1
{
	public interface ITorrentServerControl
	{
		[OperationContract(IsInitiating = true)]
		Guid Register();

		[OperationContract(Name = "DownloadTorrent")]
		void DownloadTorrent(string torrentUri, string localName);

		[OperationContract(IsTerminating = true)]
		void Unregister(Guid registrationCookie);
	}

	public class TorrentServerControl : ITorrentServerControl
	{
		public Guid Register()
		{
			return Guid.Empty;
		}

		public void DownloadTorrent(string torrentUrl, string localName)
		{
			// TODO: Determine full server path for the torrent file
			//	by configuration or some such method...
			if (Path.IsPathRooted(localName) ||
				string.IsNullOrEmpty(localName))
			{
				localName = string.Format("{0:B}.torrent", Guid.NewGuid());
			}
			string localTorrentPath =
				Path.Combine(Environment.CurrentDirectory, "Torrents");
			localTorrentPath =
				Path.Combine(localTorrentPath, localName);

			// TODO: Determine the local torrent data path
			//	presumeably based on category or some such
			string localTorrentDataPath = localTorrentPath;

			// TODO: Determine torrent settings
			//	these should be passed to the method from the client
			//	and limited based on user profile/authorisation
			TorrentSettings torrentSettings = new TorrentSettings();
			torrentSettings.SavePath = localTorrentDataPath;
			torrentSettings.StartImmediately = true;

			Uri torrentUri = new Uri(torrentUrl);
			if (torrentUri.Scheme == Uri.UriSchemeHttp ||
				torrentUri.Scheme == Uri.UriSchemeHttps)
			{
				TorrentObject torrentObject = TorrentObject.Load(
					torrentUri, localTorrentPath);
				TorrentManager torrentManager = new TorrentManager (
					torrentObject, torrentSettings);

				ClientEngine engine = ServiceLocator.Current
					.GetInstance<ClientEngine>();
				engine.Register(torrentManager);
				return;
			}

			throw new FaultException("Only http/https torrent uris are supported.");
		}

		public void Unregister(Guid registrationCookie)
		{
		}
	}
}
