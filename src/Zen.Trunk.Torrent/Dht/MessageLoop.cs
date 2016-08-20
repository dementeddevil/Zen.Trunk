namespace Zen.Trunk.Torrent.Dht
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using Zen.Trunk.Torrent.Bencoding;
	using Zen.Trunk.Torrent.Common;
	using Zen.Trunk.Torrent.Dht.Listeners;
	using Zen.Trunk.Torrent.Dht.Messages;
	using System.Collections.Concurrent;

	internal class MessageLoop
	{
		private struct SendDetails
		{
			public SendDetails(IPEndPoint destination, DhtMessage message)
			{
				Destination = destination;
				Message = message;
				SentAt = DateTime.MinValue;
			}
			public IPEndPoint Destination;
			public DhtMessage Message;
			public DateTime SentAt;
		}

		private DhtEngine _engine;
		private int _lastSent;
		private DhtListener _listener;
		private object _locker = new object();
		private ConcurrentQueue<SendDetails> _sendQueue = 
			new ConcurrentQueue<SendDetails>();
		private ConcurrentQueue<KeyValuePair<IPEndPoint, DhtMessage>> _receiveQueue = 
			new ConcurrentQueue<KeyValuePair<IPEndPoint, DhtMessage>>();
		private CloneableList<SendDetails> _waitingResponse = new CloneableList<SendDetails>();

		internal event EventHandler<SendQueryEventArgs> QuerySent;

		public MessageLoop(DhtEngine engine, DhtListener listener)
		{
			_engine = engine;
			_listener = listener;
			listener.MessageReceived += new MessageReceived(MessageReceived);
			DhtEngine.MainLoop.QueueRecurring(
				TimeSpan.FromMilliseconds(10),
				() =>
				{
					if (engine.Disposed)
					{
						return false;
					}

					SendMessage();
					ReceiveMessage();
					TimeoutMessage();

					return !engine.Disposed;
				});
		}

		private bool CanSend
		{
			get
			{
				return _sendQueue.Count > 0 &&
					(Environment.TickCount - _lastSent) > 5;
			}
		}

		internal void Start()
		{
			if (_listener.Status != ListenerStatus.Listening)
			{
				_listener.Start();
			}
		}

		internal void Stop()
		{
			if (_listener.Status != ListenerStatus.NotListening)
			{
				_listener.Stop();
			}
		}

		private void MessageReceived(byte[] buffer, IPEndPoint endpoint)
		{
			// I should check the IP address matches as well as the transaction id
			// FIXME: This should throw an exception if the message doesn't exist, we need to handle this
			// and return an error message (if that's what the spec allows)
			try
			{
				DhtMessage m = MessageFactory.DecodeMessage((BEncodedDictionary)BEncodedValue.Decode(buffer));
				_receiveQueue.Enqueue(new KeyValuePair<IPEndPoint, DhtMessage>(endpoint, m));
			}
			catch (MessageException ex)
			{
				System.Diagnostics.Debug.WriteLine("Message exception: {0}", ex);
				// Caused by bad transaction id usually - ignore
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("General exception {0}", ex);
				//throw new Exception("IP:" + endpoint.Address.ToString() + "bad transaction:" + e.Message);
			}
		}

		private void ReceiveMessage()
		{
			KeyValuePair<IPEndPoint, DhtMessage> receive;
			if(_receiveQueue.TryDequeue(out receive))
			{
				DhtMessage message = receive.Value;
				IPEndPoint source = receive.Key;

				// Remove waiting messages with this transaction id
				_waitingResponse.RemoveAll(
					(msg) => msg.Message.TransactionId.Equals(message.TransactionId));

				System.Diagnostics.Debug.WriteLine(
					"Received: {0} from {1}", message.GetType().Name, source);
				try
				{
					Node node = _engine.RoutingTable.FindNode(message.Id);

					// What do i do with a null node?
					if (node == null)
					{
						node = new Node(message.Id, source);
						_engine.RoutingTable.Add(node);
					}
					node.Seen();
					System.Diagnostics.Debug.WriteLine(
						"Seen {0}", (object)node.Id.ToString());

					message.Handle(_engine, node);
					ResponseMessage response = message as ResponseMessage;
					if (response != null)
					{
						RaiseMessageSent(node.EndPoint, response.Query, response);
					}
				}
				catch (MessageException ex)
				{
					System.Diagnostics.Debug.WriteLine("Incoming message error: {0}", ex);
					// Normal operation (FIXME: do i need to send a response error message?) 
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine("Handle Error for message: {0}", ex);
					EnqueueSend(
						new ErrorMessage(
							ErrorCode.GenericError,
							"Misshandle received message!"),
						source);
				}
			}
		}

		private void RaiseMessageSent(
			IPEndPoint endpoint, QueryMessage query, ResponseMessage response)
		{
			EventHandler<SendQueryEventArgs> handle = QuerySent;
			if (handle != null)
			{
				handle(this, new SendQueryEventArgs(endpoint, query, response));
			}
		}

		private void TimeoutMessage()
		{
			if (_waitingResponse.Count > 0)
			{
				if ((DateTime.UtcNow - _waitingResponse[0].SentAt) > _engine.TimeOut)
				{
					SendDetails details = _waitingResponse.Dequeue();
					MessageFactory.UnregisterSend((QueryMessage)details.Message);
					RaiseMessageSent(details.Destination, (QueryMessage)details.Message, null);
				}
			}
		}

		private void SendMessage()
		{
			SendDetails send;
			if (CanSend && _sendQueue.TryDequeue(out send))
			{
				SendMessage(send.Message, send.Destination);
				send.SentAt = DateTime.UtcNow;

				if (send.Message is QueryMessage)
				{
					_waitingResponse.Add(send);
				}
			}
		}

		private void SendMessage(DhtMessage message, IPEndPoint endpoint)
		{
			System.Diagnostics.Debug.WriteLine(
				"Sending: {0} to {1}", message.GetType().Name, endpoint);
			_lastSent = Environment.TickCount;
			byte[] buffer = message.Encode();
			_listener.Send(buffer, endpoint);
		}

		internal void EnqueueSend(DhtMessage message, Node node)
		{
			EnqueueSend(message, node.EndPoint);
		}

		internal void EnqueueSend(DhtMessage message, IPEndPoint endpoint)
		{
			lock (_locker)
			{
				if (message.TransactionId == null)
				{
					if (message is ResponseMessage)
					{
						throw new ArgumentException("Message must have a transaction id");
					}

					do
					{
						message.TransactionId = TransactionId.NextId();
					} while (MessageFactory.IsRegistered(message.TransactionId));
				}

				// We need to be able to cancel a query message if we time out waiting for a response
				if (message is QueryMessage)
				{
					MessageFactory.RegisterSend((QueryMessage)message);
				}

				_sendQueue.Enqueue(new SendDetails(endpoint, message));
			}
		}
	}
}

