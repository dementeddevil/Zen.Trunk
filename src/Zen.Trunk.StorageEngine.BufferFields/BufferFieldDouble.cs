using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldDouble : SimpleBufferField<double>
    {
        public BufferFieldDouble()
        {
        }

        public BufferFieldDouble(double value)
            : base(value)
        {
        }

        public BufferFieldDouble(BufferField prev)
            : base(prev)
        {
        }

        public BufferFieldDouble(BufferField prev, double value)
            : base(prev, value)
        {
        }

        public override int DataSize => 8;

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = reader.ReadDouble();
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value);
        }
    }
}