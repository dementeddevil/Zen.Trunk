// -----------------------------------------------------------------------
// <copyright file="StatefulBufferScope.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.Storage
{
	using System;

	/// <summary>
	/// <c>StatefulBufferScope</c> is used to manage the lifetime of a buffer.
	/// </summary>
	public class StatefulBufferScope<TBufferType> : IDisposable
		where TBufferType : StatefulBuffer
	{
		private TBufferType _buffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatefulBufferScope{TBufferType}"/> class.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        public StatefulBufferScope(TBufferType buffer)
		{
			_buffer = buffer;
		}

        /// <summary>
        /// Gets the buffer.
        /// </summary>
        /// <value>
        /// The buffer.
        /// </value>
        public TBufferType Buffer => _buffer;

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
			if (_buffer != null)
			{
				_buffer.Release();
				_buffer = null;
			}
		}
	}
}
