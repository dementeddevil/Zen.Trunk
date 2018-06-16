using System;
using Zen.Trunk.IO;

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

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = reader.ReadUInt16();
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value);
        }
    }
}