using System;
using System.Collections.Specialized;
using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldBitVector32 : SimpleBufferField<BitVector32>
    {
        public BufferFieldBitVector32()
            : this(0)
        {
        }

        public BufferFieldBitVector32(int value)
            : base(new BitVector32(value))
        {
        }

        public BufferFieldBitVector32(BufferField prev)
            : this(prev, 0)
        {
        }

        public BufferFieldBitVector32(BufferField prev, int value)
            : base(prev, new BitVector32(value))
        {
        }

        #region Public Properties
        public override int DataSize => 4;

        #endregion

        [CLSCompliant(false)]
        public bool GetBit(BitVector32.Section section, uint mask)
        {
            return (Value[section] & mask) != 0;
        }

        [CLSCompliant(false)]
        public void SetBit(BitVector32.Section section, uint mask, bool on)
        {
            var vector = Value;
            if (on)
            {
                vector[section] |= (ushort)mask;
            }
            else
            {
                vector[section] &= (ushort)(~mask);
            }
        }

        public void SetValue(BitVector32.Section section, int value)
        {
            var vector = Value;
            vector[section] = value;
        }

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = new BitVector32(reader.ReadInt32());
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value.Data);
        }
    }
}