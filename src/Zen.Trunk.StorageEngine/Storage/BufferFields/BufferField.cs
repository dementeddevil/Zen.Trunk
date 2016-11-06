using System;
using Zen.Trunk.Storage.IO;

// ReSharper disable MissingXmlDoc

namespace Zen.Trunk.Storage.BufferFields
{
    /// <summary>
    /// Represents a field stored in a buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fields can be locked or unlocked.
    /// Locked fields never write themselves when saving to a buffer.
    /// </para>
    /// </remarks>
    public abstract class BufferField
    {
        #region Public Events
        /// <summary>
        /// Occurs when this buffer field value is changing.
        /// </summary>
        public event EventHandler<BufferFieldChangingEventArgs> Changing;

        /// <summary>
        /// Occurs when this buffer field value has changed.
        /// </summary>
        public event EventHandler Changed;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BufferField"/> class.
        /// </summary>
        protected BufferField()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferField"/> class.
        /// </summary>
        /// <param name="prev">The prev.</param>
        protected BufferField(BufferField prev)
        {
            prev?.SetNextField(this);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets a value indicating whether this instance is writeable.
        /// </summary>
        /// <value><c>true</c> if this instance is writeable; otherwise, <c>false</c>.</value>
        public bool IsWriteable { get; private set; } = true;

        /// <summary>
        /// Gets the size of a single element of this field
        /// </summary>
        /// <value>The size of the data.</value>
        public abstract int DataSize
        {
            get;
        }

        /// <summary>
        /// Gets the number of discrete elements tracked in this object
        /// </summary>
        /// <value>The max number of elements.</value>
        /// <remarks>
        /// By default this property returns 1.
        /// </remarks>
        [CLSCompliant(false)]
        public virtual ushort MaxElements => 1;

        /// <summary>
        /// Gets the maximum length of this field.
        /// </summary>
        /// <value>The length of the field.</value>
        /// <remarks>
        /// By default this is the <see cref="P:DataSize"/> value multiplied
        /// by the <see cref="P:MaxElements"/> value.
        /// </remarks>
        public virtual int FieldLength => (DataSize * MaxElements);

        /// <summary>
        /// Gets the next field.
        /// </summary>
        /// <value>The next <see cref="T:BufferField"/> field object.</value>
        public BufferField NextField { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Locks this instance.
        /// </summary>
        public void Lock()
        {
            IsWriteable = true;
        }

        /// <summary>
        /// Unlocks this instance.
        /// </summary>
        public void Unlock()
        {
            IsWriteable = false;
        }

        /// <summary>
        /// Reads this instance from the specified steam manager.
        /// </summary>
        /// <param name="streamManager">A <see cref="T:BufferReaderWriter"/> object.</param>
        public void Read(BufferReaderWriter streamManager)
        {
            OnRead(streamManager);
            if (NextField != null && NextField.CanContinue(true))
            {
                NextField.Read(streamManager);
            }
        }

        /// <summary>
        /// Writes the specified stream manager.
        /// </summary>
        /// <param name="streamManager">A <see cref="T:BufferReaderWriter"/> object.</param>
        public void Write(BufferReaderWriter streamManager)
        {
            streamManager.IsWritable = IsWriteable;
            OnWrite(streamManager);
            if (NextField != null && NextField.CanContinue(false))
            {
                NextField.Write(streamManager);
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Determines whether this instance can continue persistence.
        /// </summary>
        /// <param name="isReading">if set to <c>true</c> then instance is being read;
        /// otherwise <c>false</c> and the instance is being written.</param>
        /// <returns>
        /// <c>true</c> if this instance can continue persistence; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// By default this method returns <c>true</c>.
        /// </remarks>
        protected virtual bool CanContinue(bool isReading)
        {
            return true;
        }

        /// <summary>
        /// Raises the <see cref="E:Changing"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:BufferFieldChangingEventArgs"/> 
        /// instance containing the event data.</param>
        /// <returns><c>true</c> if the change has been allowed; otherwise,
        /// <c>false</c> if the change has been cancelled.</returns>
        protected virtual bool OnValueChanging(BufferFieldChangingEventArgs e)
        {
            var handler = Changing;
            if (handler != null)
            {
                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (EventHandler<BufferFieldChangingEventArgs>
                    handlerInstance in handler.GetInvocationList())
                {
                    handlerInstance(this, e);
                    if (e.Cancel)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Raises the <see cref="E:Changed"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:EventArgs"/> instance containing
        /// the event data.</param>
        protected virtual void OnValueChanged(EventArgs e)
        {
            var handler = Changed;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Called when reading from the specified stream manager.
        /// </summary>
        /// <param name="streamManager">A <see cref="T:BufferReaderWriter"/> object.</param>
        /// <remarks>
        /// Derived classes must provide an implementation for this method.
        /// </remarks>
        protected abstract void OnRead(BufferReaderWriter streamManager);

        /// <summary>
        /// Called when writing to the specified stream manager.
        /// </summary>
        /// <param name="streamManager">A <see cref="T:BufferReaderWriter"/> object.</param>
        /// <remarks>
        /// Derived classes must provide an implementation for this method.
        /// </remarks>
        protected abstract void OnWrite(BufferReaderWriter streamManager);
        #endregion

        #region Private Methods
        private void SetNextField(BufferField next)
        {
            if (NextField != null)
            {
                throw new InvalidOperationException("Already chained.");
            }
            NextField = next;
        }
        #endregion
    }
}
