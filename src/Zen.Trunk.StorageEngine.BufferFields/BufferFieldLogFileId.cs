using Zen.Trunk.IO;
using Zen.Trunk.Storage.Logging;

namespace Zen.Trunk.Storage.BufferFields
{
    public class BufferFieldLogFileId : SimpleBufferField<LogFileId>
    {
        public BufferFieldLogFileId()
            : this(LogFileId.Zero)
        {
        }

        public BufferFieldLogFileId(LogFileId value)
            : base(value)
        {
        }

        public BufferFieldLogFileId(BufferField prev)
            : this(prev, LogFileId.Zero)
        {
        }

        public BufferFieldLogFileId(BufferField prev, LogFileId value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 4;
        #endregion

        protected override void OnRead(SwitchingBinaryReader reader)
        {
            Value = new LogFileId(reader.ReadUInt32());
        }

        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write(Value.FileId);
        }
    }
}