// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FixedLengthStream.cs" company="Zen Design Software">
//   © Zen Design Software 2015
// </copyright>
// <summary>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;

namespace Zen.Streaming
{
    /// <summary>
    /// </summary>
    public class FixedLengthStream : DelegatingStream
    {
        private long _length;

        /// <summary>
        /// Initialises a new instance of the <see cref="FixedLengthStream" /> class.
        /// </summary>
        /// <param name="innerStream">The inner stream.</param>
        /// <param name="length">The length.</param>
        public FixedLengthStream(Stream innerStream, long length)
            : base(innerStream)
        {
            _length = length;
        }

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        public override long Length => _length;

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            base.SetLength(value);
            _length = value;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the
        /// <see cref="T:System.IO.Stream" /> and optionally releases the managed
        /// resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            _length = 0;
            base.Dispose(disposing);
        }
    }
}