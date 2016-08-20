namespace Zen.Trunk.Storage
{
	using System;
	using Zen.Trunk.Storage.IO;

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

		VirtualBuffer AllocateBuffer();
	}
}
