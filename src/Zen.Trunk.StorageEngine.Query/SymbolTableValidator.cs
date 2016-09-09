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
    /// <seealso cref="Zen.Trunk.Storage.Query.TrunkSqlBaseVisitor{System.Boolean}" />
    public class SymbolTableValidator : TrunkSqlBaseVisitor<bool>
    {
        private readonly GlobalSymbolScope _globalScope =
            new GlobalSymbolScope();
        private readonly Stack<SymbolScope> _scopeStack =
            new Stack<SymbolScope>();

        /// <summary>
        /// Gets the global symbol scope.
        /// </summary>
        /// <value>
        /// The global symbol scope.
        /// </value>
        public GlobalSymbolScope GlobalSymbolScope => _globalScope;

        /// <summary>
        /// Gets the current symbol scope.
        /// </summary>
        /// <value>
        /// The current symbol scope.
        /// </value>
        public SymbolScope CurrentSymbolScope => _scopeStack.Count > 0 ? _scopeStack.Peek() : _globalScope;

        /// <summary>
        /// Gets the current function symbol scope.
        /// </summary>
        /// <value>
        /// The current function symbol scope.
        /// </value>
        public FunctionSymbolScope CurrentFunctionSymbolScope => CurrentSymbolScope as FunctionSymbolScope;

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

        /// <summary>
        /// Visit a parse tree produced by <see cref="M:Zen.Trunk.Storage.Query.TrunkSqlParser.sql_clauses" />.
        /// <para>
        /// The default implementation returns the result of calling <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)" />
        /// on <paramref name="context" />.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <returns></returns>
        /// <return>The visitor result.</return>
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
    }
}