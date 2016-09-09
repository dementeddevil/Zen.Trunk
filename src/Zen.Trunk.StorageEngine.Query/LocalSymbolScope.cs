namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Query.ChildSymbolScope" />
    public class LocalSymbolScope : ChildSymbolScope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalSymbolScope"/> class.
        /// </summary>
        /// <param name="parentScope">The parent scope.</param>
        public LocalSymbolScope(SymbolScope parentScope)
            : base(parentScope)
        {
        }
    }
}