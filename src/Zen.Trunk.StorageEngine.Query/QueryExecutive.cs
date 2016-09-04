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
using Zen.Trunk.Storage.Data.Table;

namespace Zen.Trunk.Storage.Query
{
    public class QueryExecutive
    {
        private readonly MasterDatabaseDevice _masterDevice;

        public QueryExecutive(MasterDatabaseDevice masterDevice)
        {
            _masterDevice = masterDevice;
        }

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
            var expression = compileUnit.Accept(visitor);

            if (onlyPrepare)
            {
                return;
            }

            // Walk the batches and execute each one
            var executionContext = new ExecutionContext(_masterDevice);
            await expression.Compile()(executionContext).ConfigureAwait(false);
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
    public class SqlBatchOperationBuilder : TrunkSqlBaseVisitor<Expression<Func<ExecutionContext, Task>>>
    {
        private readonly MasterDatabaseDevice _masterDatabase;
        private DatabaseDevice _activeDatabase;

        private IsolationLevel _currentIsolationLevel;
        private int _transactionDepth;
        private readonly List<Expression<Func<ExecutionContext, Task>>> _operations =
            new List<Expression<Func<ExecutionContext, Task>>>();

        public SqlBatchOperationBuilder(MasterDatabaseDevice masterDatabase)
        {
            _masterDatabase = masterDatabase;
        }

        protected override Expression<Func<ExecutionContext, Task>> DefaultResult => null;

        public override Expression<Func<ExecutionContext, Task>> VisitTsql_file([NotNull] TrunkSqlParser.Tsql_fileContext context)
        {
            return base.VisitTsql_file(context);
        }

        public override Expression<Func<ExecutionContext, Task>> VisitUse_statement(TrunkSqlParser.Use_statementContext context)
        {
            var dbName = context.database.ToString();
            return ec => ExecuteUseDatabase(ec, dbName);
        }

        public override Expression<Func<ExecutionContext, Task>> VisitBegin_transaction_statement([NotNull] TrunkSqlParser.Begin_transaction_statementContext context)
        {
            return base.VisitBegin_transaction_statement(context);
        }

        public override Expression<Func<ExecutionContext, Task>> VisitCommit_transaction_statement([NotNull] TrunkSqlParser.Commit_transaction_statementContext context)
        {
            return base.VisitCommit_transaction_statement(context);
        }

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

        public override Expression<Func<ExecutionContext, Task>> VisitCreate_database(TrunkSqlParser.Create_databaseContext context)
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

            return (ec) => ec.MasterDatabase.AttachDatabase(attachDatabaseParameters);
        }

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
            return ec => ExecuteCompositeExpression(ec, aggregate, childToAdd);
        }

