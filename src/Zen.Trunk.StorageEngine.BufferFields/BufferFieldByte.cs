using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="SimpleBufferField{Byte}" />
    public class BufferFieldByte : SimpleBufferField<byte>
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BufferFieldByte"/> class.
        /// </summary>
        public BufferFieldByte()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferFieldByte"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public BufferFieldByte(byte value)
            : base(value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferFieldByte"/> class.
        /// </summary>
        /// <param name="prev">The prev.</param>
        public BufferFieldByte(BufferField prev)
            : base(prev)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferFieldByte"/> class.
        /// </summary>
        /// <param name="prev">The previous.</param>
        /// <param name="value">The value.</param>
        public BufferFieldByte(BufferField prev, byte value)
            : base(prev, value)
        {
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the size of a single element of this field
        /// </summary>
        /// <value>
        /// The size of the data.
        /// </value>
        public override int DataSize => 1;
        #endregion

        #region Protected Methods
        /// <summary>
        /// Called when reading from the specified stream manager.
        /// </summary>
        /// <param name="reader">A <see cref="T:BufferReaderWriter" /> object.</param>
        /// <remarks>
        /// Derived classes must provide an implementation for this method.
        /// </remarks>
        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = reader.ReadByte();
        }

        /// <summary>
        /// Called when writing to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:BufferReaderWriter" /> object.</param>
        /// <remarks>
        /// Derived classes must provide an implementation for this method.
        /// </remarks>
        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value);
        } 
        #endregion
    }
}