using System;
using System.Collections.Generic;

namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// 
    /// </summary>
    public class SymbolScope
    {
        private readonly IDictionary<string, Symbol> _symbolTable =
            new Dictionary<string, Symbol>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolScope"/> class.
        /// </summary>
        /// <param name="parentScope">The parent scope.</param>
        protected SymbolScope(SymbolScope parentScope = null)
        {
            ParentScope = parentScope;
        }

        private SymbolScope ParentScope { get; }

        /// <summary>
        /// Adds the specified symbol to the symbol scope.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        public void AddSymbol(Symbol symbol)
        {
            ValidateSymbol(symbol);
            _symbolTable.Add(symbol.Name, symbol);
        }

        /// <summary>
        /// Finds the symbol with the specified name.
        /// </summary>
        /// <param name="symbolName">Name of the symbol.</param>
        /// <returns></returns>
        public virtual Symbol Find(string symbolName)
        {
            return FindInThisScope(symbolName) ?? FindInParentScope(symbolName);
        }

        protected Symbol FindInThisScope(string symbolName)
        {
            return _symbolTable.ContainsKey(symbolName) ? _symbolTable[symbolName] : null;
        }

        protected Symbol FindInParentScope(string symbolName)
        {
            return ParentScope?.Find(symbolName);
        }

        /// <summary>
        /// Validates the symbol.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        protected virtual void ValidateSymbol(Symbol symbol)
        {
        }
    }
}