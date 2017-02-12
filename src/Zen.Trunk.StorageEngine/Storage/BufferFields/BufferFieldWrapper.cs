using System.IO;
using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    /// <summary>
    /// <c>BufferFieldWrapper</c> extends <see cref="T:BufferFieldWrapperBase"/>
    /// by adding public persistence operators and providing a linkage for
    /// reading from a <see cref="T:BufferBase"/> instance.
    /// </summary>
    public class BufferFieldWrapper : BufferFieldWrapperBase
    {
        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BufferFieldWrapper"/> class.
        /// </summary>
        protected BufferFieldWrapper()
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Reads the specified stream manager.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        public void Read(SwitchingBinaryReader streamManager)
        {
            OnRead(streamManager);
        }

        /// <summary>
        /// Writes the specified stream manager.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        public void Write(SwitchingBinaryWriter streamManager)
        {
            OnWrite(streamManager);
        }
        #endregion

        #region Internal Methods
        internal void ReadFrom(Stream stream)
        {
            using (var streamManager = new SwitchingBinaryReader(stream))
            {
                Read(streamManager);
            }
        }
        #endregion
    }
}