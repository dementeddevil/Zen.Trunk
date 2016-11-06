using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldInt16 : SimpleBufferField<short>
    {
        public BufferFieldInt16()
        {
        }

        public BufferFieldInt16(short value)
            : base(value)
        {
        }

        public BufferFieldInt16(BufferField prev)
            : base(prev)
        {
        }

        public BufferFieldInt16(BufferField prev, short value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 2;

        #endregion

        protected override void OnRead(BufferReaderWriter streamManager)
        {
            Value = streamManager.ReadInt16();
        }

        protected override void OnWrite(BufferReaderWriter streamManager)
        {
            streamManager.Write(Value);
        }
    }
}