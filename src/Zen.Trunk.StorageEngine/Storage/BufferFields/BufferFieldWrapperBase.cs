using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    /// <summary>
    /// <c>BufferFieldWrapperBase</c> is an abstract class used to wrap a chain
    /// of <see cref="T:BufferField"/> derived classes.
    /// </summary>
    public class BufferFieldWrapperBase
    {
        #region Private Fields
        private bool _gotTotalLength;
        private int _totalLength;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BufferFieldWrapperBase"/> class.
        /// </summary>
        protected BufferFieldWrapperBase()
        {
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the total length of the field chain.
        /// </summary>
        /// <value>The total length of the field.</value>
        public int TotalFieldLength
        {
            get
            {
                if (!_gotTotalLength)
                {
                    _totalLength = 0;
                    var field = FirstField;
                    while (field != null)
                    {
                        _totalLength += field.FieldLength;
                        field = field.NextField;
                    }
                    _gotTotalLength = true;
                }
                return _totalLength;
            }
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the first buffer field object.
        /// </summary>
        /// <value>A <see cref="T:BufferField"/> object.</value>
        protected virtual BufferField FirstField => null;

        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>A <see cref="T:BufferField"/> object.</value>
        protected virtual BufferField LastField => null;

        #endregion

        #region Protected Methods
        /// <summary>
        /// Reads the field chain from the specified stream manager.
        /// </summary>
        /// <param name="streamManager">A <see cref="T:BufferReaderWriter"/> object.</param>
        protected virtual void DoRead(BufferReaderWriter streamManager)
        {
            if (FirstField != null)
            {
                FirstField.Read(streamManager);
            }
        }

        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="streamManager">A <see cref="T:BufferReaderWriter"/> object.</param>
        protected virtual void DoWrite(BufferReaderWriter streamManager)
        {
            if (FirstField != null)
            {
                FirstField.Write(streamManager);
            }
        }
        #endregion
    }
}