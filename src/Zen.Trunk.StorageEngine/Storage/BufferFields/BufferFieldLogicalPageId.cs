using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldLogicalPageId : SimpleBufferField<LogicalPageId>
    {
        public BufferFieldLogicalPageId()
            : this(LogicalPageId.Zero)
        {
        }

        public BufferFieldLogicalPageId(LogicalPageId value)
            : base(value)
        {
        }

        public BufferFieldLogicalPageId(BufferField prev)
            : this(prev, LogicalPageId.Zero)
        {
        }

        public BufferFieldLogicalPageId(BufferField prev, LogicalPageId value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 8;

        #endregion

        protected override void OnRead(SwitchingBinaryReader streamManager)
        {
            Value = new LogicalPageId(streamManager.ReadUInt64());
        }

        protected override void OnWrite(SwitchingBinaryWriter streamManager)
        {
            streamManager.Write(Value.Value);
        }
    }
}