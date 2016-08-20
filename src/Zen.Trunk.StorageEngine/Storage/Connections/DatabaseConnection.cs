// -----------------------------------------------------------------------
// <copyright file="DatabaseConnection.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.Storage.Locking
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks.Dataflow;
	using System.Collections.Concurrent;
using System.Transactions;
using Zen.Trunk.Storage.Data;

	public class ExternalDatabaseConnection
	{
		public Guid ConnectionId
		{
			get;
			set;
		}

		public IsolationLevel IsolationLevel
		{
			get;
			set;
		}

		public TimeSpan LockTimeout
		{
			get;
			set;
		}
	}

	/// <summary>
	/// <c>InternalDatabaseConnection</c> maintains the state necessary to link
	/// transaction state for a database to an external connection.
	/// </summary>
	/// <remarks>
	/// This object is necessary because a <see cref="DatabaseTransaction"/> is
	/// tied to a single instance of <see cref="DatabaseDevice"/>.
	/// This object makes it possible to have multiple connections participate
	/// within the same transaction.
	/// </remarks>
	public class InternalDatabaseConnection
	{
		internal InternalDatabaseConnection(string dbName)
		{
			ActiveDatabaseName = dbName;
		}

		/// <summary>
		/// Gets the name of the active database.
		/// </summary>
		/// <value>The default name of the database.</value>
		/// <remarks>
		/// This database is changed by the USE SQL command.
		/// </remarks>
		public string ActiveDatabaseName
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the transaction object used for the accessing the
		/// active database.
		/// </summary>
		public DatabaseTransaction Transaction
		{
			get;
			set;
		}

		public IsolationLevel IsolationLevel
		{
			get;
			set;
		}

		public TimeSpan LockTimeout
		{
			get;
			set;
		}

		internal IDisposable EnterTransaction(DatabaseDevice device)
		{
			// Setup the transaction object as required
			if (Transaction == null)
			{
				Transaction = new DatabaseTransaction(device, IsolationLevel, LockTimeout);
			}
			
			// Next we need to start a transaction scope - this scope must not
			//	commit or dispose of our transaction scope object when it falls
			//	out of scope
			return TrunkTransactionContext.SwitchTransactionContext(Transaction);
		}

		internal void Commit()
		{
			if (Transaction != null)
			{
				Transaction.Commit();
				Transaction.Dispose();
				Transaction = null;
			}
		}

		internal void Rollback()
		{
			if (Transaction != null)
			{
				Transaction.Rollback();
				Transaction.Dispose();
				Transaction = null;
			}
		}
	}

	public class InternalConnectionMapper
	{
		private class InternalDatabaseMapper : ConcurrentDictionary<string, InternalDatabaseConnection>
		{
			public InternalDatabaseMapper()
				: base(StringComparer.InvariantCultureIgnoreCase)
			{
			}
		}

		private ConcurrentDictionary<Guid, InternalDatabaseMapper> _connectionDatabases =
			new ConcurrentDictionary<Guid, InternalDatabaseMapper>();

		public InternalDatabaseConnection GetOrCreateInternalConnection(
			ExternalDatabaseConnection connection, string dbName)
		{
			// Locate the internal database mapper for this connection
			InternalDatabaseMapper mapper = GetOrCreateInternalMapper(connection.ConnectionId);

			// Locate the connection for this database
			return mapper.GetOrAdd(
				dbName,
				(name) =>
					new InternalDatabaseConnection(name)
					{
						IsolationLevel = connection.IsolationLevel,
						LockTimeout = connection.LockTimeout
					});
		}

		public void Commit(Guid connectionId)
		{
			InternalDatabaseMapper mapper = GetOrCreateInternalMapper(connectionId);
			foreach (InternalDatabaseConnection connection in
				mapper.Values.ToArray().Where((item) => item.Transaction != null))
			{
				connection.Commit();
			}
		}

		public void Rollback(Guid connectionId)
		{
			InternalDatabaseMapper mapper = GetOrCreateInternalMapper(connectionId);
			foreach (InternalDatabaseConnection connection in
				mapper.Values.ToArray().Where((item) => item.Transaction != null))
			{
				connection.Rollback();
			}
		}

		private InternalDatabaseMapper GetOrCreateInternalMapper(
			Guid connectionId)
		{
			// Locate the internal database mapper for this connection
			return _connectionDatabases.GetOrAdd(
				connectionId, (id) => new InternalDatabaseMapper());
		}
	}
}
