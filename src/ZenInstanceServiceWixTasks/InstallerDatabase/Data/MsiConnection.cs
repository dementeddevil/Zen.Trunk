using System;
using System.IO;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase.Data
{
    public class MsiConnection : IDisposable
	{
		private readonly string _pathName;
		private MsiDatabase _database;
		private bool _isDisposed;

		public MsiConnection(string pathName)
		{
			if (!File.Exists(pathName))
			{
				throw new FileNotFoundException(pathName + " not found.");
			}
			_pathName = pathName;
		}

		~MsiConnection()
		{
			Dispose(false);
		}

		public void Open()
		{
			CheckDisposed();
			if (_database != null)
			{
				throw new InvalidOperationException("Database already open.");
			}

			_database = new MsiDatabase(_pathName, PersistMode.ReadOnly);
		}

		public void Close()
		{
			CheckDisposed();
			if (_database != null)
			{
				_database.Close();
				_database = null;
			}
		}

		internal MsiDatabase Handle
		{
			get
			{
				CheckDisposed();
				return _database;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				_isDisposed = true;
				Close();
			}
		}

		protected void CheckDisposed()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}
	}
}
