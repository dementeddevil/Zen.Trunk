using System;
using System.Collections.Generic;

namespace Zen.Trunk.Storage.Query
{
    public class DatabaseSymbolScope : ChildSymbolScope
    {
        private readonly IDictionary<string, SchemaSymbolScope> _schemaScopes =
            new Dictionary<string, SchemaSymbolScope>(StringComparer.OrdinalIgnoreCase);

        public DatabaseSymbolScope(string name, SymbolScope parentScope) : base(name, parentScope)
        {
        }

        public SchemaSymbolScope GetSchemaSymbolScope(string schemaName)
        {
            if (!_schemaScopes.TryGetValue(schemaName, out var scope))
            {
                scope = new SchemaSymbolScope(schemaName, this);
                _schemaScopes.Add(schemaName, scope);
            }

            return scope;
        }
    }
}