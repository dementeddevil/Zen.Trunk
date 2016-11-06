using System;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldBitVector8 : BufferFieldByte
    {
        public BufferFieldBitVector8()
            : this(0)
        {
        }

        public BufferFieldBitVector8(byte value)
            : base(value)
        {
        }

        public BufferFieldBitVector8(BufferField prev)
            : this(prev, 0)
        {
        }

        public BufferFieldBitVector8(BufferField prev, byte value)
            : base(prev, value)
        {
        }

        #region Public Methods
        public bool GetBit(byte index)
        {
            if (index > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range (0-7)");
            }
            var mask = (byte)(1 << index);
            return (Value & mask) != 0;
        }

        public void SetBit(byte index, bool on)
        {
            if (index > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range (0-7)");
            }
            var mask = (byte)(1 << index);
            if (on)
            {
                Value |= mask;
            }
            else
            {
                Value &= (byte)(~mask);
            }
        }
        #endregion
    }
}