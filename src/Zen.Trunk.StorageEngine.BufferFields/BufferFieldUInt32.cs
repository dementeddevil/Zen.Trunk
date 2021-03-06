using System;
using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    [CLSCompliant(false)]
    public class BufferFieldUInt32 : SimpleBufferField<uint>
    {
        public BufferFieldUInt32()
        {
        }

        public BufferFieldUInt32(uint value)
            : base(value)
        {
        }

        public BufferFieldUInt32(BufferField prev)
            : base(prev)
        {
        }

        public BufferFieldUInt32(BufferField prev, uint value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 4;

        #endregion

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = reader.ReadUInt32();
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value);
        }
    }
}