using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldIndexId : SimpleBufferField<IndexId>
    {
        public BufferFieldIndexId()
            : this(IndexId.Zero)
        {
        }

        public BufferFieldIndexId(IndexId value)
            : base(value)
        {
        }

        public BufferFieldIndexId(BufferField prev)
            : this(prev, IndexId.Zero)
        {
        }

        public BufferFieldIndexId(BufferField prev, IndexId value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 4;
        #endregion

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = new IndexId(reader.ReadUInt32());
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value.Value);
        }
    }
}