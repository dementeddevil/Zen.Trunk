namespace Zen.Trunk.Torrent.Dht.Messages
{
	using Zen.Trunk.Torrent.Bencoding;

	internal abstract class QueryMessage : DhtMessage
	{
		private static readonly BEncodedString QueryArgumentsKey = "a";
		private static readonly BEncodedString QueryNameKey = "q";
		internal static readonly BEncodedString QueryType = "q";

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryMessage"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="queryName">Name of the query.</param>
		/// <param name="responseCreator">The response creator.</param>
		protected QueryMessage(
			NodeId id,
			BEncodedString queryName,
			ResponseCreator responseCreator)
			: this(id, queryName, new BEncodedDictionary(), responseCreator)
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryMessage"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="queryName">Name of the query.</param>
		/// <param name="queryArguments">The query arguments.</param>
		/// <param name="responseCreator">The response creator.</param>
		protected QueryMessage(
			NodeId id,
			BEncodedString queryName,
			BEncodedDictionary queryArguments,
			ResponseCreator responseCreator)
			: base(QueryType)
		{
			Properties.Add(QueryNameKey, queryName);
			Properties.Add(QueryArgumentsKey, queryArguments);

			Parameters.Add(IdKey, id.BencodedString());
			ResponseCreator = responseCreator;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryMessage"/> class.
		/// </summary>
		/// <param name="d">The d.</param>
		/// <param name="responseCreator">The response creator.</param>
		protected QueryMessage(
			BEncodedDictionary d, ResponseCreator responseCreator)
			: base(d)
		{
			ResponseCreator = responseCreator;
		}

		internal override NodeId Id
		{
			get
			{
				return new NodeId((BEncodedString)Parameters[IdKey]);
			}
		}

		internal ResponseCreator ResponseCreator
		{
			get;
			private set;
		}

		protected BEncodedDictionary Parameters
		{
			get
			{
				return (BEncodedDictionary)Properties[QueryArgumentsKey];
			}
		}
	}
}
