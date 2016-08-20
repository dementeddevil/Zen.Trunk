namespace Zen.Trunk.Storage.Query
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using System.Transactions;
	using Zen.Trunk.Storage.Data;
	using Zen.Trunk.Storage.Data.Table;
	using Zen.Trunk.Storage.Locking;

	partial class TrunkSQLPipeline
	{
		private class Batch
		{
			private TrunkSQLPipeline _pipeline;
			private List<Func<Task>> _statements = new List<Func<Task>>();

			public Batch(TrunkSQLPipeline pipeline)
			{
				_pipeline = pipeline;
				IsolationLevel = IsolationLevel.ReadCommitted;
			}

			public bool RequiresTransaction
			{
				get;
				set;
			}

			public IsolationLevel IsolationLevel
			{
				get;
				set;
			}

			public void AddStatement(Func<Task> request)
			{
				_statements.Add(request);
			}

			public async Task Execute()
			{
				// TODO: Remove this code when we have command support for 
				//	beginning transactions and performing commit/rollback
				// In addition we need to be able to locate instructions that
				//	change the isolation level for a transaction
				//	(this will probably be best implemented as a property on
				//	the batch object)
				bool enteredTransaction = false;
				if (RequiresTransaction)
				{
					TrunkTransactionContext.BeginTransaction(
						_pipeline.MasterDatabase,
						new TransactionOptions
						{
							IsolationLevel = IsolationLevel
						});
					RequiresTransaction = false;
					enteredTransaction = true;
				}

				Exception exceptionToThrow = null;
				try
				{
					foreach (var statement in _statements)
					{
						await statement().ConfigureAwait(false);
					}
				}
				catch (Exception exception)
				{
					exceptionToThrow = exception;
				}

				if (enteredTransaction)
				{
					if (exceptionToThrow != null)
					{
						await TrunkTransactionContext.Rollback();
						throw exceptionToThrow;
					}
					else
					{
						await TrunkTransactionContext.Commit();
					}
				}
			}
		}

		private Batch _currentBatch;
		private bool _isDDLBatch;

		public MasterDatabaseDevice MasterDatabase
		{
			get;
			set;
		}

		public DatabaseDevice CurrentDatabase
		{
			get;
			set;
		}

		public Task QueueDMLTask(object taskParameters)
		{
			return QueueDMLTask(taskParameters, true);
		}

		public async Task QueueDMLTask(object taskParameters, bool requiresTransaction)
		{
			await EnsureBatch(false);

			if (CurrentDatabase != null)
			{
				var deferredTask = GetStatementFromParameters(CurrentDatabase, MasterDatabase, taskParameters);
				_currentBatch.AddStatement(deferredTask);
			}
			else
			{
				var deferredTask = GetStatementFromParameters(MasterDatabase, MasterDatabase, taskParameters);
				_currentBatch.AddStatement(deferredTask);
			}

			if (requiresTransaction)
			{
				_currentBatch.RequiresTransaction = true;
			}
		}

		public Task QueueDDLTask(object taskParameters)
		{
			return QueueDDLTask(taskParameters, true);
		}

		public async Task QueueDDLTask(object taskParameters, bool requiresTransaction)
		{
			await EnsureBatch(true);

			var deferredTask = GetStatementFromParameters(MasterDatabase, MasterDatabase, taskParameters);
			_currentBatch.AddStatement(deferredTask);

			if (requiresTransaction)
			{
				_currentBatch.RequiresTransaction = true;
			}
		}

		public async Task ExecuteBatch()
		{
			if (_currentBatch != null)
			{
				// Create transaction
				await _currentBatch.Execute();
				_currentBatch = null;
			}
		}

		private static Func<Task> GetStatementFromParameters(DatabaseDevice currentDatabase, MasterDatabaseDevice masterDatabase, object taskParameters)
		{
			var statement = GetStatementFromType(taskParameters.GetType());
			return () => statement(currentDatabase, masterDatabase, taskParameters);
		}

		private static Func<DatabaseDevice, MasterDatabaseDevice, object, Task> GetStatementFromType(Type paramType)
		{
			var types = new Dictionary<Type, Func<DatabaseDevice, MasterDatabaseDevice, object, Task>>
				{
					{
						typeof(AddFileGroupDeviceParameters),
						(currentDatabase, masterDatabase, taskParams) => currentDatabase.AddFileGroupDevice((AddFileGroupDeviceParameters)taskParams)
					},
					{
						typeof(RemoveFileGroupDeviceParameters),
						(currentDatabase, masterDatabase, taskParams) => currentDatabase.RemoveFileGroupDevice((RemoveFileGroupDeviceParameters)taskParams)
					},
					{
						typeof(AddFileGroupTableParameters),
						(currentDatabase, masterDatabase, taskParams) => currentDatabase.AddFileGroupTable((AddFileGroupTableParameters)taskParams)
					},
					{
						typeof(AttachDatabaseParameters),
						(currentDatabase, masterDatabase, taskParams) => masterDatabase.AttachDatabase((AttachDatabaseParameters)taskParams)
					},
					{
						typeof(ChangeDatabaseStatusParameters),
						(currentDatabase, masterDatabase, taskParams) => masterDatabase.ChangeDatabaseStatus((ChangeDatabaseStatusParameters)taskParams)
					},
				};

			Func<DatabaseDevice, MasterDatabaseDevice, object, Task> result;
			if (!types.TryGetValue(paramType, out result))
			{
				throw new ArgumentException("Unrecognised task parameter object.");
			}
			return result;
		}

		private async Task ExecuteBatchesSequentially(IEnumerable<Task> tasks)
		{
			foreach (Task task in tasks)
			{
				await task;
			}
		}

		private string StripStringWrapper(string text)
		{
			if (text.StartsWith("'") && text.EndsWith("'"))
			{
				if (text.Length > 2)
				{
					text = text.Substring(1, text.Length - 2);
				}
				else
				{
					text = string.Empty;
				}
			}
			return text;
		}

		private TableColumnDataType ConvertToTableColumnType(string typeName)
		{
			TableColumnDataType type;
			if (!Enum.TryParse<TableColumnDataType>(typeName, true, out type))
			{
				throw new ArgumentException("Unknown table column type encountered.");
			}
			if (type == TableColumnDataType.None)
			{
				throw new ArgumentException("Illegal table column type encountered.");
			}
			return type;
		}

		private async Task EnsureBatch(bool isDDLBatch)
		{
			if (_currentBatch != null && _isDDLBatch != isDDLBatch)
			{
				await ExecuteBatch();
			}

			if (_currentBatch == null)
			{
				_currentBatch = new Batch(this);
				_isDDLBatch = isDDLBatch;
			}
		}
	}
}