using System;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    [CLSCompliant(false)]
    public class BufferFieldStringVariable : VariableBufferField<string>
    {
        public BufferFieldStringVariable(ushort maxLength)
            : this(null, maxLength, string.Empty)
        {
        }

        public BufferFieldStringVariable(BufferField prev, ushort maxLength)
            : this(prev, maxLength, string.Empty)
        {
        }

        public BufferFieldStringVariable(BufferField prev, ushort maxLength, string value)
            : base(prev, maxLength, value)
        {
        }

        #region Public Properties
        public override int DataSize => UseUnicode ? 2 : 1;

        public bool UseUnicode { get; set; }

        public override int FieldLength => base.FieldLength + 2;

        #endregion

        protected override void OnRead(BufferReaderWriter streamManager)
        {
            streamManager.UseUnicode = UseUnicode;
            Value = streamManager.ReadString();
        }

        protected override void OnWrite(BufferReaderWriter streamManager)
        {
            streamManager.UseUnicode = UseUnicode;
            streamManager.Write(Value);
        }
    }
}