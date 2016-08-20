using System;
using System.Collections.Generic;
using System.Text;
using Zen.Trunk.Torrent.Dht.Messages;
using System.Threading.Tasks;

namespace Zen.Trunk.Torrent.Dht.Tasks
{
	internal class SendQueryTask
	{
		private DhtEngine _engine;
		private Node _node;
		private QueryMessage _query;
		private int _retries;
		private int _origRetries;
		private TaskCompletionSource<SendQueryEventArgs> _tcs;

		public SendQueryTask(DhtEngine engine, QueryMessage query, Node node)
			: this(engine, query, node, 3)
		{
		}

		public SendQueryTask(DhtEngine engine, QueryMessage query, Node node, int retries)
		{
			if (engine == null)
			{
				throw new ArgumentNullException("engine");
			}
			if (query == null)
			{
				throw new ArgumentNullException("message");
			}
			if (node == null)
			{
				throw new ArgumentNullException("message");
			}

			_engine = engine;
			_query = query;
			_node = node;
			_retries = retries;
			_origRetries = retries;
		}

		public int Retries
		{
			get
			{
				return _origRetries;
			}
		}

		public Node Target
		{
			get
			{
				return _node;
			}
		}

		public Task<SendQueryEventArgs> Execute()
		{
			_tcs = new TaskCompletionSource<SendQueryEventArgs>();
			_engine.MessageLoop.QuerySent += MessageSent;
			_engine.MessageLoop.EnqueueSend(_query, _node);
			return _tcs.Task.ContinueWith(
				(task) =>
				{
					_engine.MessageLoop.QuerySent -= MessageSent;
					return task.Result;
				});
		}

		private void MessageSent(object sender, SendQueryEventArgs e)
		{
			if (e.Query != _query)
			{
				return;
			}

			// If the message timed out and we we haven't already hit the maximum retries
			// send again. Otherwise we propagate the eventargs through the Complete event.
			if (e.TimedOut)
			{
				_node.FailedCount++;
			}
			else
			{
				Target.LastSeen = DateTime.UtcNow;
			}

			if (e.TimedOut && --_retries > 0)
			{
				_engine.MessageLoop.EnqueueSend(_query, _node);
			}
			else
			{
				_tcs.TrySetResult(e);
			}
		}
	}
}
