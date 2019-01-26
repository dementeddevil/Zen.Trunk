using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using Zen.Trunk.Storage.Data.Table;

namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Need to add schema scoping support (with dbo default scope used when no schema is specified
    /// This will affect proc, function and table methods
    /// </remarks>
    /// <seealso cref="TrunkSqlBaseVisitor{Boolean}" />
    public class SymbolTableValidator : TrunkSqlBaseVisitor<bool>
    {
        private class SymbolScopeHolder : IDisposable
        {
            private readonly SymbolTableValidator _validator;
            private bool _isDisposed;

            public SymbolScopeHolder(SymbolTableValidator validator, SymbolScope scope)
            {
                _validator = validator;
                _validator._scopeStack.Push(scope);
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                    _validator._scopeStack.Pop();
                }
            }
        }

        private readonly Stack<SymbolScope> _scopeStack = new Stack<SymbolScope>();
        private string _currentDatabaseName;

        public SymbolTableValidator(string currentDatabaseName = null)
        {
            _currentDatabaseName = currentDatabaseName ?? "master";
        }

        /// <summary>
        /// Gets the global symbol scope.
        /// </summary>
        /// <value>
        /// The global symbol scope.
        /// </value>
        public GlobalSymbolScope GlobalSymbolScope { get; } = new GlobalSymbolScope();

        /// <summary>
        /// Gets the current symbol scope.
        /// </summary>
        /// <value>
        /// The current symbol scope.
        /// </value>
        public SymbolScope CurrentSymbolScope => _scopeStack.Count > 0 ? _scopeStack.Peek() : GlobalSymbolScope;

        /// <summary>
        /// Gets the current database symbol scope.
        /// </summary>
        public DatabaseSymbolScope CurrentDatabaseScope => (DatabaseSymbolScope)_scopeStack.FirstOrDefault(s => s is DatabaseSymbolScope);

        /// <summary>
        /// Gets the current schema symbol scope.
        /// </summary>
        public SchemaSymbolScope CurrentSchemaScope => (SchemaSymbolScope)_scopeStack.FirstOrDefault(s => s is SchemaSymbolScope);

        /// <summary>
        /// Gets the current method symbol scope.
        /// </summary>
        /// <value>
        /// The current function symbol scope.
        /// </value>
        public MethodSymbolScope CurrentMethodSymbolScope => (MethodSymbolScope)_scopeStack.FirstOrDefault(s => s is MethodSymbolScope);

        /// <summary>
        /// Visit a parse tree produced by <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.create_procedure" />.
        /// <para>
        /// The default implementation returns the result of calling <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)" />
        /// on <paramref name="context" />.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns></returns>
        /// <exception cref="Exception">proc name is not unique</exception>
        /// <return>The visitor result.</return>
        public override bool VisitCreate_procedure(TrunkSqlParser.Create_procedureContext context)
        {
            // Update database and schema context based on function name
            var funcOrProcName = context.func_proc_name();
            UpdateDatabaseAndSchemaScopes(
                funcOrProcName.database.GetText(),
                funcOrProcName.schema.GetText());
                
            // Validate that procedure name is unique across the owning schema
            var funcName = funcOrProcName.procedure.GetText();
            if (CurrentSchemaScope.Find(funcName) != null)
            {
                // TODO: Throw information should include symbol location
                throw new Exception("proc name is not unique");
            }

            // Create procedure symbol for this proc in global scope
            CurrentSchemaScope.AddSymbol(new ProcedureSymbol(funcName));

            // Create new function symbol scope
            using (BeginSymbolScope(new MethodSymbolScope(CurrentSchemaScope, funcName)))
            {
                return base.VisitCreate_procedure(context);
            }
        }

        public override bool VisitBlock_statement(TrunkSqlParser.Block_statementContext context)
        {
            using (BeginSymbolScope(new LocalSymbolScope(CurrentSymbolScope)))
            {
                return base.VisitBlock_statement(context);
            }
        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.procedure_param" />.
        /// <para>
        /// The default implementation returns the result of calling <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)" />
        /// on <paramref name="context" />.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns></returns>
        /// <return>The visitor result.</return>
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

        /// <summary>
        /// Visit a parse tree produced by <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.declare_local" />.
        /// <para>
        /// The default implementation returns the result of calling <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)" />
        /// on <paramref name="context" />.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns></returns>
        /// <return>The visitor result.</return>
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

        public override bool VisitUse_statement(TrunkSqlParser.Use_statementContext context)
        {
            _currentDatabaseName = context.database.GetText();
            UpdateDatabaseAndSchemaScopes(_currentDatabaseName, null);
            return base.VisitUse_statement(context);
        }

        private void UpdateDatabaseAndSchemaScopes(string databaseName, string schemaName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                databaseName = _currentDatabaseName;
            }

            if (string.IsNullOrEmpty(schemaName))
            {
                // TODO: The default schema is a property of the active connection
                //  and by default this is [dbo]
                schemaName = "dbo";
            }

            if (CurrentDatabaseScope == null ||
                !CurrentDatabaseScope.Name.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
            {
                // Discard all scopes
                _scopeStack.Clear();
                _scopeStack.Push(GlobalSymbolScope.GetDatabaseSymbolScope(databaseName));
            }

            if (CurrentSchemaScope == null ||
                !CurrentSchemaScope.Name.Equals(schemaName, StringComparison.OrdinalIgnoreCase))
            {
                while (!(_scopeStack.Peek() is DatabaseSymbolScope))
                {
                    _scopeStack.Pop();
                }

                // ReSharper disable once PossibleNullReferenceException
                _scopeStack.Push(CurrentDatabaseScope.GetSchemaSymbolScope(schemaName));
            }
        }

        private IDisposable BeginSymbolScope(SymbolScope scope)
        {
            return new SymbolScopeHolder(this, scope);
        }
    }
}