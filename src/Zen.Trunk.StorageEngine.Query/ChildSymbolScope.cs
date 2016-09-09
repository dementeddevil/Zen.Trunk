namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Query.SymbolScope" />
    public class ChildSymbolScope : SymbolScope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChildSymbolScope"/> class.
        /// </summary>
        /// <param name="parentScope">The parent scope.</param>
        protected ChildSymbolScope(SymbolScope parentScope)
            : base(parentScope)
        {
        }
    }
}