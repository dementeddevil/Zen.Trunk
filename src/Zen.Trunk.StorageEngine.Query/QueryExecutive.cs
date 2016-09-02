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
using System.Linq.Expressions;

namespace Zen.Trunk.Storage.Query
{
    public class QueryExecutive
    {
        private readonly MasterDatabaseDevice _masterDevice;

        public QueryExecutive(MasterDatabaseDevice masterDevice)
        {
            _masterDevice = masterDevice;
        }

        public CompoundFile File { get; private set; }

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
            File = (CompoundFile)compileUnit.Accept(visitor);

            if (onlyPrepare)
            {
                return;
            }

            // Walk the batches and execute each one
            await File.ExecuteAsync().ConfigureAwait(false);
        }
    }

    public class ExecutionContext
    {
        public ExecutionContext(MasterDatabaseDevice masterDatabase, DatabaseDevice activeDatabase = null)
        {
            MasterDatabase = masterDatabase;
            ActiveDatabase = activeDatabase ?? masterDatabase;
        }

        public MasterDatabaseDevice MasterDatabase { get; }

        public DatabaseDevice ActiveDatabase { get; set; }
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
    public class SqlBatchOperationBuilder : TrunkSqlBaseVisitor<CompoundOperation>
    {
        private readonly MasterDatabaseDevice _masterDatabase;
        private CompoundFile _file;
        private BatchedCompoundOperation _currentBatch;
        private CompoundOperation _currentOperation;
        private DatabaseDevice _activeDatabase;

        private IsolationLevel _currentIsolationLevel;
        private int _transactionDepth;
        private readonly List<Expression<Func<ExecutionContext, Task>>> _operations =
            new List<Expression<Func<ExecutionContext, Task>>>();

        public SqlBatchOperationBuilder(MasterDatabaseDevice masterDatabase)
        {
            _masterDatabase = masterDatabase;
        }

        public CompoundFile File => _file;

        public override CompoundOperation VisitTsql_file([NotNull] TrunkSqlParser.Tsql_fileContext context)
        {
            base.VisitTsql_file(context);
            return _file;
        }

        public override CompoundOperation VisitUse_statement(TrunkSqlParser.Use_statementContext context)
        {
            var dbName = context.database.ToString();
            _operations.Add((ec) => ExecuteUseDatabase(ec, dbName));
            return base.VisitUse_statement(context);
        }

        public override CompoundOperation VisitBegin_transaction_statement([NotNull] TrunkSqlParser.Begin_transaction_statementContext context)
        {
            return base.VisitBegin_transaction_statement(context);
        }

        public override CompoundOperation VisitCommit_transaction_statement([NotNull] TrunkSqlParser.Commit_transaction_statementContext context)
        {
            return base.VisitCommit_transaction_statement(context);
        }

        public override CompoundOperation VisitSet_transaction_isolation_level_statement([NotNull] TrunkSqlParser.Set_transaction_isolation_level_statementContext context)
        {
            /*if (context.ChildCount > 4 &&
                context.GetChild(0) == context.SET() &&
                context.GetChild(1) == context.TRANSACTION() &&
                context.GetChild(2) == context.ISOLATION() &&
                context.GetChild(3) == context.LEVEL())*/

            IsolationLevel level = IsolationLevel.ReadCommitted;
            if (context.GetChild(4) == context.SNAPSHOT())
            {
                level = IsolationLevel.Snapshot;
            }
            else if (context.GetChild(4) == context.SERIALIZABLE())
            {
                level = IsolationLevel.Serializable;
            }
            if (context.ChildCount > 5)
            {
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
            }

            _currentIsolationLevel = level;
            return base.VisitSet_transaction_isolation_level_statement(context);
        }

        public override CompoundOperation VisitCreate_database(TrunkSqlParser.Create_databaseContext context)
        {
            var attachDatabaseParameters = new AttachDatabaseParameters();
            attachDatabaseParameters.Name = context.database.ToString();
            var fileSpecCount = context.database_file_spec().Length;
            var rawDatabaseFileSpec = context.database_file_spec(0);
            var fileSpecIndex = 0;
            var isLogFileSpec = false;
            var fileGroupName = string.Empty;
            for (int index = 0; index < context.ChildCount; ++index)
            {
                var token = context.GetChild(index);
                if (token == context.LOG())
                {
                    isLogFileSpec = true;
                }
                if (token == context.PRIMARY())
                {
                    fileGroupName = "PRIMARY";
                }
                if (token == rawDatabaseFileSpec)
                {
                    var rawFileGroupSpec = rawDatabaseFileSpec.file_group();
                    var rawFileSpec = rawDatabaseFileSpec.file_spec();
                    if (!isLogFileSpec)
                    {
                        if (rawFileGroupSpec != null)
                        {
                            fileGroupName = rawFileGroupSpec.id().ToString();
                            foreach (var rfs in rawFileGroupSpec.file_spec())
                            {
                                var nativeFileSpec = GetNativeFileSpecFromFileSpec(rfs);
                                attachDatabaseParameters.AddDataFile(fileGroupName, nativeFileSpec);
                            }
                        }
                        else if (rawFileSpec != null)
                        {
                            var nativeFileSpec = GetNativeFileSpecFromFileSpec(rawFileSpec);
                            attachDatabaseParameters.AddDataFile(fileGroupName, nativeFileSpec);
                        }
                    }
                    else
                    {
                        if (rawFileGroupSpec != null)
                        {
                            foreach (var rfs in rawFileGroupSpec.file_spec())
                            {
                                var nativeFileSpec = GetNativeFileSpecFromFileSpec(rfs);
                                attachDatabaseParameters.AddLogFile(nativeFileSpec);
                            }
                        }
                        else if (rawFileSpec != null)
                        {
                            var nativeFileSpec = GetNativeFileSpecFromFileSpec(rawFileSpec);
                            attachDatabaseParameters.AddLogFile(nativeFileSpec);
                        }
                    }

                    // Process file specification and add to parameters
                    if (++fileSpecIndex < fileSpecCount)
                    {
                        rawDatabaseFileSpec = context.database_file_spec(fileSpecIndex);
                    }
                    else
                    {
                        rawDatabaseFileSpec = null;
                    }
                }
            }

            _operations.Add((ec) => ec.MasterDatabase.AttachDatabase(attachDatabaseParameters));
            return _currentBatch;
        }

        private FileSpec GetNativeFileSpecFromFileSpec(TrunkSqlParser.File_specContext fileSpecContext)
        {
            var nativeFileSpec =
                new FileSpec
                {
                    Name = fileSpecContext.id().ToString(),
                    FileName = fileSpecContext.file.Text,
                };

            for (int index = 0; index < fileSpecContext.ChildCount; ++index)
            {
                var child = fileSpecContext.GetChild(index);
                if (child == fileSpecContext.SIZE())
                {
                    nativeFileSpec.Size = GetNativeSizeFromFileSize(
                        (TrunkSqlParser.File_sizeContext)fileSpecContext.GetChild(index + 2));
                }
                if (child == fileSpecContext.MAXSIZE())
                {
                    if (fileSpecContext.GetChild(index + 2) == fileSpecContext.UNLIMITED())
                    {
                        nativeFileSpec.MaxSize = FileSize.Unlimited;
                    }
                    else
                    {
                        nativeFileSpec.MaxSize = GetNativeSizeFromFileSize(
                            (TrunkSqlParser.File_sizeContext)fileSpecContext.GetChild(index + 2));
                    }
                }
                if (child == fileSpecContext.FILEGROWTH())
                {
                    nativeFileSpec.FileGrowth = GetNativeSizeFromFileSize(
                        (TrunkSqlParser.File_sizeContext)fileSpecContext.GetChild(index + 2));
                }
            }

            return nativeFileSpec;
        }

        private FileSize GetNativeSizeFromFileSize(TrunkSqlParser.File_sizeContext fileSizeContext)
        {
            // Get the 
            var sizeText = fileSizeContext.GetChild(0).GetText();
            var unit = FileSize.FileSizeUnit.MegaBytes;
            if (fileSizeContext.ChildCount > 1)
            {
                var unitToken = fileSizeContext.GetChild(1);
                if (unitToken == fileSizeContext.KB())
                {
                    unit = FileSize.FileSizeUnit.KiloBytes;
                }
                else if (unitToken == fileSizeContext.MB())
                {
                    unit = FileSize.FileSizeUnit.MegaBytes;
                }
                else if (unitToken == fileSizeContext.GB())
                {
                    unit = FileSize.FileSizeUnit.GigaBytes;
                }
                else if (unitToken == fileSizeContext.TB())
                {
                    unit = FileSize.FileSizeUnit.TeraBytes;
                }
                else if (unitToken == fileSizeContext.MODULE())
                {
                    unit = FileSize.FileSizeUnit.Percentage;
                }
            }

            double value;
            if (double.TryParse(sizeText, out value))
            {
                return new FileSize(value, unit);
            }

            return new FileSize(0.0, unit);
        }

        private Task ExecuteUseDatabase(ExecutionContext context, string dbName)
        {
            var dbDevice = context.MasterDatabase.GetDatabaseDevice(dbName);
            if (dbDevice == null)
            {
                throw new Exception($"Unknown database ({dbName})");
            }
            context.ActiveDatabase = dbDevice;
            return Task.FromResult(true);
        }
    }

    public class CompoundFile : CompoundOperation
    {
        private readonly IList<BatchedCompoundOperation> _batches =
            new List<BatchedCompoundOperation>();

        public CompoundFile(MasterDatabaseDevice masterDatabase, DatabaseDevice activeDatabase = null)
            : base(masterDatabase, activeDatabase)
        {
        }

        public void Add(BatchedCompoundOperation batchedOperation)
        {
            _batches.Add(batchedOperation);
        }

        protected override async Task ExecuteCoreAsync()
        {
            foreach (var batch in _batches)
            {
                await batch.ExecuteAsync().ConfigureAwait(false);
            }
        }
    }

    public class BatchedCompoundOperation : CompoundOperation
    {
        private IsolationLevel _isolationLevel = IsolationLevel.ReadCommitted;
        private BatchedCompoundOperation _parentBatch;

        public BatchedCompoundOperation(
            MasterDatabaseDevice masterDatabase,
            DatabaseDevice activeDatabase = null)
            : base(masterDatabase, activeDatabase)
        {
        }

        private BatchedCompoundOperation(
            BatchedCompoundOperation parentBatch,
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

        public BatchedCompoundOperation PushNestedBatch()
        {
            var childBatch = new BatchedCompoundOperation(this, MasterDatabase, ActiveDatabase);
            childBatch.SetTransactionIsolationLevel(_isolationLevel);
            return childBatch;
        }

        public BatchedCompoundOperation PopNestedBatch()
        {
            if (_parentBatch == null)
            {
                throw new InvalidOperationException("Not in a transaction");
            }

            return _parentBatch;
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

        public DatabaseDevice ActiveDatabase => _activeDatabase;

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

        public void PushAttachDatabase(AttachDatabaseParameters attachParameters)
        {
            _operations.Add(() => _masterDatabase.AttachDatabase(attachParameters));
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
