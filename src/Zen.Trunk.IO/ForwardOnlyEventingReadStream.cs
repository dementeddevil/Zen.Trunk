// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ForwardOnlyEventingReadStream.cs" company="Zen Design Software">
//   © Zen Design Software 2015
// </copyright>
// <summary>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace Zen.Trunk.IO
{
    /// <summary>
    /// <c>ForwardOnlyEventingReadStream</c> extends
    /// <see cref="EventingReadStream" />
    /// to limit seeking ability.
    /// </summary>
    public abstract class ForwardOnlyEventingReadStream : EventingReadStream
    {
        #region Public Properties
        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// true if the stream supports reading; otherwise, false.
        /// </returns>
        public override bool CanRead => true;

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// true if the stream supports seeking; otherwise, false.
        /// </returns>
        public override bool CanSeek => false;

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        /// <value></value>
        /// <returns>The current position within the stream.</returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// The stream does not support
        /// seeking.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// Methods were called after
        /// the stream was closed.
        /// </exception>
        public override long Position
        {
            set
            {
                if (value != Position)
                {
                    throw new NotSupportedException("ForwardOnlyEventingReadStream does not support setting Position.");
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">
        /// A byte offset relative to the <paramref name="origin" />
        /// parameter.
        /// </param>
        /// <param name="origin">
        /// A value of type <see cref="T:System.IO.SeekOrigin" />
        /// indicating the reference point used to obtain the new position.
        /// </param>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        /// <exception cref="System.ArgumentException">origin</exception>
        /// <exception cref="System.NotSupportedException">
        /// ForwardOnlyEventingReadStream
        /// does not support Seek().
        /// </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = -1;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = Position + offset;
                    break;
                case SeekOrigin.End:
                    break;
                default:
                    throw new ArgumentException("origin");
            }

            if (newPosition != Position)
            {
                throw new NotSupportedException("ForwardOnlyEventingReadStream does not support Seek().");
            }

            return newPosition;
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="System.NotSupportedException">
        /// ForwardOnlyEventingReadStream
        /// does not support SetLength().
        /// </exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("ForwardOnlyEventingReadStream does not support SetLength().");
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be
        /// written to the underlying device.
        /// </summary>
        /// <exception cref="System.NotSupportedException">
        /// ForwardOnlyEventingReadStream
        /// does not support Flush().
        /// </exception>
        public override void Flush()
        {
            throw new NotSupportedException("ForwardOnlyEventingReadStream does not support Flush().");
        }
        #endregion
    }
}