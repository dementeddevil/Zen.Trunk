using Zen.Trunk.Storage.Data.Table;

namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Query.Symbol" />
    public class ParameterSymbol : DataTypeSymbol
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterSymbol"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="length">The length.</param>
        /// <param name="isVarying">if set to <c>true</c> [is varying].</param>
        /// <param name="isOutput">if set to <c>true</c> [is output].</param>
        /// <param name="isReadOnly">if set to <c>true</c> [is read only].</param>
        public ParameterSymbol(
            string name,
            TableColumnDataType type,
            int length,
            bool isVarying,
            bool isOutput,
            bool isReadOnly)
            : base(name, type, length)
        {
            IsVarying = isVarying;
            IsOutput = isOutput;
            IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is varying.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is varying; otherwise, <c>false</c>.
        /// </value>
        public bool IsVarying { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is output.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is output; otherwise, <c>false</c>.
        /// </value>
        public bool IsOutput { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly { get; }
    }
}