namespace Zen.Trunk.Storage
{
	using System;
	using System.Threading.Tasks;
	using Zen.Trunk.Storage.IO;

	[CLSCompliant(false)]
	public interface ISingleBufferDevice : IBufferDevice
	{
		string Name
		{
			get;
		}

	    uint PageCount
		{
			get;
		}

		Task LoadBufferAsync(uint physicalPageId, VirtualBuffer buffer);

        Task SaveBufferAsync(uint physicalPageId, VirtualBuffer buffer);

        Task FlushBuffersAsync(bool flushReads, bool flushWrites);

        uint ExpandDevice(int pageCount);
	}
}
