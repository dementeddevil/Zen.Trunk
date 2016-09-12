using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Antlr4.Runtime.Misc;
using Zen.Trunk.Storage.Data;

namespace Zen.Trunk.Storage.Query
{
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
    public class SqlBatchOperationBuilder : TrunkSqlBaseVisitor<Expression<Func<ExecutionContext, Task>>>
    {
        private readonly MasterDatabaseDevice _masterDatabase;
        private DatabaseDevice _activeDatabase;

        private IsolationLevel _currentIsolationLevel;
        private int _transactionDepth;
        private readonly List<Expression<Func<ExecutionContext, Task>>> _operations =
            new List<Expression<Func<ExecutionContext, Task>>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlBatchOperationBuilder"/> class.
        /// </summary>
        /// <param name="masterDatabase">The master database.</param>
        public SqlBatchOperationBuilder(MasterDatabaseDevice masterDatabase)
        {
            _masterDatabase = masterDatabase;
            _activeDatabase = masterDatabase;
        }

        /// <summary>
        /// Gets the default value returned by visitor methods.
        /// </summary>
        /// <remarks>
        /// Gets the default value returned by visitor methods. This value is
        /// returned by the default implementations of
        /// <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitTerminal(Antlr4.Runtime.Tree.ITerminalNode)">visitTerminal</see>
        /// ,
        /// <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitErrorNode(Antlr4.Runtime.Tree.IErrorNode)">visitErrorNode</see>
        /// .
        /// The default implementation of
        /// <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)">visitChildren</see>
        /// initializes its aggregate result to this value.
        /// <p>The base implementation returns
        /// <see langword="null" />
        /// .</p>
        /// </remarks>
        protected override Expression<Func<ExecutionContext, Task>> DefaultResult => null;

        /// <summary>
        /// Visit a parse tree produced by <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.tsql_file" />.
        /// <para>
        /// The default implementation returns the result of calling <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)" />
        /// on <paramref name="context" />.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns></returns>
        /// <return>The visitor result.</return>
        public override Expression<Func<ExecutionContext, Task>> VisitTsql_file([NotNull] TrunkSqlParser.Tsql_fileContext context)
        {
            return base.VisitTsql_file(context);
        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.use_statement" />.
        /// <para>
        /// The default implementation returns the result of calling <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)" />
        /// on <paramref name="context" />.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns></returns>
        /// <return>The visitor result.</return>
        public override Expression<Func<ExecutionContext, Task>> VisitUse_statement(TrunkSqlParser.Use_statementContext context)
        {
            var dbName = context.database.ToString();
            return ec => ExecuteUseDatabase(ec, dbName);
        }

        /// <summary>
        /// Visit a parse tree produced by the <c>begin_transaction_statement</c>
        /// labeled alternative in <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.transaction_statement" />.
        /// <para>
        /// The default implementation returns the result of calling <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)" />
        /// on <paramref name="context" />.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns></returns>
        /// <return>The visitor result.</return>
        public override Expression<Func<ExecutionContext, Task>> VisitBegin_transaction_statement([NotNull] TrunkSqlParser.Begin_transaction_statementContext context)
        {
            return base.VisitBegin_transaction_statement(context);
        }

        /// <summary>
        /// Visit a parse tree produced by the <c>commit_transaction_statement</c>
        /// labeled alternative in <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.transaction_statement" />.
        /// <para>
        /// The default implementation returns the result of calling <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)" />
        /// on <paramref name="context" />.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns></returns>
        /// <return>The visitor result.</return>
        public override Expression<Func<ExecutionContext, Task>> VisitCommit_transaction_statement([NotNull] TrunkSqlParser.Commit_transaction_statementContext context)
        {
            return base.VisitCommit_transaction_statement(context);
        }

        /// <summary>
        /// Visit a parse tree produced by the <c>set_transaction_isolation_level_statement</c>
        /// labeled alternative in <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.set_special" />.
        /// <para>
        /// The default implementation returns the result of calling <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)" />
        /// on <paramref name="context" />.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns></returns>
        /// <return>The visitor result.</return>
        public override Expression<Func<ExecutionContext, Task>> VisitSet_transaction_isolation_level_statement([NotNull] TrunkSqlParser.Set_transaction_isolation_level_statementContext context)
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

        /// <summary>
        /// Visit a parse tree produced by <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.create_database" />.
        /// <para>
        /// The default implementation returns the result of calling <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)" />
        /// on <paramref name="context" />.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns></returns>
        /// <return>The visitor result.</return>
        public override Expression<Func<ExecutionContext, Task>> VisitCreate_database(TrunkSqlParser.Create_databaseContext context)
        {
            var attachDatabaseParameters = new AttachDatabaseParameters(context.database.GetText(), true);
            var fileSpecCount = context.database_file_spec().Length;
            var rawDatabaseFileSpec = fileSpecCount == 0 ? null : context.database_file_spec(0);
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
                    fileGroupName = StorageConstants.PrimaryFileGroupName;
                }
                if (rawDatabaseFileSpec != null && token == rawDatabaseFileSpec)
                {
                    var rawFileGroupSpec = rawDatabaseFileSpec.file_group();
                    var rawFileSpec = rawDatabaseFileSpec.file_spec();
                    if (!isLogFileSpec)
                    {
                        if (rawFileGroupSpec != null)
                        {
                            fileGroupName = rawFileGroupSpec.id().GetText();
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

            return (ec) => ec.MasterDatabase.AttachDatabaseAsync(attachDatabaseParameters);
        }

        /// <summary>
        /// Aggregates the result.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="childToAdd">The child to add.</param>
        /// <returns></returns>
        protected override Expression<Func<ExecutionContext, Task>> AggregateResult(
            Expression<Func<ExecutionContext, Task>> aggregate,
            Expression<Func<ExecutionContext, Task>> childToAdd)
        {
            if (aggregate == null)
            {
                return childToAdd;
            }

            if (childToAdd == null)
            {
                return aggregate;
            }

            // TODO: Replace this with better expression reduction logic
            return ec => ExecuteCompositeExpressionAsync(ec, aggregate, childToAdd);
        }

        private async Task ExecuteCompositeExpressionAsync(
            ExecutionContext executionContext,
            Expression<Func<ExecutionContext, Task>> lhs,
            Expression<Func<ExecutionContext, Task>> rhs)
        {
            var compiledLeft = lhs.Compile();
            var compiledRight = rhs.Compile();
            await compiledLeft(executionContext).ConfigureAwait(false);
            await compiledRight(executionContext).ConfigureAwait(false);
        }

        private FileSpec GetNativeFileSpecFromFileSpec(TrunkSqlParser.File_specContext fileSpecContext)
        {
            var nativeFileSpec =
                new FileSpec
                {
                    Name = GetNativeString(fileSpecContext.id().GetText()),
                    FileName = GetNativeString(fileSpecContext.file.Text),
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

        private string GetNativeString(string text)
        {
            bool isUnicode = false;
            if (text.StartsWith("N'", StringComparison.OrdinalIgnoreCase))
            {
                isUnicode = true;
                text = text.Substring(1);
            }
            if (text.StartsWith("'"))
            {
                text = text.Substring(1);
            }
            if (text.EndsWith("'"))
            {
                text = text.Substring(0, text.Length - 1);
            }

            // Now we need to convert text into unicode
            if (!isUnicode)
            {
                // TODO: Use code page to determine how to treat string
            }

            return text;
        }
    }
}