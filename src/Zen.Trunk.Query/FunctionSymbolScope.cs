namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Query.ChildSymbolScope" />
    public class FunctionSymbolScope : ChildSymbolScope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSymbolScope"/> class.
        /// </summary>
        /// <param name="globalScope">The global scope.</param>
        /// <param name="functionName">Name of the function.</param>
        public FunctionSymbolScope(GlobalSymbolScope globalScope, string functionName)
            : base(globalScope)
        {
            Name = functionName;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        // ReSharper disable once RedundantOverriddenMember
        /// <summary>
        /// Validates the symbol.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        protected override void ValidateSymbol(Symbol symbol)
        {
            base.ValidateSymbol(symbol);

            // TODO: Validate things allowed in proc/func parameter list
        }
    }
}