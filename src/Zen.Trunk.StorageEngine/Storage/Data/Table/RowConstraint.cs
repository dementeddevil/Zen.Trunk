using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Data.Table
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="BufferFieldWrapper" />
    public class RowConstraint : BufferFieldWrapper
    {
        private readonly BufferFieldUInt16 _columnId;
        private readonly BufferFieldByte _constraintType;
        private readonly BufferFieldStringFixed _constraintData;

        /// <summary>
        /// Initializes a new instance of the <see cref="RowConstraint"/> class.
        /// </summary>
        public RowConstraint()
        {
            _columnId = new BufferFieldUInt16();
            _constraintType = new BufferFieldByte(_columnId);
            _constraintData = new BufferFieldStringFixed(_constraintType, 64);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RowConstraint"/> class.
        /// </summary>
        /// <param name="columnId">The column identifier.</param>
        /// <param name="constraintType">Type of the constraint.</param>
        public RowConstraint(
            ushort columnId, RowConstraintType constraintType) : this()
        {
            ColumnId = columnId;
            ConstraintType = constraintType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RowConstraint"/> class.
        /// </summary>
        /// <param name="columnId">The column identifier.</param>
        /// <param name="constraintType">Type of the constraint.</param>
        /// <param name="constraintData">The constraint data.</param>
        public RowConstraint(
            ushort columnId, RowConstraintType constraintType, string constraintData) : this()
        {
            ColumnId = columnId;
            ConstraintType = constraintType;
            ConstraintData = constraintData;
        }

        /// <summary>
        /// Gets or sets the column identifier.
        /// </summary>
        /// <value>
        /// The column identifier.
        /// </value>
        public ushort ColumnId
        {
            get
            {
                return _columnId.Value;
            }
            set
            {
                _columnId.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of the constraint.
        /// </summary>
        /// <value>
        /// The type of the constraint.
        /// </value>
        public RowConstraintType ConstraintType
        {
            get
            {
                return (RowConstraintType)_constraintType.Value;
            }
            set
            {
                _constraintType.Value = (byte)value;
            }
        }

        /// <summary>
        /// Gets or sets the constraint data.
        /// </summary>
        /// <value>
        /// The constraint data.
        /// </value>
        public string ConstraintData
        {
            get
            {
                return _constraintData.Value;
            }
            set
            {
                _constraintData.Value = value;
            }
        }

        /// <summary>
        /// Gets the first buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField FirstField => _columnId;

        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _constraintData;
    }
}