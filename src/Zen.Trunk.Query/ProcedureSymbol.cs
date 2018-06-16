using Zen.Trunk.Storage.Data.Table;

namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Query.Symbol" />
    public class ProcedureSymbol : Symbol
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcedureSymbol"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        public ProcedureSymbol(string name)
            : base(name)
        {
        }
    }
}