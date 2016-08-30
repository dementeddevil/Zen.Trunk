using System;

namespace Zen.Trunk.Storage
{
	public interface IVirtualBufferFactory : IDisposable
	{
		bool IsNearlyFull
		{
			get;
		}

		int BufferSize
		{
			get;
		}

		IVirtualBuffer AllocateBuffer();
	}
}