        private async Task ExecuteCompositeExpression(
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

    public class SymbolTableValidator : TrunkSqlBaseVisitor<bool>
    {
        private readonly GlobalSymbolScope _globalScope =
            new GlobalSymbolScope();
        private readonly Stack<SymbolScope> _scopeStack =
            new Stack<SymbolScope>();

        public GlobalSymbolScope GlobalSymbolScope => _globalScope;

        public SymbolScope CurrentSymbolScope => _scopeStack.Count > 0 ? _scopeStack.Peek() : _globalScope;

        public FunctionSymbolScope CurrentFunctionSymbolScope => CurrentSymbolScope as FunctionSymbolScope;

        public override bool VisitCreate_procedure(TrunkSqlParser.Create_procedureContext context)
        {
            // TODO: Look for matching symbol matching context name
            var funcName = context.func_proc_name().GetText();
            if (CurrentSymbolScope.Find(funcName) != null)
            {
                // TODO: Throw information should include symbol location
                throw new Exception("proc name is not unique");
            }

            // Create function symbol for this proc in global scope
            GlobalSymbolScope.AddSymbol(new FunctionSymbol(funcName, TableColumnDataType.None, 0));

            // Create new function symbol scope
            _scopeStack.Push(new FunctionSymbolScope(GlobalSymbolScope, context.func_proc_name().GetText()));

            var result = base.VisitCreate_procedure(context);

            // Pop function scope off stack
            _scopeStack.Pop();
            return result;
        }

        public override bool VisitSql_clauses([NotNull] TrunkSqlParser.Sql_clausesContext context)
        {
            bool needToPopScope = false;
            if (CurrentSymbolScope != GlobalSymbolScope)
            {
                _scopeStack.Push(new LocalSymbolScope(CurrentSymbolScope));
                needToPopScope = true;
            }

            var result = base.VisitSql_clauses(context);

            // If we entered a scope earlier then make sure we pop
            if (needToPopScope)
            {
                _scopeStack.Pop();
            }
            return result;
        }

        public override bool VisitProcedure_param([NotNull] TrunkSqlParser.Procedure_paramContext context)
        {
            var symbolName = context.LOCAL_ID().GetText();
            var dataType = context.data_type();
            bool isVarying = context.children.Any(c => c == context.VARYING());
            bool isOutput = context.children.Any(c => c == context.OUT() || c == context.OUTPUT());
            bool isReadOnly = context.children.Any(c => c == context.READONLY());
            var dataTypeChild = dataType.GetChild(0);
            var length = 0;
            if (dataType.ChildCount > 2)
            {
                int.TryParse(dataType.GetChild(2).GetText(), out length);
            }
            if (dataTypeChild == dataType.BIT())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Bit, 1, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.SMALLINT())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Short, 2, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.INT())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Int, 4, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.BIGINT())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Long, 8, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.BINARY())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Byte, length, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.VARBINARY())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Byte, length, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.CHAR())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Char, length, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.VARCHAR())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Long, 8, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.NCHAR())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.NChar, length * 2, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.NVARCHAR())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Long, length * 2, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.DATETIME())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.DateTime, 8, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.FLOAT())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Float, 4, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.REAL())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Double, 8, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.MONEY())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Money, 8, isVarying, isOutput, isReadOnly));
            }
            else if (dataTypeChild == dataType.UNIQUEIDENTIFIER())
            {
                CurrentSymbolScope.AddSymbol(new ParameterSymbol(
                    symbolName, TableColumnDataType.Guid, 16, isVarying, isOutput, isReadOnly));
            }
            return base.VisitProcedure_param(context);
        }

        public override bool VisitDeclare_local([NotNull] TrunkSqlParser.Declare_localContext context)
        {
            var symbolName = context.LOCAL_ID().GetText();
            var dataType = context.data_type();
            var dataTypeChild = dataType.GetChild(0);
            var length = 0;
            if (dataType.ChildCount > 2)
            {
                int.TryParse(dataType.GetChild(2).GetText(), out length);
            }
            if (dataTypeChild == dataType.BIT())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Bit, 1));
            }
            else if (dataTypeChild == dataType.SMALLINT())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Short, 2));
            }
            else if (dataTypeChild == dataType.INT())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Int, 4));
            }
            else if (dataTypeChild == dataType.BIGINT())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Long, 8));
            }
            else if (dataTypeChild == dataType.BINARY())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Byte, length));
            }
            else if (dataTypeChild == dataType.VARBINARY())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Byte, length));
            }
            else if (dataTypeChild == dataType.CHAR())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Char, length));
            }
            else if (dataTypeChild == dataType.VARCHAR())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Long, 8));
            }
            else if (dataTypeChild == dataType.NCHAR())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.NChar, length * 2));
            }
            else if (dataTypeChild == dataType.NVARCHAR())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Long, length * 2));
            }
            else if (dataTypeChild == dataType.DATETIME())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.DateTime, 8));
            }
            else if (dataTypeChild == dataType.FLOAT())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Float, 4));
            }
            else if (dataTypeChild == dataType.REAL())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Double, 8));
            }
            else if (dataTypeChild == dataType.MONEY())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Money, 8));
            }
            else if (dataTypeChild == dataType.UNIQUEIDENTIFIER())
            {
                CurrentSymbolScope.AddSymbol(new VariableSymbol(
                    symbolName, TableColumnDataType.Guid, 16));
            }
            return base.VisitDeclare_local(context);
        }
    }

    public class SymbolScope
    {
        private readonly IDictionary<string, Symbol> _symbolTable =
            new Dictionary<string, Symbol>(StringComparer.OrdinalIgnoreCase);

        protected SymbolScope(SymbolScope parentScope = null)
        {
            ParentScope = parentScope;
        }

        public virtual SymbolScope ParentScope { get; }

        public void AddSymbol(Symbol symbol)
        {
            ValidateSymbol(symbol);
            _symbolTable.Add(symbol.Name, symbol);
        }

        public virtual Symbol Find(string symbolName)
        {
            if (_symbolTable.ContainsKey(symbolName))
            {
                return _symbolTable[symbolName];
            }

            if (ParentScope != null)
            {
                return ParentScope.Find(symbolName);
            }

            return null;
        }

        protected virtual void ValidateSymbol(Symbol symbol)
        {
        }
    }

    public class GlobalSymbolScope : SymbolScope
    {
    }

    public class ChildSymbolScope : SymbolScope
    {
        protected ChildSymbolScope(SymbolScope parentScope)
        {
        }
    }

    public class FunctionSymbolScope : ChildSymbolScope
    {
        public FunctionSymbolScope(GlobalSymbolScope globalScope, string functionName)
            : base(globalScope)
        {
            Name = functionName;
        }

        public string Name { get; }

        protected override void ValidateSymbol(Symbol symbol)
        {
            base.ValidateSymbol(symbol);

            // TODO: Validate things allowed in proc/func parameter list
        }
    }

    public class LocalSymbolScope : ChildSymbolScope
    {
        public LocalSymbolScope(SymbolScope parentScope)
            : base(parentScope)
        {
        }
    }

    public class Symbol
    {
        public Symbol(string name, TableColumnDataType type, int length)
        {
            Name = name;
            Type = type;
            Length = length;
        }

        public string Name { get; }

        public TableColumnDataType Type { get; }

        public int Length { get; }
    }

    public class VariableSymbol : Symbol
    {
        public VariableSymbol(string name, TableColumnDataType type, int length)
            : base(name, type, length)
        {
        }
    }

    public class ParameterSymbol : Symbol
    {
        public ParameterSymbol(
            string name,
            TableColumnDataType type,
            int length,
            bool isVarying,
            bool isOutput,
            bool isReadOnly)
            : base(name, type, length)
        {
            IsVarying = isVarying;
            IsOutput = isOutput;
            IsReadOnly = isReadOnly;
        }

        public bool IsVarying { get; }

        public bool IsOutput { get; }

        public bool IsReadOnly { get; }
    }

    public class FunctionSymbol : Symbol
    {
        public FunctionSymbol(string name, TableColumnDataType type, int length)
            : base(name, type, length)
        {
        }
    }

    public class ProcedureSymbol : Symbol
    {
        public ProcedureSymbol(string name, TableColumnDataType type)
            : base(name, type, 0)
        {
        }
    }
}
