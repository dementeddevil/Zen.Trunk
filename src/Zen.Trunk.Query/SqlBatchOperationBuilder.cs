﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Transactions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Zen.Trunk.Storage.Data;

namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// <c>SqlBatchOperationBuilder</c> builds an <see cref="Expression"/> that
    /// represents the Trunk-SQL passed to it.
    /// </summary>
    /// <remarks>
    /// The purpose of this class is to build a list of batches based on
    /// the AST that is walked.
    /// Each batch will create a transaction of an appropriate isolation level
    /// and execute a series of asynchronous blocks.
    /// Each block is an asynchronous operation or composite (where BEGIN/END is used).
    /// </remarks>
    public class SqlBatchOperationBuilder : TrunkSqlBaseVisitor<Expression>
    {
        private SymbolDocumentInfo _documentInfo = Expression.SymbolDocument("blank.sql");

        private int _transactionDepth;
        private readonly List<Expression<Func<QueryExecutionContext, Task>>> _operations =
            new List<Expression<Func<QueryExecutionContext, Task>>>();

        private readonly ParameterExpression _executionContextParameterExpression =
            Expression.Parameter(typeof(QueryExecutionContext), "executionContext");

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
        protected override Expression DefaultResult => null;

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
        public override Expression VisitUse_statement(TrunkSqlParser.Use_statementContext context)
        {
            var sourceLine = context.Start.Line;

            // TODO: We cannot be inside an active transaction and we must obtain a shared lock
            //  on the desired database - looks like setting the active database needs a method
            //  on the MasterDatabaseDevice object.
            var dbNameExpr = VisitId(context.database);
            var dbDeviceExpr = Expression.Variable(typeof(DatabaseDevice), "dbDevice");
            return Expression.Block(
                GetDebugInfoFromContext(context),
                dbDeviceExpr,
                Expression.Assign(
                    dbDeviceExpr,
                    Expression.Call(
                        GetQueryExecutionContextMasterDatabaseExpression(),
                        nameof(MasterDatabaseDevice.GetDatabaseDevice),
                        new[] { typeof(string) },
                        dbNameExpr)),
                Expression.IfThen(
                    Expression.Equal(dbDeviceExpr, Expression.Constant(null)),
                    Expression.Throw(Expression.Constant(new Exception($"Unknown database at line {sourceLine}.")))),
                Expression.Assign(
                    GetQueryExecutionContextActiveDatabaseExpression(),
                    dbDeviceExpr));
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
        public override Expression VisitBegin_transaction_statement([NotNull] TrunkSqlParser.Begin_transaction_statementContext context)
        {
            return Expression.Block(
                GetDebugInfoFromContext(context),
                Expression.Call(
                    GetQueryExecutionContextActiveDatabaseExpression(),
                    // ReSharper disable once AssignNullToNotNullAttribute
                    typeof(DatabaseDevice).GetMethod(nameof(DatabaseDevice.BeginTransaction)),
                    GetQueryExecutionContextIsolationLevelExpression()));
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
        public override Expression VisitCommit_transaction_statement([NotNull] TrunkSqlParser.Commit_transaction_statementContext context)
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
        public override Expression VisitSet_transaction_isolation_level_statement([NotNull] TrunkSqlParser.Set_transaction_isolation_level_statementContext context)
        {
            var level = IsolationLevel.ReadCommitted;
            if (context.GetChild(4) == context.SNAPSHOT())
            {
                level = IsolationLevel.Snapshot;
            }
            else if (context.GetChild(4) == context.SERIALIZABLE())
            {
                level = IsolationLevel.Serializable;
            }
            else if (context.ChildCount > 5)
            {
                if (context.GetChild(4) == context.READ() &&
                    context.GetChild(5) == context.UNCOMMITTED())
                {
                    level = IsolationLevel.ReadUncommitted;
                }
                else if (context.GetChild(4) == context.READ() &&
                    context.GetChild(5) == context.COMMITTED())
                {
                    level = IsolationLevel.ReadCommitted;
                }
                else if (context.GetChild(4) == context.REPEATABLE() &&
                    context.GetChild(5) == context.READ())
                {
                    level = IsolationLevel.RepeatableRead;
                }
            }

            return Expression.Block(
                GetDebugInfoFromContext(context),
                Expression.Assign(
                    GetQueryExecutionContextIsolationLevelExpression(),
                    Expression.Constant(level)));
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
        public override Expression VisitCreate_database(TrunkSqlParser.Create_databaseContext context)
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

            // TODO: Statements need to return Expression that evaluates to
            //  Task FooBar(ExecutionContext ec) and our expression aggregator
            //  needs to chain each child using appropriate semantics for
            //  task chaining - or needs to add statements into task chain
            //  that we can do async/await processing via helper...
            return Expression.Block(
                new[]
                {
                    _executionContextParameterExpression
                },
                GetDebugInfoFromContext(context),
                Expression.Call(
                    GetQueryExecutionContextMasterDatabaseExpression(),
                    nameof(MasterDatabaseDevice.AttachDatabaseAsync),
                    null,
                    Expression.Constant(attachDatabaseParameters)));
        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.create_table" />.
        /// <para>
        /// The default implementation returns the result of calling <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)" />
        /// on <paramref name="context" />.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns></returns>
        /// <return>The visitor result.</return>
        public override Expression VisitCreate_table(TrunkSqlParser.Create_tableContext context)
        {
            //context.table_name().
            return Expression.Block(
                GetDebugInfoFromContext(context),
                base.VisitCreate_table(context));
        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.table_name" />.
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns>
        /// An <see cref="Expression"/> that creates a <see cref="TableIdentifier"/> based on supplied arguments.
        /// </returns>
        /// <return>The visitor result.</return>
        public override Expression VisitTable_name(TrunkSqlParser.Table_nameContext context)
        {
            return Expression.New(
                // ReSharper disable once AssignNullToNotNullAttribute
                typeof(TableIdentifier).GetConstructor(new[] { typeof(string), typeof(string), typeof(string) }),
                VisitId(context.database),
                VisitId(context.schema),
                VisitId(context.table));
        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.id" />.
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns>
        /// A <see cref="Expression"/> representing the constant for the identifier with delimiters removed.
        /// </returns>
        /// <return>The visitor result.</return>
        public override Expression VisitId(TrunkSqlParser.IdContext context)
        {
            var nativeId = GetNativeId(context.GetText());
            return Expression.Constant(nativeId);
        }

        /// <summary>
        /// Aggregates the result.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="childToAdd">The child to add.</param>
        /// <returns></returns>
        protected override Expression AggregateResult(
            Expression aggregate, Expression childToAdd)
        {
            if (aggregate == null)
            {
                aggregate = Expression.Block();
            }

            if (childToAdd == null)
            {
                return aggregate;
            }

            var blockAggregate = (BlockExpression)aggregate;
            return blockAggregate.Update(
                blockAggregate.Variables,
                blockAggregate.Expressions.Concat(
                    new[]
                    {
                        GetQueryExecutionContextThrowIfCancelledExpression(),
                        childToAdd
                    }));
        }

        private MethodCallExpression GetQueryExecutionContextThrowIfCancelledExpression()
        {
            return Expression.Call(
                _executionContextParameterExpression,
                // ReSharper disable once AssignNullToNotNullAttribute
                typeof(QueryExecutionContext).GetMethod(
                    nameof(QueryExecutionContext.ThrowIfCancellationRequested)));
        }

        private MemberExpression GetQueryExecutionContextMasterDatabaseExpression()
        {
            return Expression.Property(
                _executionContextParameterExpression,
                typeof(QueryExecutionContext),
                nameof(QueryExecutionContext.MasterDatabase));
        }

        private MemberExpression GetQueryExecutionContextActiveDatabaseExpression()
        {
            return Expression.Property(
                _executionContextParameterExpression,
                typeof(QueryExecutionContext),
                nameof(QueryExecutionContext.ActiveDatabase));
        }

        private MemberExpression GetQueryExecutionContextIsolationLevelExpression()
        {
            return Expression.Property(
                _executionContextParameterExpression,
                typeof(QueryExecutionContext),
                nameof(QueryExecutionContext.IsolationLevel));
        }

        //private async Task ExecuteCompositeExpressionAsync(
        //    QueryExecutionContext queryExecutionContext,
        //    Expression<Func<QueryExecutionContext, Task>> lhs,
        //    Expression<Func<QueryExecutionContext, Task>> rhs)
        //{
        //    var compiledLeft = lhs.Compile();
        //    var compiledRight = rhs.Compile();
        //    await compiledLeft(queryExecutionContext).ConfigureAwait(false);
        //    await compiledRight(queryExecutionContext).ConfigureAwait(false);
        //}

        private FileSpec GetNativeFileSpecFromFileSpec(TrunkSqlParser.File_specContext fileSpecContext)
        {
            var nativeFileSpec =
                new FileSpec
                {
                    Name = GetNativeId(fileSpecContext.id().GetText()),
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
            // ReSharper disable once InvertIf
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

            return double.TryParse(sizeText, out var value) ? new FileSize(value, unit) : new FileSize(0.0, unit);
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

        private string GetNativeId(string text)
        {
            if ((text.StartsWith("\"") && text.EndsWith("\"")) ||
                (text.StartsWith("[") && text.EndsWith("]")))
            {
                return text.Substring(1, text.Length - 2);
            }

            return text;
        }

        private DebugInfoExpression GetDebugInfoFromContext(ParserRuleContext context)
        {
            return Expression.DebugInfo(
                _documentInfo,
                context.Start.Line,
                Math.Max(1, context.Start.Column),
                context.Stop.Line,
                Math.Max(1, context.Stop.Column));
        }
    }
}
