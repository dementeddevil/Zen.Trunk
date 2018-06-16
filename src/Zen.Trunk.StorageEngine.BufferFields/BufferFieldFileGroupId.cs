using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldFileGroupId : SimpleBufferField<FileGroupId>
    {
        public BufferFieldFileGroupId()
            : this(FileGroupId.Invalid)
        {
        }

        public BufferFieldFileGroupId(FileGroupId value)
            : base(value)
        {
        }

        public BufferFieldFileGroupId(BufferField prev)
            : this(prev, FileGroupId.Invalid)
        {
        }

        public BufferFieldFileGroupId(BufferField prev, FileGroupId value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 8;
        #endregion

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = new FileGroupId(reader.ReadByte());
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value.Value);
        }
    }
}