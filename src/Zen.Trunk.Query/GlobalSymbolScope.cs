using System;
using System.Collections.Generic;

namespace Zen.Trunk.Query
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Query.SymbolScope" />
    public class GlobalSymbolScope : SymbolScope
    {
        private readonly IDictionary<string, DatabaseSymbolScope> _databaseScopes =
            new Dictionary<string, DatabaseSymbolScope>(StringComparer.OrdinalIgnoreCase);

        public DatabaseSymbolScope GetDatabaseSymbolScope(string databaseName)
        {
            if (!_databaseScopes.TryGetValue(databaseName, out var scope))
            {
                scope = new DatabaseSymbolScope(databaseName, this);
                _databaseScopes.Add(databaseName, scope);
            }

            return scope;
        }
    }
}