// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NonClosingStream.cs" company="Zen Design Software">
//   © Zen Design Software 2015
// </copyright>
// <summary>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;

namespace Zen.Trunk.IO
{
    /// <summary>
    /// <c>NonClosingStream</c> is a wrapper stream that never closes or
    /// disposes of the underlying stream.
    /// </summary>
    public class NonClosingStream : DelegatingStream
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="NonClosingStream" /> class.
        /// </summary>
        /// <param name="innerStream">The inner stream.</param>
        public NonClosingStream(Stream innerStream)
            : base(innerStream)
        {
        }

        /// <summary>
        /// Releases the unmanaged resources used by the
        /// <see cref="T:System.IO.Stream" /> and optionally releases the managed
        /// resources.
        /// </summary>
        /// <param name="disposing">
        /// true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            InvalidateInnerStream();
            base.Dispose(disposing);
        }
    }
}