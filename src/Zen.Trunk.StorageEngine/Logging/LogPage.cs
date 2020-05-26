using System.IO;
using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.Logging
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Page" />
    public class LogPage : Page
    {
        #region Private Fields
        private Stream _backingStore;
        #endregion

        #region Public Properties
        /// <summary>
        /// Overridden. Gets the header size - 128 bytes
        /// </summary>
        public override uint HeaderSize => 128;

        /// <summary>
        /// Overridden. Gets the page size - 128kbytes
        /// </summary>
        public override uint PageSize => 128 * 1024;

        /// <summary>
        /// Gets the extent pages.
        /// </summary>
        /// <value>
        /// The extent pages.
        /// </value>
        public uint ExtentPages => 4;

        /// <summary>
        /// Gets the allocation pages.
        /// </summary>
        /// <value>
        /// The allocation pages.
        /// </value>
        public uint AllocationPages => 4;

        /// <summary>
        /// Gets the size of the extent.
        /// </summary>
        /// <value>
        /// The size of the extent.
        /// </value>
        public uint ExtentSize => ExtentPages * PageSize;

        /// <summary>
        /// Gets the size of the allocation.
        /// </summary>
        /// <value>
        /// The size of the allocation.
        /// </value>
        public uint AllocationSize => AllocationPages * PageSize;

        /// <summary>
        /// Gets a value indicating whether this page is attached to a new
        /// <see cref="T:BufferBase" /> object.
        /// </summary>
        public override bool IsNewPage
        {
            get
            {
                // TODO: Is this really good enough?
                return false;
            }
            internal set
            {
            }
        }
        #endregion

        #region Public Methods
        #endregion

        #region Protected Methods
        /// <summary>
        /// Overridden. Creates the header stream.
        /// </summary>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <returns></returns>
        protected override Stream CreateHeaderStream(bool readOnly)
        {
            // Return memory stream based on underlying _buffer memory
            return new SubStream(new NonClosingStream(_backingStore),
                0, HeaderSize);
        }

        /// <summary>
        /// Overridden. Creates the data stream.
        /// </summary>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <returns></returns>
        public override Stream CreateDataStream(bool readOnly)
        {
            // Return memory stream based on underlying _buffer memory
            return new SubStream(new NonClosingStream(_backingStore),
                HeaderSize, DataSize);
        }
        #endregion

        #region Internal Properties
        internal Stream BackingStore
        {
            get => _backingStore;
            set => _backingStore = value;
        }
        #endregion
    }

}
