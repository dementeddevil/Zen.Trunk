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

        public IEnumerable<BatchedCompoundOperation> Batches { get; private set; }

        public async Task Execute(string statementBatch, bool onlyPrepare = false)
        {
            // Tokenise the input character stream
            var charStream = new AntlrInputStream(statementBatch);
            var lexer = new TrunkSqlLexer(charStream);

            // Build AST from the token stream
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new TrunkSqlParser(tokenStream);
            var compileUnit = parser.tsql_file();

            // Build query batch pipeline from the AST
            var visitor = new SqlBatchOperationBuilder(_masterDevice);
            Batches = compileUnit.Accept(visitor);

            if(onlyPrepare)
            {
                return;
            }

            // Walk the batches and execute each one
            foreach(var batch in Batches)
            {
                await batch.ExecuteAsync().ConfigureAwait(false);
            }
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
    public class SqlBatchOperationBuilder : TrunkSqlBaseVisitor<IList<BatchedCompoundOperation>>
    {
        private readonly MasterDatabaseDevice _masterDatabase;
        private readonly List<BatchedCompoundOperation> _batches =
            new List<BatchedCompoundOperation>();

        private BatchedCompoundOperation _currentBatch;
        private DatabaseDevice _activeDatabase;

        public SqlBatchOperationBuilder(MasterDatabaseDevice masterDatabase)
        {
            _masterDatabase = masterDatabase;
        }

        public IEnumerable<BatchedCompoundOperation> Batches => _batches;

        public override IList<BatchedCompoundOperation> VisitBatch(TrunkSqlParser.BatchContext context)
        {
            CreateNewBatch();

            base.VisitBatch(context);

            CommitCurrentBatch();
            return _batches;
        }

        public override IList<BatchedCompoundOperation> VisitUse_statement(TrunkSqlParser.Use_statementContext context)
        {
            var dbName = context.database.ToString();
            var dbDevice = _masterDatabase.GetDatabaseDevice(dbName);
            if (dbDevice == null)
            {
                throw new Exception($"Unknown database ({dbName})");
            }

            _activeDatabase = dbDevice;
            _currentBatch.PushDatabaseSwitch(dbName);
            return base.VisitUse_statement(context);
        }

        public override IList<BatchedCompoundOperation> VisitSet_special(TrunkSqlParser.Set_specialContext context)
        {
            if (context.ChildCount > 4 &&
                context.GetChild(0) == context.SET() &&
                context.GetChild(1) == context.TRANSACTION() &&
                context.GetChild(2) == context.ISOLATION() &&
                context.GetChild(3) == context.LEVEL())
            {
                IsolationLevel level = IsolationLevel.ReadCommitted;
                if (context.GetChild(4) == context.READ() &&
                    context.GetChild(5) == context.UNCOMMITTED())
                {
                    level = IsolationLevel.ReadUncommitted;
                }
                if (context.GetChild(4) == context.READ() &&
                    context.GetChild(5) == context.COMMITTED())
                {
                    level = IsolationLevel.ReadCommitted;
                }
                if (context.GetChild(4) == context.REPEATABLE() &&
                    context.GetChild(5) == context.READ())
                {
                    level = IsolationLevel.RepeatableRead;
                }
                if (context.GetChild(4) == context.SNAPSHOT())
                {
                    level = IsolationLevel.Snapshot;
                }
                if (context.GetChild(4) == context.SERIALIZABLE())
                {
                    level = IsolationLevel.Serializable;
                }

                // TODO: Determine whether this demands a new batch
                _currentBatch.SetTransactionIsolationLevel(level);
            }
            return base.VisitSet_special(context);
        }

        private void CreateNewBatch()
        {
            CommitCurrentBatch();

            _currentBatch = new BatchedCompoundOperation(_masterDatabase, _activeDatabase);
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
        private IsolationLevel _isolationLevel = IsolationLevel.ReadCommitted;

        public BatchedCompoundOperation(
            MasterDatabaseDevice masterDatabase,
            DatabaseDevice activeDatabase = null)
            : base(masterDatabase, activeDatabase)
        {
        }

        public IsolationLevel TransactionIsolationLevel => _isolationLevel;

        protected override Task PreExecuteAsync()
        {
            TrunkTransactionContext.BeginTransaction(
                MasterDatabase.LifetimeScope,
                _isolationLevel,
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

        public void SetTransactionIsolationLevel(IsolationLevel level)
        {
            _isolationLevel = level;
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
