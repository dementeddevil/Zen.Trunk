using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldInt64 : SimpleBufferField<long>
    {
        public BufferFieldInt64()
        {
        }

        public BufferFieldInt64(long value)
            : base(value)
        {
        }

        public BufferFieldInt64(BufferField prev)
            : base(prev)
        {
        }

        public BufferFieldInt64(BufferField prev, long value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 8;

        #endregion

        protected override void OnRead(BufferReaderWriter streamManager)
        {
            Value = streamManager.ReadInt64();
        }

        protected override void OnWrite(BufferReaderWriter streamManager)
        {
            streamManager.Write(Value);
        }
    }
}