namespace Zen.Trunk.Query
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Query.SymbolScope" />
    public class ChildSymbolScope : NamedSymbolScope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChildSymbolScope"/> class.
        /// </summary>
        /// <param name="name">Scope name</param>
        /// <param name="parentScope">The parent scope.</param>
        protected ChildSymbolScope(string name, SymbolScope parentScope)
            : base(name, parentScope)
        {
        }
    }
}