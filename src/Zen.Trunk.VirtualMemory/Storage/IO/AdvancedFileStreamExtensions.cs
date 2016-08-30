namespace Zen.Trunk.Storage.IO
{
	using System.IO;
	using System.Threading.Tasks;

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
