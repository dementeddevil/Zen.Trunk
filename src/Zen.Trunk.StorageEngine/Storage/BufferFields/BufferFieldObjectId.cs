using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldObjectId : SimpleBufferField<ObjectId>
    {
        public BufferFieldObjectId()
            : this(ObjectId.Zero)
        {
        }

        public BufferFieldObjectId(ObjectId value)
            : base(value)
        {
        }

        public BufferFieldObjectId(BufferField prev)
            : this(prev, ObjectId.Zero)
        {
        }

        public BufferFieldObjectId(BufferField prev, ObjectId value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 4;

        #endregion

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = new ObjectId(reader.ReadUInt32());
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value.Value);
        }
    }
}