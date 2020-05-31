// -----------------------------------------------------------------------
// <copyright file="StatefulBufferScope.cs" company="Zen Design Software">
// © Zen Design Software 2009 - 2016
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Zen.Trunk.Storage
{
	/// <summary>
	/// <c>StatefulBufferScope</c> is used to manage the lifetime of a buffer.
	/// </summary>
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public class StatefulBufferScope<TBufferType> : IDisposable
		where TBufferType : class, IStatefulBuffer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatefulBufferScope{TBufferType}"/> class.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        public StatefulBufferScope(TBufferType buffer)
		{
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

			Buffer = buffer;
		}

        /// <summary>
        /// Gets the buffer.
        /// </summary>
        /// <value>
        /// The buffer.
        /// </value>
        public TBufferType Buffer { get; private set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
		{
			Dispose(true);
            GC.SuppressFinalize(this);
		}

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
		{
			if (disposing && Buffer != null)
			{
				Buffer.Release();
				Buffer = null;
			}
		}
	}
}
