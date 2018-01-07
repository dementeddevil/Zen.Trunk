using System;

namespace Zen.Trunk.Storage.BufferFields
{
    /// <summary>
    /// Represents a fixed width field.
    /// </summary>
    /// <typeparam name="T">The underlying type.</typeparam>
    public abstract class SimpleBufferField<T> : BufferField
    {
        #region Private Fields
        private T _value;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleBufferField{T}"/> class.
        /// </summary>
        protected SimpleBufferField()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleBufferField{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        protected SimpleBufferField(T value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleBufferField{T}"/> class.
        /// </summary>
        /// <param name="prev">The prev.</param>
        protected SimpleBufferField(BufferField prev)
            : base(prev)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleBufferField{T}"/> class.
        /// </summary>
        /// <param name="prev">The previous.</param>
        /// <param name="value">The value.</param>
        protected SimpleBufferField(BufferField prev, T value)
            : base(prev)
        {
            _value = value;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets/sets the underlying value.
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if ((_value == null && value != null) ||
                    (_value != null && !_value.Equals(value)))
                {
                    var e = new BufferFieldChangingEventArgs(_value, value);
                    if (OnValueChanging(e))
                    {
                        _value = (T)e.NewValue;
                        OnValueChanged(EventArgs.Empty);
                    }
                }
            }
        }
        #endregion
    }
}