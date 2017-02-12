using System;
using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldByteArrayUnbounded : SimpleBufferField<byte[]>
    {
        #region Public Constructors
        public BufferFieldByteArrayUnbounded()
        {
        }

        public BufferFieldByteArrayUnbounded(byte[] value)
            : base(value)
        {
        }

        public BufferFieldByteArrayUnbounded(BufferField prev)
            : base(prev)
        {
        }

        public BufferFieldByteArrayUnbounded(BufferField prev, byte[] value)
            : base(prev, value)
        {
        }
        #endregion

        #region Public Properties
        [CLSCompliant(false)]
        public override ushort MaxElements
        {
            get
            {
                if (Value == null)
                {
                    return 0;
                }
                return (ushort)Value.Length;
            }
        }

        public override int DataSize => 1;

        public override int FieldLength => base.FieldLength + 1;

        #endregion

        protected override bool OnValueChanging(BufferFieldChangingEventArgs e)
        {
            var data = (byte[])e.NewValue;
            if (data != null && data.Length > 255)
            {
                throw new InvalidOperationException("Array too long (255 elem max).");
            }
            return base.OnValueChanging(e);
        }
        protected override void OnRead(SwitchingBinaryReader streamManager)
        {
            var length = streamManager.ReadByte();
            if (length > 0)
            {
                Value = streamManager.ReadBytes(length);
            }
            else
            {
                Value = null;
            }
        }

        protected override void OnWrite(SwitchingBinaryWriter streamManager)
        {
            streamManager.Write((byte)MaxElements);
            if (MaxElements > 0)
            {
                streamManager.Write(Value);
            }
        }
    }
}