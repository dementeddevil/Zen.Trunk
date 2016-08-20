namespace Zen.Trunk.Torrent.Client.Connections
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Threading.Tasks;
	using Zen.Trunk.Torrent.Client.Messages;
	using Zen.Trunk.Torrent.Client.Messages.Standard;
	using Zen.Trunk.Torrent.Common;

	public partial class HttpConnection : ConnectionBase
	{
		#region Private Types
		private class HttpResult : TaskCompletionSource<int>
		{
			public HttpResult(byte[] buffer, int offset, int count)
			{
				Buffer = buffer;
				Offset = offset;
				Count = count;
			}

			#region Public Properties
			public byte[] Buffer
			{
				get;
				private set;
			}

			public int Offset
			{
				get;
				private set;
			}

			public int Count
			{
				get;
				private set;
			}

			public int BytesTransferred
			{
				get;
				set;
			}
			#endregion

			public void Complete(int bytes)
			{
				BytesTransferred = bytes;
				SetResult(bytes);
			}
		} 
		#endregion

		#region Private Fields
		private HttpRequestData _currentRequest;
		private Stream _dataStream;
		private TorrentManager _manager;
		private HttpResult _receiveResult;
		private List<RequestMessage> _requestMessages;
		private HttpResult _sendResult;
		private int _totalExpected;
		private Uri _uri;
		private Queue<KeyValuePair<WebRequest, int>> _webRequests;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="HttpConnection"/> class.
		/// </summary>
		/// <param name="uri">The URI.</param>
		public HttpConnection(Uri uri)
		{
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}
			if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("Scheme is not http");
			}

			_uri = uri;
			_requestMessages = new List<RequestMessage>();
			_webRequests = new Queue<KeyValuePair<WebRequest, int>>();
		}
		#endregion

		#region Public Properties
		public override byte[] AddressBytes
		{
			get
			{
				return new byte[4];
			}
		}

		public override bool CanReconnect
		{
			get
			{
				return false;
			}
		}

		public override bool Connected
		{
			get
			{
				return true;
			}
		}

		private HttpRequestData CurrentRequest
		{
			get
			{
				return _currentRequest;
			}
		}

		public override EndPoint EndPoint
		{
			get
			{
				return null;
			}
		}

		public override bool IsIncoming
		{
			get
			{
				return false;
			}
		}

		public TorrentManager Manager
		{
			get
			{
				return _manager;
			}
			set
			{
				_manager = value;
			}
		}

		public override Uri Uri
		{
			get
			{
				return _uri;
			}
		}
		#endregion

		public override Task ConnectAsync()
		{
			return CompletedTask.Default;
		}

		public override async Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
		{
			if (_receiveResult != null)
			{
				throw new InvalidOperationException("Cannot call ReceiveAsync twice");
			}

			_receiveResult = new HttpResult(buffer, offset, count);
			try
			{
				// ReceiveAsync has been called *before* we have sent a piece request.
				// Wait for a piece request to be sent before allowing this to complete.
				if (_dataStream == null)
				{
					return await _receiveResult.Task;
				}

				await DoReceive();
			}
			catch (Exception ex)
			{
				if (_sendResult != null)
				{
					_sendResult.TrySetException(ex);
				}

				if (_receiveResult != null)
				{
					_receiveResult.TrySetException(ex);
				}
			}
			return await _receiveResult.Task;
		}

		public override async Task<int> SendAsync(byte[] buffer, int offset, int count)
		{
			if (_sendResult != null)
			{
				throw new InvalidOperationException("Cannot call SendAsync twice");
			}

			_sendResult = new HttpResult(buffer, offset, count);
			try
			{
				List<PeerMessage> bundle = new List<PeerMessage>();
				for (int i = offset; i < offset + count; )
				{
					PeerMessage message = PeerMessage.DecodeMessage(buffer, i, count + offset - i, null);
					bundle.Add(message);
					i += message.ByteLength;
				}

				if (bundle.TrueForAll((message) => message is RequestMessage))
				{
					_requestMessages.AddRange(bundle.Cast<RequestMessage>());

					// The RequestMessages are always sequential
					RequestMessage start = (RequestMessage)bundle[0];
					RequestMessage end = (RequestMessage)bundle[bundle.Count - 1];
					CreateWebRequests(start, end);

					KeyValuePair<WebRequest, int> r = _webRequests.Dequeue();
					_totalExpected = r.Value;

					WebResponse response = await r.Key.GetResponseAsync();
					_dataStream = response.GetResponseStream();

					if (_receiveResult != null)
					{
						await DoReceive();
					}
				}
				else
				{
					_sendResult.Complete(count);
				}
			}
			catch (Exception ex)
			{
				_sendResult.TrySetException(ex);
			}

			return await _sendResult.Task;
		}

		protected override void DisposeManagedObjects()
		{
			if (_dataStream != null)
			{
				_dataStream.Dispose();
				_dataStream = null;
			}

			base.DisposeManagedObjects();
		}

		private void CreateWebRequests(RequestMessage start, RequestMessage end)
		{
			// Properly handle the case where we have multiple files
			// This is only implemented for single file torrents
			Uri u = _uri;

			if (_uri.OriginalString.EndsWith("/"))
			{
				u = new Uri(_uri, Manager.Torrent.Name);
			}

			// startOffset and endOffset are *inclusive*. I need to subtract '1' from the end index so that i
			// stop at the correct byte when requesting the byte ranges from the server
			long startOffset = (long)start.PieceIndex * _manager.Torrent.PieceLength + start.StartOffset;
			long endOffset = (long)end.PieceIndex * _manager.Torrent.PieceLength + end.StartOffset + end.RequestLength;

			foreach (TorrentFile file in _manager.Torrent.Files)
			{
				if (endOffset == 0)
				{
					break;
				}

				// We want data from a later file
				if (startOffset >= file.Length)
				{
					startOffset -= file.Length;
					endOffset -= file.Length;
				}
				// We want data from the end of the current file and from the next few files
				else if (endOffset >= file.Length)
				{
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(u, file.Path));
					request.AddRange((int)startOffset, (int)(file.Length - 1));
					_webRequests.Enqueue(new KeyValuePair<WebRequest, int>(request, (int)(file.Length - startOffset)));
					startOffset = 0;
					endOffset -= file.Length;
				}
				// All the data we want is from within this file
				else
				{
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(u, file.Path));
					request.AddRange((int)startOffset, (int)(endOffset - 1));
					_webRequests.Enqueue(new KeyValuePair<WebRequest, int>(request, (int)(endOffset - startOffset)));
					endOffset = 0;
				}
			}
		}

		private async Task DoReceive()
		{
			byte[] buffer = _receiveResult.Buffer;
			int offset = _receiveResult.Offset;
			int count = _receiveResult.Count;

			// Use next request message if we have no current request
			if (_currentRequest == null && _requestMessages.Count > 0)
			{
				_currentRequest = new HttpRequestData(_requestMessages[0]);
				_requestMessages.RemoveAt(0);
			}

			while (_totalExpected == 0)
			{
				if (_webRequests.Count == 0)
				{
					_sendResult.TrySetResult(0);
					return;
				}
				else
				{
					KeyValuePair<WebRequest, int> r = _webRequests.Dequeue();
					_totalExpected = r.Value;

					WebResponse response = await r.Key.GetResponseAsync();
					_dataStream = response.GetResponseStream();

					if (_receiveResult == null)
					{
						return;
					}
				}
				return;
			}

			if (!_currentRequest.SentLength)
			{
				// The message length counts as the first four bytes
				_currentRequest.SentLength = true;
				_currentRequest.TotalReceived += 4;
				Message.Write(_receiveResult.Buffer, _receiveResult.Offset, _currentRequest.TotalToReceive - _currentRequest.TotalReceived);
				_receiveResult.Complete(4);
				return;
			}
			else if (!_currentRequest.SentHeader)
			{
				_currentRequest.SentHeader = true;

				// We have *only* written the messageLength to the stream
				// Now we need to write the rest of the PieceMessage header
				int written = 0;
				written += Message.Write(buffer, offset + written, PieceMessage.MessageId);
				written += Message.Write(buffer, offset + written, CurrentRequest.Request.PieceIndex);
				written += Message.Write(buffer, offset + written, CurrentRequest.Request.StartOffset);
				count -= written;
				offset += written;
				_receiveResult.BytesTransferred += written;
				_currentRequest.TotalReceived += written;
			}

			try
			{
				int received = await _dataStream.ReadAsync(buffer, offset, count);
				if (received == 0)
				{
					throw new WebException("No futher data is available");
				}

				_receiveResult.BytesTransferred += received;
				_currentRequest.TotalReceived += received;

				// We've received everything for this piece, so null it out
				if (_currentRequest.IsComplete)
				{
					_currentRequest = null;
				}

				_totalExpected -= received;
				_receiveResult.TrySetResult(_receiveResult.BytesTransferred);
			}
			catch (Exception ex)
			{
				_receiveResult.TrySetException(ex);
			}
			finally
			{
				// If there are no more requests pending, complete the Send call
				if (_currentRequest == null && _requestMessages.Count == 0)
				{
					_dataStream.Dispose();
					_dataStream = null;

					// Let MonoTorrent know we've finished requesting everything it asked for
					if (_sendResult != null)
						_sendResult.Complete(_sendResult.Count);
				}
			}
		}
	}
}
