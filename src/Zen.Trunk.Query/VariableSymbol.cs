using Zen.Trunk.Storage.Data.Table;

namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Query.Symbol" />
    public class VariableSymbol : Symbol
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VariableSymbol"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="length">The length.</param>
        public VariableSymbol(string name, TableColumnDataType type, int length)
            : base(name, type, length)
        {
        }
    }
}