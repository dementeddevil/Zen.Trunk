using Zen.Trunk.Storage.Data.Table;

namespace Zen.Trunk.Query
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Query.Symbol" />
    public class FunctionSymbol : DataTypeSymbol
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSymbol"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="length">The length.</param>
        public FunctionSymbol(string name, TableColumnDataType type, int length)
            : base(name, type, length)
        {
        }
    }
}