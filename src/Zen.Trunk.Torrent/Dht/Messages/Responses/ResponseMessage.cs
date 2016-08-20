namespace Zen.Trunk.Torrent.Dht.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Zen.Trunk.Torrent.Bencoding;

	internal abstract class ResponseMessage : DhtMessage
	{
		internal static readonly BEncodedString ResponseType = "r";
		private static readonly BEncodedString ReturnValuesKey = "r";
		private QueryMessage _queryMessage;

		protected ResponseMessage(NodeId id)
			: base(ResponseType)
		{
			Properties.Add(ReturnValuesKey, new BEncodedDictionary());
			Parameters.Add(IdKey, id.BencodedString());
		}

		protected ResponseMessage(BEncodedDictionary d, QueryMessage m)
			: base(d)
		{
			_queryMessage = m;
		}

		internal override NodeId Id
		{
			get
			{
				return new NodeId((BEncodedString)Parameters[IdKey]);
			}
		}
		public BEncodedDictionary Parameters
		{
			get
			{
				return (BEncodedDictionary)Properties[ReturnValuesKey];
			}
		}

		public QueryMessage Query
		{
			get
			{
				return _queryMessage;
			}
			protected set
			{
				_queryMessage = value;
			}
		}
	}
}
