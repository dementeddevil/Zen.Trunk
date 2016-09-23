using System;
using Zen.Tasks.Wix.InstanceService.InstallerDatabase.Data;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase.Linq
{
    public class MsiQueryProvider : IDisposable
	{
		private MsiConnection _connection;

		public MsiQueryProvider(string pathName)
		{
			_connection = new MsiConnection(pathName);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
		    _connection?.Dispose();
		    _connection = null;
		}
	}
}