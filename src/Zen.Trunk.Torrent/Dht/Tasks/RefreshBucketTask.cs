using System;
using System.Collections.Generic;
using System.Text;
using Zen.Trunk.Torrent.Dht.Messages;
using Zen.Trunk.Torrent.Dht.Tasks;
using System.Threading.Tasks;

namespace Zen.Trunk.Torrent.Dht
{
	internal class RefreshBucketTask
	{
		private DhtEngine _engine;
		private Bucket _bucket;

		public RefreshBucketTask(DhtEngine engine, Bucket bucket)
		{
			_engine = engine;
			_bucket = bucket;
		}

		public async Task Execute()
		{
			if (_bucket.Nodes.Count > 0)
			{
				Console.WriteLine("Choosing first from: {0}", _bucket.Nodes.Count);
				_bucket.SortBySeen();
				await QueryNode(_bucket.Nodes[0]);
			}
		}

		private async Task QueryNode(Node node)
		{
			FindNode message = new FindNode(_engine.LocalId, node.Id);
			SendQueryTask task = new SendQueryTask(_engine, message, node);
			SendQueryEventArgs args = await task.Execute();
			if (args.TimedOut)
			{
				_bucket.SortBySeen();
				int index = _bucket.Nodes.IndexOf(node);
				if (index == -1 || (++index < _bucket.Nodes.Count))
				{
					await QueryNode(_bucket.Nodes[0]);
				}
			}
		}
	}
}