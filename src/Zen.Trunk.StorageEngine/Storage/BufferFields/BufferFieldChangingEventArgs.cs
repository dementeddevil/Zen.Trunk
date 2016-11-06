using System;

namespace Zen.Trunk.Storage.BufferFields
{
    /// <summary>
    /// <c>BufferFieldChangingEventArgs</c> is passed as event data when
    /// a buffer field is changing.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    /// <seealso cref="BufferField.Changing"/>
    public class BufferFieldChangingEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BufferFieldChangingEventArgs"/> class.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public BufferFieldChangingEventArgs(object oldValue, object newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Gets the old value.
        /// </summary>
        /// <value>
        /// The old value.
        /// </value>
        public object OldValue { get; }

        /// <summary>
        /// Gets or sets the new value.
        /// </summary>
        /// <value>
        /// The new value.
        /// </value>
        public object NewValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BufferFieldChangingEventArgs"/> is cancel.
        /// </summary>
        /// <value>
        ///   <c>true</c> if cancel; otherwise, <c>false</c>.
        /// </value>
        public bool Cancel { get; set; }
    }
}