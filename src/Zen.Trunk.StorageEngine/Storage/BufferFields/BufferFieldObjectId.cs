using Zen.Trunk.Storage.IO;

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

        protected override void OnRead(BufferReaderWriter streamManager)
        {
            Value = new ObjectId(streamManager.ReadUInt32());
        }

        protected override void OnWrite(BufferReaderWriter streamManager)
        {
            streamManager.Write(Value.Value);
        }
    }
}