using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Autofac;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Query
{
	public class QueryExecutive
	{
		private readonly MasterDatabaseDevice _masterDevice;

		public QueryExecutive(MasterDatabaseDevice masterDevice)
		{
			_masterDevice = masterDevice;
		}

		public Task Execute(string statementBatch)
		{
            // Tokenise the input character stream
			var charStream = new AntlrInputStream(statementBatch);
			var lexer = new TrunkSqlLexer(charStream);

			// Build AST from the token stream
			var tokenStream = new CommonTokenStream(lexer);
			var parser = new TrunkSqlParser(tokenStream);
			var compileUnit = parser.tsql_file();

			// Build query batch pipeline from the AST
		    var listener = new QueryTreeListener(_masterDevice);
		    listener.EnterTsql_file(compileUnit);

		    return Task.FromResult(true);
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// The purpose of this class is to build a list of batches based on
    /// the AST that is walked.
    /// Each batch will create a transaction of an appropriate isolation level
    /// and execute a series of asynchronous blocks.
    /// Each block is an asynchronous operation or composite (where BEGIN/END is used).
    /// </remarks>
    public class QueryTreeListener : TrunkSqlBaseListener
    {
        private readonly MasterDatabaseDevice _masterDatabase;
        private readonly List<BatchedCompoundOperation> _batches =
            new List<BatchedCompoundOperation>();

        private BatchedCompoundOperation _currentBatch;

        public QueryTreeListener(MasterDatabaseDevice masterDatabase)
        {
            _masterDatabase = masterDatabase;
        }

        public override void EnterBatch([NotNull] TrunkSqlParser.BatchContext context)
        {
            CreateNewBatch();

            base.EnterBatch(context);
        }

        public override void ExitBatch([NotNull] TrunkSqlParser.BatchContext context)
        {
            base.ExitBatch(context);

            CommitCurrentBatch();
        }

        private void CreateNewBatch()
        {
            CommitCurrentBatch();

            _currentBatch = new BatchedCompoundOperation(_masterDatabase);
        }

        private void CommitCurrentBatch()
        {
            if (_currentBatch != null && !_currentBatch.IsEmpty)
            {
                _batches.Add(_currentBatch);
                _currentBatch = null;
            }
        }
    }

    public class BatchedCompoundOperation : CompoundOperation
    {
        public BatchedCompoundOperation(
            MasterDatabaseDevice masterDatabase,
            DatabaseDevice activeDatabase = null)
            : base(masterDatabase, activeDatabase)
        {
        }

        protected override Task PreExecuteAsync()
        {
            TrunkTransactionContext.BeginTransaction(
                MasterDatabase.LifetimeScope,
                IsolationLevel.ReadCommitted,
                TimeSpan.FromSeconds(30));

            return base.PreExecuteAsync();
        }

        protected override async Task PostExecuteAsync(bool commit)
        {
            await base.PostExecuteAsync(commit).ConfigureAwait(false);

            if (commit)
            {
                await TrunkTransactionContext.Commit().ConfigureAwait(false);
            }
            else
            {
                await TrunkTransactionContext.Rollback().ConfigureAwait(false);
            }
        }
    }

    public class CompoundOperation
    {
        private readonly IList<Func<Task>> _operations = new List<Func<Task>>();
        private readonly MasterDatabaseDevice _masterDatabase;
        private DatabaseDevice _activeDatabase;

        public CompoundOperation(
            MasterDatabaseDevice masterDatabase,
            DatabaseDevice activeDatabase = null)
        {
            _masterDatabase = masterDatabase;
            _activeDatabase = activeDatabase ?? _masterDatabase;
        }

        public MasterDatabaseDevice MasterDatabase => _masterDatabase;

        public bool IsEmpty => _operations.Count == 0;

        public virtual async Task ExecuteAsync()
        {
            await PreExecuteAsync().ConfigureAwait(false);

            bool okayToCommit = true;
            try
            {
                await ExecuteCoreAsync().ConfigureAwait(false);
            }
            catch
            {
                okayToCommit = false;
                throw;
            }
            finally
            {
                await PostExecuteAsync(okayToCommit).ConfigureAwait(false);
            }
        }

        public void PushDatabaseSwitch(string databaseName)
        {
            _operations.Add(
                () =>
                {
                    _activeDatabase = _masterDatabase.GetDatabaseDevice(databaseName);
                    return Task.FromResult(true);
                });
        }

        public void PushNestedOperation(CompoundOperation operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            _operations.Add(operation.ExecuteAsync);
        }

        protected virtual Task PreExecuteAsync()
        {
            return Task.FromResult(true);
        }

        protected virtual Task ExecuteCoreAsync()
        {
            return Task.FromResult(true);
        }

        protected virtual Task PostExecuteAsync(bool commit)
        {
            return Task.FromResult(true);
        }
    }
}
