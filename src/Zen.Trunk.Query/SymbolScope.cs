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

        /// <summary>
        /// Gets the parent scope.
        /// </summary>
        /// <value>
        /// The parent scope.
        /// </value>
        public virtual SymbolScope ParentScope { get; }

        /// <summary>
        /// Adds the symbol.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        public void AddSymbol(Symbol symbol)
        {
            ValidateSymbol(symbol);
            _symbolTable.Add(symbol.Name, symbol);
        }

        /// <summary>
        /// Finds the specified symbol name.
        /// </summary>
        /// <param name="symbolName">Name of the symbol.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Validates the symbol.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        protected virtual void ValidateSymbol(Symbol symbol)
        {
        }
    }
}