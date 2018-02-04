using System;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// Defines basic information required for every transaction log entry.
    /// </summary>
    [Serializable]
    public class LogEntry : BufferFieldWrapper
    {
        #region Private Fields
        private LogEntryType _logType;

        private readonly BufferFieldUInt32 _logId;
        private readonly BufferFieldUInt32 _lastLog;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="logType">Type of the log.</param>
        public LogEntry(LogEntryType logType = LogEntryType.NoOp)
        {
            _logType = logType;
            _logId = new BufferFieldUInt32();
            _lastLog = new BufferFieldUInt32(_logId);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the raw size of this log entry record.
        /// </summary>
        /// <value>
        /// The size of this record in bytes.
        /// </value>
        public virtual uint RawSize => (uint)(TotalFieldLength + 1);

        /// <summary>
        /// Gets or sets the log identifier.
        /// </summary>
        /// <value>
        /// The log identifier.
        /// </value>
        public uint LogId
        {
            get => _logId.Value;
            set => _logId.Value = value;
        }

        /// <summary>
        /// Gets or sets the last log.
        /// </summary>
        /// <value>
        /// The last log.
        /// </value>
        public uint LastLog
        {
            get => _lastLog.Value;
            set => _lastLog.Value = value;
        }

        /// <summary>
        /// Gets the type of the log.
        /// </summary>
        /// <value>
        /// The type of the log.
        /// </value>
        public LogEntryType LogType => _logType;
        #endregion

        #region Public Methods
        /// <summary>
        /// Reads the entry.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Illegal log entry type detected.</exception>
        public static LogEntry ReadEntry(SwitchingBinaryReader streamManager)
        {
            LogEntry entry;
            var logType = ReadLogType(streamManager);
            switch (logType)
            {
                case LogEntryType.NoOp:
                    entry = new NoOpLogEntry();
                    break;
                case LogEntryType.BeginCheckpoint:
                    entry = new BeginCheckPointLogEntry();
                    break;
                case LogEntryType.EndCheckpoint:
                    entry = new EndCheckPointLogEntry();
                    break;
                case LogEntryType.BeginXact:
                    entry = new BeginTransactionLogEntry();
                    break;
                case LogEntryType.CommitXact:
                    entry = new CommitTransactionLogEntry();
                    break;
                case LogEntryType.RollbackXact:
                    entry = new RollbackTransactionLogEntry();
                    break;
                case LogEntryType.CreatePage:
                    entry = new PageImageCreateLogEntry();
                    break;
                case LogEntryType.ModifyPage:
                    entry = new PageImageUpdateLogEntry();
                    break;
                case LogEntryType.DeletePage:
                    entry = new PageImageDeleteLogEntry();
                    break;
                default:
                    throw new InvalidOperationException("Illegal log entry type detected.");
            }
            entry._logType = logType;
            entry.Read(streamManager);
            return entry;
        }

        /// <summary>
        /// Performs the rollforward action on the associated page buffer from 
        /// this log record state.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <returns></returns>
        public virtual Task RollForward(DatabaseDevice device)
        {
            return CompletedTask.Default;
        }

        /// <summary>
        /// Performs the rollback action on the associated page buffer from 
        /// this log record state.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <returns></returns>
        public virtual Task RollBack(DatabaseDevice device)
        {
            return CompletedTask.Default;
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the first buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField FirstField => _logId;

        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _lastLog;
        #endregion

        #region Protected Methods
        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            writer.Write((byte)(int)_logType);
            base.OnWrite(writer);
        }
        #endregion

        #region Private Methods
        private static LogEntryType ReadLogType(SwitchingBinaryReader streamManager)
        {
            return (LogEntryType)streamManager.ReadByte();
        }
        #endregion
    }
}
