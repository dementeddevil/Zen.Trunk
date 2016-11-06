using System;

namespace Zen.Trunk.Storage.BufferFields
{
    /// <summary>
    /// Represents a fixed array of fixed length fields.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [CLSCompliant(false)]
    public abstract class ArrayBufferField<T> : SimpleBufferField<T>
    {
        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayBufferField{T}"/> class.
        /// </summary>
        /// <param name="maxElements">The maximum elements.</param>
        protected ArrayBufferField(ushort maxElements)
        {
            MaxElements = maxElements;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayBufferField{T}"/> class.
        /// </summary>
        /// <param name="maxElements">The maximum elements.</param>
        /// <param name="value">The value.</param>
        protected ArrayBufferField(ushort maxElements, T value)
            : base(value)
        {
            MaxElements = maxElements;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayBufferField{T}"/> class.
        /// </summary>
        /// <param name="prev">The previous.</param>
        /// <param name="maxElements">The maximum elements.</param>
        protected ArrayBufferField(BufferField prev, ushort maxElements)
            : base(prev)
        {
            MaxElements = maxElements;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayBufferField{T}"/> class.
        /// </summary>
        /// <param name="prev">The previous.</param>
        /// <param name="maxElements">The maximum elements.</param>
        /// <param name="value">The value.</param>
        protected ArrayBufferField(BufferField prev, ushort maxElements, T value)
            : base(prev, value)
        {
            MaxElements = maxElements;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the number of discrete elements tracked in this object
        /// </summary>
        /// <value>
        /// The max number of elements.
        /// </value>
        /// <remarks>
        /// By default this property returns 1.
        /// </remarks>
        public override ushort MaxElements { get; }
        #endregion
    }
}