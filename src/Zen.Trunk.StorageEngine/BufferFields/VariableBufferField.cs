using System;

namespace Zen.Trunk.Storage.BufferFields
{
    /// <summary>
    /// Represents a variable array of fixed length fields.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [CLSCompliant(false)]
    public abstract class VariableBufferField<T> : ArrayBufferField<T>
    {
        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="VariableBufferField{T}"/> class.
        /// </summary>
        /// <param name="maxElements">The maximum elements.</param>
        protected VariableBufferField(ushort maxElements)
            : base(maxElements)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableBufferField{T}"/> class.
        /// </summary>
        /// <param name="maxElements">The maximum elements.</param>
        /// <param name="value">The value.</param>
        protected VariableBufferField(ushort maxElements, T value)
            : base(maxElements, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableBufferField{T}"/> class.
        /// </summary>
        /// <param name="prev">The previous.</param>
        /// <param name="maxElements">The maximum elements.</param>
        protected VariableBufferField(BufferField prev, ushort maxElements)
            : base(prev, maxElements)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableBufferField{T}"/> class.
        /// </summary>
        /// <param name="prev">The previous.</param>
        /// <param name="maxElements">The maximum elements.</param>
        /// <param name="value">The value.</param>
        protected VariableBufferField(BufferField prev, ushort maxElements, T value)
            : base(prev, maxElements, value)
        {
        }
        #endregion

        /// <summary>
        /// Gets the maximum length of this field.
        /// </summary>
        /// <value>
        /// The length of the field.
        /// </value>
        /// <remarks>
        /// By default this is the <see cref="P:DataSize" /> value multiplied
        /// by the <see cref="P:MaxElements" /> value.
        /// </remarks>
        public override int FieldLength => base.FieldLength + 2;
    }
}