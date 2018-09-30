using System;
using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    /// <summary>
    /// Represents a field stored in a buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The use of buffer fields mean that a we can support locking strategies
    /// that do not require the entire buffer to be locked for the duration of
    /// the transaction which increases concurrency and reduces the likelihood
    /// of deadlocks.
    /// </para>
    /// <para>
    /// Fields can be locked or unlocked.
    /// Locked fields never write themselves when saving to a stream. The locked
    /// state makes no difference when reading from a stream
    /// </para>
    /// <para>
    /// Buffer fields inherently support chaining and make use of
    /// <see cref="SwitchingBinaryReader"/> and <see cref="SwitchingBinaryWriter"/>
    /// to persist data to and from a stream.
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
        /// Gets a value indicating whether this instance is writable.
        /// </summary>
        /// <value><c>true</c> if this instance is writable; otherwise, <c>false</c>.</value>
        public bool IsWritable { get; private set; } = true;

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
            IsWritable = false;
        }

        /// <summary>
        /// Unlocks this instance.
        /// </summary>
        public void Unlock()
        {
            IsWritable = true;
        }

        /// <summary>
        /// Reads this instance from the specified steam manager.
        /// </summary>
        /// <param name="reader">A <see cref="T:SwitchingBinaryReader"/> object.</param>
        public void Read(SwitchingBinaryReader reader)
        {
            OnRead(reader);
            if (NextField != null && NextField.CanContinue(true))
            {
                NextField.Read(reader);
            }
        }

        /// <summary>
        /// Writes the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:SwitchingBinaryWriter"/> object.</param>
        public void Write(SwitchingBinaryWriter writer)
        {
            writer.WriteToUnderlyingStream = IsWritable;
            OnWrite(writer);
            if (NextField != null && NextField.CanContinue(false))
            {
                NextField.Write(writer);
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
        /// <param name="reader">A <see cref="T:SwitchingBinaryReader"/> object.</param>
        /// <remarks>
        /// Derived classes must provide an implementation for this method.
        /// </remarks>
        protected abstract void OnRead(SwitchingBinaryReader reader);

        /// <summary>
        /// Called when writing to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:SwitchingBinaryWriter"/> object.</param>
        /// <remarks>
        /// Derived classes must provide an implementation for this method.
        /// </remarks>
        protected abstract void OnWrite(SwitchingBinaryWriter writer);
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
