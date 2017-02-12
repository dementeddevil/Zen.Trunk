using System;
using System.IO;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>IVirtualBuffer</c> defines the contract for virtual buffer objects.
    /// </summary>
    /// <seealso cref="System.IComparable{IVirtualBuffer}" />
    /// <seealso cref="System.IDisposable" />
    public interface IVirtualBuffer : IComparable<IVirtualBuffer>, IDisposable
    {
        /// <summary>
        /// Gets the buffer identifier.
        /// </summary>
        /// <value>
        /// The buffer identifier.
        /// </value>
        string BufferId { get; }

        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <value>
        /// The size of the buffer.
        /// </value>
        int BufferSize { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is dirty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is dirty; otherwise, <c>false</c>.
        /// </value>
        bool IsDirty { get; }

        /// <summary>
        /// Clears the dirty.
        /// </summary>
        void ClearDirty();

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="destination">The destination.</param>
        void CopyTo(IVirtualBuffer destination);

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        void CopyTo(byte[] buffer);

        /// <summary>
        /// Gets the buffer stream.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        /// <returns></returns>
        Stream GetBufferStream(int offset, int count, bool writable);

        /// <summary>
        /// Initializes from.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        void InitFrom(byte[] buffer);

        /// <summary>
        /// Sets the dirty.
        /// </summary>
        void SetDirty();
    }
}