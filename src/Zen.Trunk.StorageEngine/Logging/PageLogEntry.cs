using System;
using System.Threading.Tasks;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Logging
{
    /// <summary>
    /// Serves as a base class for transacted operations against a database
    /// page.
    /// </summary>
    /// <remarks>
    /// For a given transaction Id these records must only occur between
    /// a <see cref="T:BeginTransactionLogEntry"/> record and either a
    /// <see cref="T:CommitTransactionLogEntry"/> or 
    /// <see cref="T:RollbackTransactionLogEntry"/> record.
    /// </remarks>
    [Serializable]
    public abstract class PageLogEntry : TransactionLogEntry
    {
        #region Private Fields
        private readonly BufferFieldUInt64 _virtualPageId;
        private readonly BufferFieldInt64 _timestamp;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PageLogEntry" /> class.
        /// </summary>
        /// <param name="virtualPageId">The virtual page id.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="logType">Type of the log.</param>
        protected PageLogEntry(
            ulong virtualPageId,
            long timestamp,
            LogEntryType logType)
            : base(logType)
        {
            _virtualPageId = new BufferFieldUInt64(base.LastField, virtualPageId);
            _timestamp = new BufferFieldInt64(_virtualPageId, timestamp);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageLogEntry"/> class.
        /// </summary>
        protected PageLogEntry()
        {
            _virtualPageId = new BufferFieldUInt64(base.LastField);
            _timestamp = new BufferFieldInt64(_virtualPageId);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the virtual page id.
        /// </summary>
        /// <value>The virtual page id.</value>
        public VirtualPageId VirtualPageId => new VirtualPageId(_virtualPageId.Value);

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        /// <value>The timestamp.</value>
        public long Timestamp => _timestamp.Value;
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the last field.
        /// </summary>
        /// <value>The last field.</value>
        protected override BufferField LastField => _timestamp;
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs the rollback action on the associated page buffer from
        /// this log record state.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <returns></returns>
        public override async Task RollBack(DatabaseDevice device)
        {
            var page = await LoadPageFromDevice(device);

            if (page.Timestamp != _timestamp.Value)
            {
                OnUndoChanges(page.DataBuffer);
            }
        }

        /// <summary>
        /// Performs the rollforward action on the associated page buffer from
        /// this log record state.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <returns></returns>
        public override async Task RollForward(DatabaseDevice device)
        {
            var page = await LoadPageFromDevice(device);

            if (page.Timestamp != _timestamp.Value)
            {
                OnRedoChanges(page.DataBuffer);
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// <b>OnUndoChanges</b> is called during recovery to undo DataBuffer
        /// changes to the given page object.
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <remarks>
        /// This method is only called if a mismatch in timestamps has
        /// been detected.
        /// </remarks>
        protected abstract void OnUndoChanges(IPageBuffer dataBuffer);

        /// <summary>
        /// <b>OnRedoChanges</b> is called during recovery to redo DataBuffer
        /// changes to the given page object.
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <remarks>
        /// This method is only called if a mismatch in timestamps has
        /// been detected.
        /// </remarks>
        protected abstract void OnRedoChanges(IPageBuffer dataBuffer);
        #endregion

        #region Private Methods
        private async Task<IDataPage> LoadPageFromDevice(DatabaseDevice device)
        {
            var page = 
                new DataPage
                {
                    VirtualPageId = VirtualPageId,
                    FileGroupId = FileGroupId.Invalid
                };

            await device
                .LoadFileGroupPageAsync(new LoadFileGroupPageParameters(null, page, true))
                .ConfigureAwait(false);

            return page;
        }
        #endregion
    }
}