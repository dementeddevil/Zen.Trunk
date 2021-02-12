namespace Zen.Trunk.Query
{
    public class NamedSymbolScope : SymbolScope
    {
        public NamedSymbolScope(string name, SymbolScope parentScope = null) : base(parentScope)
        {
            Name = name;
        }

        public string Name { get; }
    }
}