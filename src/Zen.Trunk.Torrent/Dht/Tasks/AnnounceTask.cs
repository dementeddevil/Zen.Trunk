using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Zen.Trunk.Torrent.Dht.Messages;

namespace Zen.Trunk.Torrent.Dht.Tasks
{
	internal class AnnounceTask
	{
		private DhtEngine _engine;
		private NodeId _infoHash;
		private int _port;

		public AnnounceTask(DhtEngine engine, byte[] infoHash, int port)
			: this(engine, new NodeId(infoHash), port)
		{

		}

		public AnnounceTask(DhtEngine engine, NodeId infoHash, int port)
		{
			_engine = engine;
			_infoHash = infoHash;
			_port = port;
		}

		public async Task Execute()
		{
			GetPeersTask getpeers = new GetPeersTask(_engine, _infoHash);
			await getpeers.Execute();

			List<Task> subTasks = new List<Task>();
			foreach (Node n in getpeers.ClosestActiveNodes.Values)
			{
				if (n.Token == null)
				{
					continue;
				}

				AnnouncePeer query = new AnnouncePeer(_engine.LocalId, _infoHash, _port, n.Token);
				SendQueryTask task = new SendQueryTask(_engine, query, n);
				subTasks.Add(task.Execute());
			}
			await TaskExtra.WhenAllOrEmpty(subTasks.ToArray());
		}
	}
}
