using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.Data.Table
{
    /// <summary>
    /// 
    /// </summary>
    public class TableRowWriter
    {
        #region Private Fields
        private readonly SwitchingBinaryWriter _bufferWriter;
        private readonly IList<TableColumnInfo> _rowDef;
        private readonly object[] _rowValues;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TableRowWriter"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="rowDef">The row definition.</param>
        public TableRowWriter(Stream stream, IList<TableColumnInfo> rowDef)
        {
            _bufferWriter = new SwitchingBinaryWriter(stream, true);
            _rowDef = rowDef;
            _rowValues = new object[_rowDef.Count];
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the column data <see cref="System.Object"/> at the 
        /// specified index.
        /// </summary>
        /// <value></value>
        public object this[int index]
        {
            set
            {
                if (index < 0 || index >= _rowValues.Length)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(index), index, "index out of range.");
                }
                _rowValues[index] = value;
            }
        }

        /// <summary>
        /// Gets the size of the row in bytes.
        /// </summary>
        /// <value>The size of the row.</value>
        public ushort RowSize
        {
            get
            {
                return (ushort)_rowDef
                    .Zip(_rowValues, (def, val) => def.GetActualDataSize(val))
                    .Sum();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Writes the row data in this instance to the underlying buffer writer.
        /// </summary>
        public void Write()
        {
            for (var index = 0; index < _rowDef.Count; ++index)
            {
                _rowDef[index].WriteData(_bufferWriter, _rowValues[index]);
            }
        }
        #endregion
    }
}