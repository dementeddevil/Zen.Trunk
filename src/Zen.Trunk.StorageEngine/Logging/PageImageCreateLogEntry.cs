using System;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Logging
{
    /// <summary>
    /// Defines a log entry for creating or allocating a DataBuffer page.
    /// </summary>
    [Serializable]
    public class PageImageCreateLogEntry : PageLogEntry
    {
        #region Private Fields
        private byte[] _image;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PageImageCreateLogEntry"/> class.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="virtualPageId">The virtual page id.</param>
        /// <param name="timestamp">The timestamp.</param>
        public PageImageCreateLogEntry(
            IVirtualBuffer buffer,
            ulong virtualPageId,
            long timestamp)
            : base(virtualPageId, timestamp, LogEntryType.CreatePage)
        {
            _image = new byte[buffer.BufferSize];
            buffer.CopyTo(_image);
        }

        internal PageImageCreateLogEntry()
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
        public override uint RawSize => (uint)(base.RawSize + _image.Length);

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <value>
        /// The image.
        /// </value>
        public byte[] Image => _image;

        #endregion

        #region Protected Methods
        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnWrite(SwitchingBinaryWriter writer)
        {
            base.OnWrite(writer);
            writer.Write(_image);
        }

        /// <summary>
        /// Reads the field chain from the specified stream manager.
        /// </summary>
        /// <param name="reader">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnRead(SwitchingBinaryReader reader)
        {
            base.OnRead(reader);
            _image = reader.ReadBytes(8192);
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
            using (var stream = dataBuffer.GetBufferStream(0, 8192, false))
            {
                // NOTE: The before image in this case is empty
                var initStream = new byte[8192];
                stream.Write(initStream, 0, 8192);
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
            using (var stream = dataBuffer.GetBufferStream(0, 8192, false))
            {
                stream.Write(_image, 0, 8192);
                stream.Flush();
            }

            // Mark DataBuffer as dirty
            dataBuffer.SetDirtyAsync();
        }
        #endregion
    }
}