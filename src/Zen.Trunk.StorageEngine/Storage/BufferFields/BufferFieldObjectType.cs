using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldObjectType : SimpleBufferField<ObjectType>
    {
        public BufferFieldObjectType()
            : this(ObjectType.Unknown)
        {
        }

        public BufferFieldObjectType(ObjectType value)
            : base(value)
        {
        }

        public BufferFieldObjectType(BufferField prev)
            : this(prev, ObjectType.Unknown)
        {
        }

        public BufferFieldObjectType(BufferField prev, ObjectType value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 1;

        #endregion

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = new ObjectType(reader.ReadByte());
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value.Value);
        }
    }
}