using System;
using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    [CLSCompliant(false)]
    public class BufferFieldStringFixed : ArrayBufferField<string>
    {
        public BufferFieldStringFixed(ushort maxLength)
            : this(null, maxLength, string.Empty)
        {
        }

        public BufferFieldStringFixed(BufferField prev, ushort maxLength)
            : this(prev, maxLength, string.Empty)
        {
        }

        public BufferFieldStringFixed(BufferField prev, ushort maxLength, string value)
            : base(prev, maxLength, value)
        {
        }

        #region Public Properties
        public override int DataSize => UseUnicode ? 2 : 1;

        public bool UseUnicode { get; set; }
        #endregion

        #region Protected Methods
        protected override bool OnValueChanging(BufferFieldChangingEventArgs e)
        {
            var value = (string)e.NewValue;
            if (value != null && value.Length > MaxElements)
            {
                e.NewValue = value.Substring(0, MaxElements);
            }
            else
            {
                e.NewValue = value;
            }

            return base.OnValueChanging(e);
        }

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            reader.UseUnicode = UseUnicode;
            Value = reader.ReadStringExact(MaxElements);
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.UseUnicode = UseUnicode;
            writer.WriteStringExact(Value, MaxElements);
        }
        #endregion
    }
}