namespace Zen.Trunk.Storage
{
	using System;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;

	[CLSCompliant(false)]
	public interface IMountableDevice
	{
		Task Open(bool isCreate);
		Task Close();
	}
}
