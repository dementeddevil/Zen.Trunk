using System;
using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    [CLSCompliant(false)]
    public class BufferFieldUInt64 : SimpleBufferField<ulong>
    {
        public BufferFieldUInt64()
        {
        }

        public BufferFieldUInt64(ulong value)
            : base(value)
        {
        }

        public BufferFieldUInt64(BufferField prev)
            : base(prev)
        {
        }

        public BufferFieldUInt64(BufferField prev, ulong value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 8;

        #endregion

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = reader.ReadUInt64();
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value);
        }
    }
}