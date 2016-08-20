// -----------------------------------------------------------------------
// <copyright file="StatefulBufferScope.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.Storage
{
	using System;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class StatefulBufferScope<TBufferType> : IDisposable
		where TBufferType : StatefulBuffer
	{
		private TBufferType _buffer;

		public StatefulBufferScope(TBufferType buffer)
		{
			_buffer = buffer;
		}

		public TBufferType Buffer
		{
			get
			{
				return _buffer;
			}
		}

		public void Dispose()
		{
			DisposeManagedObjects();
		}

		protected virtual void DisposeManagedObjects()
		{
			if (_buffer != null)
			{
				_buffer.Release();
				_buffer = null;
			}
		}
	}
}
