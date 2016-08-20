namespace Zen.Trunk.Torrent.Client.Tracker
{
	using System;
	using System.Net;
	using System.Net.Sockets;
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Client.Messages.UdpTracker;

	public class UdpTracker : Tracker
	{
		private AnnounceParameters _storedParams;
		private long _connectionId;
		private UdpClient _tracker;
		private IPEndPoint _endpoint;
		private bool _hasConnected;
		private bool _amConnecting;

		public UdpTracker(Uri announceUrl)
			: base(announceUrl)
		{
			CanScrape = false;
			_tracker = new UdpClient(announceUrl.Host, announceUrl.Port);
			_endpoint = (IPEndPoint)_tracker.Client.RemoteEndPoint;
		}

		public override async Task Announce(AnnounceParameters parameters)
		{
			LastUpdated = DateTime.UtcNow;
			if (!_hasConnected && _amConnecting)
			{
				return;
			}

			if (!_hasConnected)
			{
				_storedParams = parameters;
				_amConnecting = true;
				await Connect();
				return;
			}

			AnnounceMessage m = new AnnounceMessage(_connectionId, parameters);
			byte[] data = null;
			try
			{
				data = await SendMessageAndGetResponseAsync(m.Encode(), m.ByteLength);
			}
			catch (SocketException)
			{
				TrackerConnectionId id = new TrackerConnectionId(this, false, Zen.Trunk.Torrent.Common.TorrentEvent.None, null);
				AnnounceResponseEventArgs e = new AnnounceResponseEventArgs(id);
				e.Successful = false;
				RaiseAnnounceComplete(e);
				return;
			}

			UdpTrackerMessage message = UdpTrackerMessage.DecodeMessage(
				data, 0, data.Length);
			CompleteAnnounce(message);
		}

		public override Task Scrape(ScrapeParameters parameters)
		{
			throw new NotImplementedException(
				"The method or operation is not implemented.");
		}

		private void CompleteAnnounce(UdpTrackerMessage message)
		{
			TrackerConnectionId id = new TrackerConnectionId(this, false, Zen.Trunk.Torrent.Common.TorrentEvent.None, null);
			AnnounceResponseEventArgs e = new AnnounceResponseEventArgs(id);
			ErrorMessage error = message as ErrorMessage;
			if (error != null)
			{
				e.Successful = false;
				FailureMessage = error.Error;
			}
			else
			{
				AnnounceResponseMessage response = (AnnounceResponseMessage)message;
				e.Successful = true;
				e.Peers.AddRange(response.Peers);
			}

			RaiseAnnounceComplete(e);
		}

		private async Task Connect()
		{
			ConnectMessage message = new ConnectMessage();
			byte[] response = null;
			try
			{
				_tracker.Connect(Uri.Host, Uri.Port);
				response = await SendMessageAndGetResponseAsync(
					message.Encode(), message.ByteLength);
			}
			catch (SocketException)
			{
				TrackerConnectionId id = new TrackerConnectionId(this, false, Zen.Trunk.Torrent.Common.TorrentEvent.None, null);
				AnnounceResponseEventArgs e = new AnnounceResponseEventArgs(id);
				e.Successful = false;
				RaiseAnnounceComplete(e);
				return;
			}

			ConnectResponseMessage m = (ConnectResponseMessage)
				UdpTrackerMessage.DecodeMessage(response, 0, response.Length);
			_connectionId = m.ConnectionId;
			_hasConnected = true;
			_amConnecting = false;

			await Announce(_storedParams);
			_storedParams = null;
		}

		private async Task<byte[]> SendMessageAndGetResponseAsync(
			byte[] message, int length)
		{
			await _tracker.SendAsync(message, length);
			return await Task.Factory.FromAsync<byte[]>(
				_tracker.BeginReceive(null, null),
				(ar) =>
				{
					return _tracker.EndReceive(ar, ref _endpoint);
				});
		}
	}
}
