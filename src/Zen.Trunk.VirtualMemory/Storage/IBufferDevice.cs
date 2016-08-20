namespace Zen.Trunk.Storage
{
	using System;
	using System.Threading.Tasks;

	[CLSCompliant(false)]
	public interface IBufferDevice : IDisposable
	{
		IVirtualBufferFactory BufferFactory
		{
			get;
		}

		Task OpenAsync();

		Task CloseAsync();
	}
}
