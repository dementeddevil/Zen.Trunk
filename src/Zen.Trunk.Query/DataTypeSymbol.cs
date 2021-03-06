﻿using Zen.Trunk.Storage.Data.Table;

namespace Zen.Trunk.Query
{
    /// <summary>
    /// 
    /// </summary>
    public class DataTypeSymbol : Symbol
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Symbol"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="length">The length.</param>
        public DataTypeSymbol(string name, TableColumnDataType type, int length) : base(name)
        {
            Type = type;
            Length = length;
        }


        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public TableColumnDataType Type { get; }

        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        public int Length { get; }
    }
}