namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Query.ChildSymbolScope" />
    public class MethodSymbolScope : ChildSymbolScope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodSymbolScope"/> class.
        /// </summary>
        /// <param name="schemaScope">The schema scope.</param>
        /// <param name="functionName">Name of the function.</param>
        public MethodSymbolScope(SchemaSymbolScope schemaScope, string functionName)
            : base(functionName, schemaScope)
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

        /// <summary>
        /// Overridden. Find the symbol in this scope.
        /// </summary>
        /// <param name="symbolName"></param>
        /// <returns></returns>
        /// <remarks>
        /// This overload does not recurse into parent scopes.
        /// </remarks>
        public override Symbol Find(string symbolName)
        {
            return FindInThisScope(symbolName);
        }

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