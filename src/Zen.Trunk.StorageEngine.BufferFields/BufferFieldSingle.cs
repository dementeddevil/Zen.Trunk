using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldSingle : SimpleBufferField<float>
    {
        public BufferFieldSingle()
        {
        }

        public BufferFieldSingle(float value)
            : base(value)
        {
        }

        public BufferFieldSingle(BufferField prev)
            : base(prev)
        {
        }

        public BufferFieldSingle(BufferField prev, float value)
            : base(prev, value)
        {
        }

        public override int DataSize => 4;

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = reader.ReadSingle();
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value);
        }
    }
}