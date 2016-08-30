using System.Threading.Tasks;

namespace Zen.Trunk.Storage
{
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

		Task LoadBufferAsync(uint physicalPageId, IVirtualBuffer buffer);

        Task SaveBufferAsync(uint physicalPageId, IVirtualBuffer buffer);

        Task FlushBuffersAsync(bool flushReads, bool flushWrites);

        uint ExpandDevice(int pageCount);
	}
}
