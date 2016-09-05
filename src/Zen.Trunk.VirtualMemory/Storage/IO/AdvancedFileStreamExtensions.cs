using System.Threading.Tasks;

namespace Zen.Trunk.Storage.IO
{
	public static class AdvancedFileStreamExtensions
	{
		public static Task WriteGatherAsync(
			this AdvancedFileStream stream, IVirtualBuffer[] buffers)
		{
			return Task.Factory.FromAsync(
				stream.BeginWriteGather, stream.EndWriteGather, buffers, null);
		}

		public static Task<int> ReadScatterAsync(
			this AdvancedFileStream stream, IVirtualBuffer[] buffers)
		{
			return Task<int>.Factory.FromAsync(
				stream.BeginReadScatter, stream.EndReadScatter, buffers, null);
		}
	}
}
