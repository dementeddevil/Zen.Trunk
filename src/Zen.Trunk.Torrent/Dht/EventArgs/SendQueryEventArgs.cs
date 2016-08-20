using System;
using System.Collections.Generic;
using System.Text;
using Zen.Trunk.Torrent.Dht.Messages;
using System.Net;

namespace Zen.Trunk.Torrent.Dht
{
	internal class SendQueryEventArgs : EventArgs
	{
		private IPEndPoint endpoint;
		private QueryMessage query;
		private ResponseMessage response;

		public IPEndPoint EndPoint
		{
			get
			{
				return endpoint;
			}
		}

		public QueryMessage Query
		{
			get
			{
				return query;
			}
		}

		public ResponseMessage Response
		{
			get
			{
				return response;
			}
		}

		public bool TimedOut
		{
			get
			{
				return response == null;
			}
		}

		public SendQueryEventArgs(IPEndPoint endpoint, QueryMessage query, ResponseMessage response)
		{
			this.endpoint = endpoint;
			this.query = query;
			this.response = response;
		}
	}
}
