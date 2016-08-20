//
// TrackerManager.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Text;
using System.Net;
using System.IO;
using Zen.Trunk.Torrent.Common;
using System.Collections.ObjectModel;
using System.Threading;
using System.Web;
using System.Diagnostics;
using System.Collections.Generic;
using Zen.Trunk.Torrent.Bencoding;
using Zen.Trunk.Torrent.Client.Encryption;
using System.Threading.Tasks;

namespace Zen.Trunk.Torrent.Client.Tracker
{
	/// <summary>
	/// Represents the connection to a tracker that an TorrentManager has
	/// </summary>
	public class TrackerManager
	{
		#region Private Fields
		private TorrentManager _manager;
		private byte[] _infoHash;
		private bool _updateSucceeded;
		private DateTime _lastUpdated;
		private TrackerTier[] _trackerTiers;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Creates a new TrackerConnection for the supplied torrent file
		/// </summary>
		/// <param name="manager">The TorrentManager to create the tracker connection for</param>
		public TrackerManager(TorrentManager manager)
		{
			_manager = manager;
			_infoHash = new byte[20];
			Buffer.BlockCopy(manager.Torrent.infoHash, 0, _infoHash, 0, 20);

			// Build list of all non-empty tracker tiers
			List<TrackerTier> tiers = new List<TrackerTier>();
			for (int i = 0; i < manager.Torrent.AnnounceUrls.Count; i++)
			{
				tiers.Add(new TrackerTier(manager.Torrent.AnnounceUrls[i]));
			}
			tiers.RemoveAll(
				(t) =>
				{
					return t.Trackers.Count == 0;
				});
			_trackerTiers = tiers.ToArray();

			// Hook tracker event handlers
			foreach (TrackerTier tier in _trackerTiers)
			{
				foreach (Tracker tracker in tier)
				{
					tracker.AnnounceComplete +=
						(sender, args) =>
						{
							ClientEngine.MainLoop.QueueAsync(
								() =>
								{
									OnAnnounceComplete(sender, args);
								});
						};

					tracker.ScrapeComplete +=
						(sender, args) =>
						{
							ClientEngine.MainLoop.QueueAsync(
								() =>
								{
									OnScrapeComplete(sender, args);
								});
						};
				}
			}
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// True if the last update succeeded
		/// </summary>
		public bool UpdateSucceeded
		{
			get
			{
				return _updateSucceeded;
			}
		}

		/// <summary>
		/// The time the last tracker update was sent to any tracker
		/// </summary>
		public DateTime LastUpdated
		{
			get
			{
				return _lastUpdated;
			}
		}

		/// <summary>
		/// The trackers available
		/// </summary>
		public TrackerTier[] TrackerTiers
		{
			get
			{
				return _trackerTiers;
			}
		}

		/// <summary>
		/// Returns the tracker that is current in use by the engine
		/// </summary>
		public Tracker CurrentTracker
		{
			get
			{
				if (_trackerTiers.Length == 0 ||
					_trackerTiers[0].Trackers.Count == 0)
				{
					return null;
				}

				return _trackerTiers[0].Trackers[0];
			}
		}
		#endregion

		#region Methods
		public async Task AnnounceIfNecessary()
		{
			Tracker tracker = CurrentTracker;
			if (tracker != null)
			{
				DateTime nowTime = DateTime.UtcNow;

				// If the last connection succeeded, then update at the regular
				//	interval
				if (UpdateSucceeded)
				{
					if (nowTime > (LastUpdated.AddSeconds(tracker.UpdateInterval)))
					{
						await Announce();
					}
				}

				// Otherwise update at the min interval
				else if (nowTime > (LastUpdated.AddSeconds(tracker.MinUpdateInterval)))
				{
					await Announce();
				}
			}
		}

		public Task Announce()
		{
			return Announce(CurrentTracker, TorrentEvent.None, true);
		}

		public Task Announce(Tracker tracker)
		{
			return Announce(tracker, TorrentEvent.None, false);
		}

		internal Task Announce(TorrentEvent clientEvent)
		{
			return Announce(CurrentTracker, clientEvent, true);
		}

		internal Task Announce(TorrentEvent clientEvent, bool trySubsequent)
		{
			return Announce(CurrentTracker, clientEvent, trySubsequent);
		}

		private Task Announce(Tracker tracker, TorrentEvent clientEvent, bool trySubsequent)
		{
			// Sanity check
			// If the tracker is null
			//	-or-
			// We are shutting down and this is not a stop event
			//	-or-
			// We are shutting down and announcing a stop event to a tracker
			//	we have never sent a successful start message to
			// then return - we have nothing to do this time!
			if (tracker == null || 
				(_manager.ShutdownToken.IsCancellationRequested && 
					(clientEvent != TorrentEvent.Stopped || !tracker.Tier.SentStartedEvent)))
			{
				return CompletedTask.Default;
			}

			// If the engine is null, we have been unregistered
			ClientEngine engine = _manager.Engine;
			if (engine == null)
			{
				return CompletedTask.Default;
			}

			TrackerConnectionId id = new TrackerConnectionId(tracker, trySubsequent, clientEvent, null);
			_updateSucceeded = true;
			_lastUpdated = DateTime.UtcNow;

			EncryptionTypes e = engine.Settings.AllowedEncryption;
			bool requireEncryption = !Toolbox.HasEncryption(e, EncryptionTypes.PlainText);
			bool supportsEncryption = Toolbox.HasEncryption(e, EncryptionTypes.RC4Full) || Toolbox.HasEncryption(e, EncryptionTypes.RC4Header);

			requireEncryption = requireEncryption && ClientEngine.SupportsEncryption;
			supportsEncryption = supportsEncryption && ClientEngine.SupportsEncryption;

			IPEndPoint reportedAddress = engine.Settings.ReportedAddress;
			string ip = reportedAddress == null ? null : reportedAddress.Address.ToString();
			int port = reportedAddress == null ? engine.Listener.LocalEndPoint.Port : reportedAddress.Port;

			AnnounceParameters p = new AnnounceParameters(
				_manager,
				clientEvent, 
				_infoHash, 
				id, 
				requireEncryption,
				supportsEncryption,
				ip,
				port);
			return tracker.Announce(p);
		}

		private void GetNextTracker(Tracker tracker, out TrackerTier trackerTier, out Tracker trackerReturn)
		{
			for (int i = 0; i < _trackerTiers.Length; i++)
			{
				for (int j = 0; j < _trackerTiers[i].Trackers.Count; j++)
				{
					if (_trackerTiers[i].Trackers[j] != tracker)
					{
						continue;
					}

					// If we are on the last tracker of this tier, check to see if there are more tiers
					if (j == (_trackerTiers[i].Trackers.Count - 1))
					{
						if (i == (_trackerTiers.Length - 1))
						{
							trackerTier = null;
							trackerReturn = null;
							return;
						}

						trackerTier = _trackerTiers[i + 1];
						trackerReturn = trackerTier.Trackers[0];
						return;
					}

					trackerTier = _trackerTiers[i];
					trackerReturn = trackerTier.Trackers[j + 1];
					return;
				}
			}

			trackerTier = null;
			trackerReturn = null;
		}

		private void OnScrapeComplete(object sender, ScrapeResponseEventArgs e)
		{
			// No need to do anything here.
		}

		private void OnAnnounceComplete(object sender, AnnounceResponseEventArgs e)
		{
			_updateSucceeded = e.Successful;

			if (e.Successful)
			{
				Toolbox.Switch<Tracker>(e.TrackerId.Tracker.Tier.Trackers, 0, e.TrackerId.Tracker.Tier.IndexOf(e.Tracker));
				int count = _manager.AddPeers(e.Peers);
				_manager.RaisePeersFound(new TrackerPeersAdded(_manager, count, e.Peers.Count, e.Tracker));

				e.TrackerId.TrySetResult(null);
			}
			else
			{
				TrackerTier tier;
				Tracker tracker;
				GetNextTracker(e.TrackerId.Tracker, out tier, out tracker);

				if (!e.TrackerId.TrySubsequent || tier == null || tracker == null)
				{
					e.TrackerId.TrySetException(new Exception("Failed to announce to tracker."));
					return;
				}
				Announce(tracker, e.TrackerId.TorrentEvent, true);
			}
		}

		public Task Scrape()
		{
			return Scrape(CurrentTracker, false);
		}

		public Task Scrape(Tracker tracker)
		{
			return Scrape(tracker, false);
		}

		private Task Scrape(Tracker tracker, bool trySubsequent)
		{
			if (tracker == null)
			{
				return CompletedTask.Default;
			}

			if (!tracker.CanScrape)
			{
				throw new TorrentException("This tracker does not support scraping");
			}

			TrackerConnectionId id = new TrackerConnectionId(tracker, trySubsequent, TorrentEvent.None, null);
			return tracker.Scrape(new ScrapeParameters(id, _infoHash));
		}

		#endregion
	}
}
