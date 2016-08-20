namespace Zen.Trunk.Torrent.Client.Tracker
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using System.Web;
	using Zen.Trunk.Torrent.Bencoding;
	using Zen.Trunk.Torrent.Common;

	public class HttpTracker : Tracker
	{
		private static readonly BEncodedString CustomErrorKey = (BEncodedString)"custom error";
		private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
		private static readonly Regex AnnounceRegex = new Regex(
				"/announce$",
				RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private Uri _scrapeUrl;

		public HttpTracker(Uri announceUrl)
			: base(announceUrl)
		{
			if (AnnounceRegex.IsMatch(announceUrl.OriginalString))
			{
				_scrapeUrl = new Uri(AnnounceRegex.Replace(announceUrl.OriginalString, "/scrape"));
				CanScrape = true;
			}
		}

		public override async Task Announce(AnnounceParameters parameters)
		{
			string announceUri = CreateAnnounceUri(parameters);
			System.Diagnostics.Debug.WriteLine(
				"Sending announce to {0}",
				(object)announceUri);
			LastUpdated = DateTime.UtcNow;
			UpdateSucceeded = true;

			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(announceUri);
			request.Proxy = new WebProxy();   // If i don't do this, i can't run the webrequest. It's wierd.
			request.Timeout = RequestTimeout.Milliseconds;
			parameters.Id.Request = request;
			UpdateState(TrackerState.Announcing, parameters.Torrent);

			// Get the response from the tracker server
			BEncodedDictionary dict = null;
			try
			{
				Task<WebResponse> getResponseTask = request.GetResponseAsync();
				HttpWebResponse result = (HttpWebResponse)await getResponseTask;
				dict = DecodeResponse(result, parameters.Id);
			}
			catch (Exception error)
			{
				System.Diagnostics.Debug.WriteLine(
					"Announce failed : {0}",
					(object)error.Message);
				dict = new BEncodedDictionary();
				dict.Add(CustomErrorKey, (BEncodedString)"The tracker could not be contacted");
			}

			// Process the response
			ProcessAnnounceResponse(dict, parameters.Torrent, parameters.Id);
		}

		public override async Task Scrape(ScrapeParameters parameters)
		{
			string scrapeUri = CreateScrapeUri(parameters);
			System.Diagnostics.Debug.WriteLine(
				"Sending scrape to {0}",
				(object)scrapeUri);

			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(scrapeUri);
			request.Timeout = RequestTimeout.Milliseconds;
			parameters.Id.Request = request;
			UpdateState(TrackerState.Scraping);

			// Get the response from the tracker server
			BEncodedDictionary dict = null;
			try
			{
				Task<WebResponse> getResponseTask = request.GetResponseAsync();
				HttpWebResponse result = (HttpWebResponse)await getResponseTask;
				dict = DecodeResponse(result, parameters.Id);
			}
			catch (Exception error)
			{
				System.Diagnostics.Debug.WriteLine(
					"Scrape failed : {0}", 
					(object)error.Message);
				dict = new BEncodedDictionary();
				dict.Add(CustomErrorKey, (BEncodedString)"The tracker could not be contacted");
			}

			// Process the response
			ProcessScrapeResponse(dict, parameters.Id);
		}

		protected string CreateAnnounceUri(AnnounceParameters parameters)
		{
			StringBuilder sb = new StringBuilder(256);

			sb.Append(this.Uri);
			sb.Append((this.Uri.OriginalString.IndexOf('?') == -1) ? '?' : '&');
			sb.Append("info_hash=");
			sb.Append(HttpUtility.UrlEncode(parameters.InfoHash));
			sb.Append("&peer_id=");
			sb.Append(parameters.PeerId);
			sb.Append("&port=");
			sb.Append(parameters.Port);
			if (parameters.SupportsEncryption)
			{
				sb.Append("&supportcrypto=1");
			}
			if (parameters.RequireEncryption)
			{
				sb.Append("&requirecrypto=1");
			}
			sb.Append("&uploaded=");
			sb.Append(parameters.BytesUploaded);
			sb.Append("&downloaded=");
			sb.Append(parameters.BytesDownloaded);
			sb.Append("&left=");
			sb.Append(parameters.BytesLeft);
			sb.Append("&compact=1");    // Always use compact response
			sb.Append("&numwant=");
			sb.Append(100);
			sb.Append("&key=");  // The 'key' protocol, used as a kind of 'password'. Must be the same between announces
			sb.Append(Key);
			if (parameters.IpAddress != null)
			{
				sb.Append("&ip=");
				sb.Append(parameters.IpAddress);
			}

			// If we have not successfully sent the started event to this tier, override the passed in started event
			// Otherwise append the event if it is not "none"
			if (!parameters.Id.Tracker.Tier.SentStartedEvent)
			{
				System.Diagnostics.Debug.Assert(parameters.ClientEvent != TorrentEvent.Stopped);
				sb.Append("&event=started");
				parameters.Id.Tracker.Tier.SendingStartedEvent = true;
			}
			else if (parameters.ClientEvent != TorrentEvent.None)
			{
				sb.Append("&event=");
				sb.Append(parameters.ClientEvent.ToString().ToLower());
			}
			if (!string.IsNullOrEmpty(TrackerId))
			{
				sb.Append("&trackerid=");
				sb.Append(TrackerId);
			}
			return sb.ToString();
		}

		protected string CreateScrapeUri(ScrapeParameters parameters)
		{
			string trackerUri = _scrapeUrl.OriginalString;

			// If set to false, you could retrieve scrape data for *all* torrents hosted by the tracker. I see no practical use
			// at the moment, so i've removed the ability to set this to false.
			if (true)
			{
				if (trackerUri.IndexOf('?') == -1)
				{
					trackerUri += "?info_hash=" + HttpUtility.UrlEncode(parameters.InfoHash);
				}
				else
				{
					trackerUri += "&info_hash=" + HttpUtility.UrlEncode(parameters.InfoHash);
				}
			}

			return trackerUri;
		}

		private BEncodedDictionary DecodeResponse(
			HttpWebResponse response, TrackerConnectionId id)
		{
			int bytesRead = 0;
			int totalRead = 0;
			byte[] buffer = new byte[2048];

			try
			{
				using (MemoryStream dataStream = new MemoryStream(response.ContentLength > 0 ? (int)response.ContentLength : 256))
				{
					using (BinaryReader reader = new BinaryReader(response.GetResponseStream()))
					{
						// If there is a ContentLength, use that to decide how much we read.
						if (response.ContentLength > 0)
						{
							while (totalRead < response.ContentLength)
							{
								bytesRead = reader.Read(buffer, 0, buffer.Length);
								dataStream.Write(buffer, 0, bytesRead);
								totalRead += bytesRead;
							}
						}
						else    // A compact response doesn't always have a content length, so we
						{       // just have to keep reading until we think we have everything.
							while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
							{
								dataStream.Write(buffer, 0, bytesRead);
							}
						}
					}
					response.Close();
					dataStream.Seek(0, SeekOrigin.Begin);
					return (BEncodedDictionary)BEncodedValue.Decode(dataStream);
				}
			}
			catch (WebException)
			{
				BEncodedDictionary dict = new BEncodedDictionary();
				dict.Add(CustomErrorKey, (BEncodedString)"The tracker could not be contacted");
				return dict;
			}
			catch (BEncodingException)
			{
				BEncodedDictionary dict = new BEncodedDictionary();
				dict.Add(CustomErrorKey, (BEncodedString)"The tracker returned an invalid or incomplete response");
				return dict;
			}
		}

		private void ProcessAnnounceResponse(
			BEncodedDictionary dict, 
			TorrentManager torrentManager, 
			TrackerConnectionId id)
		{
			AnnounceResponseEventArgs args = new AnnounceResponseEventArgs(id);

			UpdateSucceeded = !dict.ContainsKey(CustomErrorKey);
			if (!UpdateSucceeded)
			{
				FailureMessage = dict[CustomErrorKey].ToString();
				UpdateState(TrackerState.AnnouncingFailed, torrentManager);
			}
			else
			{
				if (id.Tracker.Tier.SendingStartedEvent)
				{
					id.Tracker.Tier.SentStartedEvent = true;
				}

				HandleAnnounce(dict, args);
				UpdateState(TrackerState.AnnounceSuccessful, torrentManager);
			}

			id.Tracker.Tier.SendingStartedEvent = false;
			args.Successful = UpdateSucceeded;
			RaiseAnnounceComplete(args);
		}

		private void ProcessScrapeResponse(
			BEncodedDictionary dict, TrackerConnectionId id)
		{
			bool successful = !dict.ContainsKey("custom error");
			ScrapeResponseEventArgs args = 
				new ScrapeResponseEventArgs(this, successful);
			if (!successful)
			{
				FailureMessage = dict["custom error"].ToString();
				UpdateState(TrackerState.ScrapingFailed);
			}
			else if (!dict.ContainsKey("files"))
			{
				args.Successful = false;
				UpdateState(TrackerState.ScrapingFailed);
			}
			else
			{
				HandleScrape(dict, args);
				UpdateState(TrackerState.ScrapeSuccessful);
			}
			RaiseScrapeComplete(args);
		}

		private void HandleAnnounce(
			BEncodedDictionary dict, AnnounceResponseEventArgs args)
		{
			foreach (KeyValuePair<BEncodedString, BEncodedValue> keypair in dict)
			{
				switch (keypair.Key.Text)
				{
					case ("complete"):
						Complete = Convert.ToInt32(keypair.Value.ToString());
						break;

					case ("incomplete"):
						Incomplete = Convert.ToInt32(keypair.Value.ToString());
						break;

					case ("downloaded"):
						Downloaded = Convert.ToInt32(keypair.Value.ToString());
						break;

					case ("tracker id"):
						TrackerId = keypair.Value.ToString();
						break;

					case ("min interval"):
						MinUpdateInterval = int.Parse(keypair.Value.ToString());
						break;

					case ("interval"):
						UpdateInterval = int.Parse(keypair.Value.ToString());
						break;

					case ("peers"):
						if (keypair.Value is BEncodedList)          // Non-compact response
						{
							args.Peers.AddRange(Peer.Decode((BEncodedList)keypair.Value));
						}
						else if (keypair.Value is BEncodedString)   // Compact response
						{
							args.Peers.AddRange(Peer.Decode((BEncodedString)keypair.Value));
						}
						break;

					case ("failure reason"):
						FailureMessage = keypair.Value.ToString();
						args.Successful = false;
						break;

					case ("warning message"):
						WarningMessage = keypair.Value.ToString();
						break;

					default:
						Logger.Log(null, "HttpTracker - Unknown announce tag received: Key {0}  Value: {1}", keypair.Key.ToString(), keypair.Value.ToString());
						break;
				}
			}
		}

		private void HandleScrape(
			BEncodedDictionary dict, ScrapeResponseEventArgs args)
		{
			BEncodedDictionary files = (BEncodedDictionary)dict["files"];
			foreach (KeyValuePair<BEncodedString, BEncodedValue> keypair in files)
			{
				BEncodedDictionary d = (BEncodedDictionary)keypair.Value;
				foreach (KeyValuePair<BEncodedString, BEncodedValue> kp in d)
				{
					switch (kp.Key.ToString())
					{
						case ("complete"):
							Complete = Convert.ToInt32(kp.Value.ToString());
							break;

						case ("downloaded"):
							Downloaded = Convert.ToInt32(kp.Value.ToString());
							break;

						case ("incomplete"):
							Incomplete = Convert.ToInt32(kp.Value.ToString());
							break;

						default:
							Logger.Log(null, "HttpTracker - Unknown scrape tag received: Key {0}  Value {1}", kp.Key.ToString(), kp.Value.ToString());
							break;
					}
				}
			}
		}

		public override bool Equals(object obj)
		{
			HttpTracker tracker = obj as HttpTracker;
			if (tracker == null)
				return false;

			// If the announce URL matches, then CanScrape and the scrape URL must match too
			return (this.Uri.Equals(tracker.Uri));
		}
	}
}
