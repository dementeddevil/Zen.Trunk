using System;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Logging
{
    /// <summary>
    /// Defines a log entry for updating an existing DataBuffer page.
    /// </summary>
    [Serializable]
    public class PageImageUpdateLogEntry : PageLogEntry
    {
        #region Private Fields
        private byte[] _beforeImage;
        private byte[] _afterImage;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PageImageUpdateLogEntry"/> class.
        /// </summary>
        /// <param name="before">The before.</param>
        /// <param name="after">The after.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="timestamp">The timestamp.</param>
        public PageImageUpdateLogEntry(
            IVirtualBuffer before,
            IVirtualBuffer after,
            ulong virtualPageId,
            long timestamp)
            : base(virtualPageId, timestamp, LogEntryType.ModifyPage)
        {
            _beforeImage = new byte[StorageConstants.PageBufferSize];
            _afterImage = new byte[StorageConstants.PageBufferSize];

            before.CopyTo(_beforeImage);
            after.CopyTo(_afterImage);
        }

        internal PageImageUpdateLogEntry()
        {
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the raw size of this log entry record.
        /// </summary>
        /// <value>
        /// The size of this record in bytes.
        /// </value>
        public override uint RawSize => base.RawSize + (2 * StorageConstants.PageBufferSize);

        /// <summary>
        /// Gets the before image.
        /// </summary>
        /// <value>
        /// The before image.
        /// </value>
        public byte[] BeforeImage => _beforeImage;

        /// <summary>
        /// Gets the after image.
        /// </summary>
        /// <value>
        /// The after image.
        /// </value>
        public byte[] AfterImage => _afterImage;
        #endregion

        #region Protected Methods
        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            base.OnWrite(writer);
            writer.Write(_beforeImage);
            writer.Write(_afterImage);
        }

        /// <summary>
        /// Reads the field chain from the specified stream manager.
        /// </summary>
        /// <param name="reader">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnRead(SwitchingBinaryReader reader)
        {
            base.OnRead(reader);
            _beforeImage = reader.ReadBytes(StorageConstants.PageBufferSize);
            _afterImage = reader.ReadBytes(StorageConstants.PageBufferSize);
        }

        /// <summary>
        /// <b>OnUndoChanges</b> is called during recovery to undo DataBuffer
        /// changes to the given page object.
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <remarks>
        /// This method is only called if a mismatch in timestamps has
        /// been detected.
        /// </remarks>
        protected override void OnUndoChanges(IPageBuffer dataBuffer)
        {
            // Copy before image into page DataBuffer
            using (var stream = dataBuffer.GetBufferStream(0, StorageConstants.PageBufferSize, false))
            {
                stream.Write(_beforeImage, 0, StorageConstants.PageBufferSize);
                stream.Flush();
            }

            // Mark DataBuffer as dirty
            dataBuffer.SetDirtyAsync();
        }

        /// <summary>
        /// <b>OnRedoChanges</b> is called during recovery to redo DataBuffer
        /// changes to the given page object.
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <remarks>
        /// This method is only called if a mismatch in timestamps has
        /// been detected.
        /// </remarks>
        protected override void OnRedoChanges(IPageBuffer dataBuffer)
        {
            // Copy after image into page DataBuffer
            using (var stream = dataBuffer.GetBufferStream(0, StorageConstants.PageBufferSize, false))
            {
                stream.Write(_afterImage, 0, StorageConstants.PageBufferSize);
                stream.Flush();
            }

            // Mark DataBuffer as dirty
            dataBuffer.SetDirtyAsync();
        }
        #endregion
    }
}