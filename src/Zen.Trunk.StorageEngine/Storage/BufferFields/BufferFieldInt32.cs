using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldInt32 : SimpleBufferField<int>
    {
        public BufferFieldInt32()
        {
        }

        public BufferFieldInt32(int value)
            : base(value)
        {
        }

        public BufferFieldInt32(BufferField prev)
            : base(prev)
        {
        }

        public BufferFieldInt32(BufferField prev, int value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 4;

        #endregion

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = reader.ReadInt32();
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value);
        }
    }
}