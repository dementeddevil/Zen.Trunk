// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IProvideReadStreamEvents.cs" company="Zen Design Software">
//   © Zen Design Software 2015
// </copyright>
// <summary>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace Zen.Trunk.IO
{
    /// <summary>
    /// <c>IProvideReadStreamEvents</c> interface exposes stream events.
    /// </summary>
    public interface IProvideReadStreamEvents
    {
        /// <summary>
        /// Gets a value indicating whether the entire stream has been read.
        /// </summary>
        /// <value>
        /// <c>true</c> if stream reading has been completed; otherwise, <c>false</c>.
        /// </value>
        bool ReadCompleted { get; }

        /// <summary>
        /// Occurs just before the stream is first read.
        /// </summary>
        event EventHandler BeforeFirstReadEvent;

        /// <summary>
        /// Occurs when the stream read.
        /// </summary>
        event EventHandler<ReadEventArgs> ReadEvent;

        /// <summary>
        /// Occurs just after EOF is read from the stream.
        /// </summary>
        event EventHandler AfterLastReadEvent;
    }

    /// <summary>
    /// </summary>
    public class ReadEventArgs : EventArgs
    {
        #region Public Constructors
        /// <summary>
        /// Initialises a new instance of the <see cref="ReadEventArgs" /> class.
        /// </summary>
        /// <param name="position">The position.</param>
        public ReadEventArgs(long position)
        {
            Position = position;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="ReadEventArgs" /> class.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="buffer">The buffer.</param>
        public ReadEventArgs(long position, byte[] buffer)
        {
            Position = position;
            Buffer = buffer;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="ReadEventArgs" /> class.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        public ReadEventArgs(long position, byte[] buffer, int offset, int count)
        {
            Position = position;
            Buffer = buffer;
            Offset = offset;
            Count = count;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the position.
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        public long Position { get; private set; }

        /// <summary>
        /// Gets the buffer.
        /// </summary>
        /// <value>
        /// The buffer.
        /// </value>
        public byte[] Buffer { get; private set; }

        /// <summary>
        /// Gets the offset.
        /// </summary>
        /// <value>
        /// The offset.
        /// </value>
        public int Offset { get; private set; }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count { get; private set; }
        #endregion
    }
}