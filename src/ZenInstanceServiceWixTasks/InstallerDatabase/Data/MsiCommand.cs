using System;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase.Data
{
    public class MsiCommand : IDisposable
	{
		private MsiView _view;
		private string _command;
		private MsiConnection _connection;
		private bool _isDisposed;

		public MsiCommand(string command, MsiConnection connection)
		{
			_command = command;
			_connection = connection;
		}

		~MsiCommand()
		{
			Dispose(false);
		}

		public MsiDataReader ExecuteReader()
		{
			CheckDisposed();
			if (_connection.Handle == null)
			{
				throw new InvalidOperationException("Connection not opened.");
			}
			_view = _connection.Handle.OpenView(_command);
			return new MsiDataReader(this);
		}

		internal MsiView Handle
		{
			get
			{
				CheckDisposed();
				return _view;
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
				if (_view != null)
				{
					_view.Close();
					_view = null;
				}
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
