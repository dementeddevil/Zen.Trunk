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

		bool IsPrimary
		{
			get;
		}

		uint PageCount
		{
			get;
		}

		Task LoadBuffer(uint physicalPageId, VirtualBuffer buffer);
		Task SaveBuffer(uint physicalPageId, VirtualBuffer buffer);
		Task FlushBuffers(bool flushReads, bool flushWrites);
		uint ExpandDevice(int pageCount);
	}
}
