using System;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage
{
	[CLSCompliant(false)]
	public interface IMountableDevice
	{
		Task OpenAsync(bool isCreate);

        Task CloseAsync();
	}
}
