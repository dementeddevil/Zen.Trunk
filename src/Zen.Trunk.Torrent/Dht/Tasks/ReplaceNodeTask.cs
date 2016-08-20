using System;
using System.Collections.Generic;
using System.Text;
using Zen.Trunk.Torrent.Dht.Messages;
using System.Threading.Tasks;

namespace Zen.Trunk.Torrent.Dht.Tasks
{
	internal class ReplaceNodeTask
	{
		private DhtEngine _engine;
		private Bucket _bucket;
		private Node _newNode;

		public ReplaceNodeTask(DhtEngine engine, Bucket bucket, Node newNode)
		{
			_engine = engine;
			_bucket = bucket;
			_newNode = newNode;
		}

		public async Task Execute()
		{
			if (_bucket.Nodes.Count > 0)
			{
				await SendPingToOldest();
			}
		}

		private async Task SendPingToOldest()
		{
			_bucket.SortBySeen();
			if ((DateTime.UtcNow - _bucket.Nodes[0].LastSeen) > TimeSpan.FromMinutes(3) && _newNode != null)
			{
				Node oldest = _bucket.Nodes[0];
				SendQueryTask task = new SendQueryTask(_engine, new Ping(_engine.LocalId), oldest);
				SendQueryEventArgs args = await task.Execute();
				if (args.TimedOut)
				{
					// If the node didn't respond and it's no longer in our
					//	bucket, we need to send a ping to the oldest node in
					//	the bucket
					// Otherwise if we have a non-responder and it's still there, replace it!
					int index = _bucket.Nodes.IndexOf(oldest);
					if (index < 0)
					{
						await SendPingToOldest();
					}
					else if(_newNode != null)
					{
						_bucket.Nodes[index] = _newNode;
						_newNode = null;
					}
				}
				else
				{
					await SendPingToOldest();
				}
			}
		}
	}
}
