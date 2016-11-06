using System;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    [CLSCompliant(false)]
    public class BufferFieldUInt16 : SimpleBufferField<ushort>
    {
        #region Public Constructors
        public BufferFieldUInt16()
        {
        }

        public BufferFieldUInt16(ushort value)
            : base(value)
        {
        }

        public BufferFieldUInt16(BufferField prev)
            : base(prev)
        {
        }

        public BufferFieldUInt16(BufferField prev, ushort value)
            : base(prev, value)
        {
        }
        #endregion

        #region Public Properties
        public override int DataSize => 2;

        #endregion

        protected override void OnRead(BufferReaderWriter streamManager)
        {
            Value = streamManager.ReadUInt16();
        }

        protected override void OnWrite(BufferReaderWriter streamManager)
        {
            streamManager.Write(Value);
        }
    }
}