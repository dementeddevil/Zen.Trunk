using System;
using System.Collections.Generic;
using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.Logging
{
    /// <summary>
    /// Serves as a base class for a check-point log record.
    /// </summary>
    /// <remarks>
    /// Check-point records are used to ensure recovery does not take an
    /// excessive amount of time to be performed. During checkpointing
    /// the state of all open transactions is written to disk and all
    /// buffers which can be written to the backing store are written.
    /// </remarks>
    [Serializable]
    public class CheckPointLogEntry : LogEntry
    {
        #region Private Fields
        private List<ActiveTransaction> _activeTransactions;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CheckPointLogEntry"/> class.
        /// </summary>
        /// <param name="logType">Type of the log.</param>
        /// <exception cref="ArgumentException">Invalid log entry type for checkpoint record.</exception>
        protected CheckPointLogEntry(LogEntryType logType)
            : base(logType)
        {
            // Sanity check
            if (logType != LogEntryType.BeginCheckpoint &&
                logType != LogEntryType.EndCheckpoint)
            {
                throw new ArgumentException("Invalid log entry type for checkpoint record.");
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the size of the raw.
        /// </summary>
        /// <value>
        /// The size of the raw.
        /// </value>
        public override uint RawSize
        {
            get
            {
                var rawSize = base.RawSize + 2;
                if (_activeTransactions != null)
                {
                    rawSize += (uint)(_activeTransactions.Count * 14);
                }
                return rawSize;
            }
        }

        /// <summary>
        /// Gets the transaction count.
        /// </summary>
        /// <value>
        /// The transaction count.
        /// </value>
        public int TransactionCount
        {
            get
            {
                if (_activeTransactions == null)
                {
                    return 0;
                }
                return _activeTransactions.Count;
            }
        }

        /// <summary>
        /// Gets the active transactions.
        /// </summary>
        /// <value>
        /// The active transactions.
        /// </value>
        public IEnumerable<ActiveTransaction> ActiveTransactions => _activeTransactions;

        /// <summary>
        /// Gets the first protected transaction.
        /// </summary>
        /// <value>
        /// The first protected transaction.
        /// </value>
        /// <exception cref="InvalidOperationException">Active transactions is null.</exception>
        public ActiveTransaction FirstProtectedTransaction
        {
            get
            {
                if (_activeTransactions == null)
                {
                    throw new InvalidOperationException("Active transactions is null.");
                }

                ActiveTransaction firstProtectedTransaction = null;
                foreach (var activeTransaction in _activeTransactions)
                {
                    if (firstProtectedTransaction == null || firstProtectedTransaction.FirstLogId > activeTransaction.FirstLogId)
                    {
                        firstProtectedTransaction = activeTransaction;
                    }
                }
                return firstProtectedTransaction;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the transaction at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Active transactions is null.</exception>
        public ActiveTransaction GetTransactionAt(int index)
        {
            if (_activeTransactions == null)
            {
                throw new InvalidOperationException("Active transactions is null.");
            }

            return _activeTransactions[index];
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            base.OnWrite(writer);

            writer.Write((ushort)TransactionCount);
            for (var index = 0; index < TransactionCount; ++index)
            {
                _activeTransactions[index].Write(writer);
            }
        }

        /// <summary>
        /// Reads the field chain from the specified stream manager.
        /// </summary>
        /// <param name="reader">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnRead(SwitchingBinaryReader reader)
        {
            base.OnRead(reader);

            var count = reader.ReadUInt16();
            if (count > 0)
            {
                _activeTransactions = new List<ActiveTransaction>();
                for (var index = 0; index < count; ++index)
                {
                    var tran = new ActiveTransaction();
                    tran.Read(reader);
                    _activeTransactions.Add(tran);
                }
            }
        }
        #endregion

        #region Internal Methods
        internal void UpdateTransactions(List<ActiveTransaction> active)
        {
            // Add transactions to object.
            if (_activeTransactions != null)
            {
                throw new InvalidOperationException("Active transaction collection already set.");
            }
            _activeTransactions = active;
        }
        #endregion
    }
}