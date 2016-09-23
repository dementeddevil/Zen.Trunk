using System;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase.Data
{
    public class MsiDataReader : IDisposable
	{
		private MsiCommand _command;
		private MsiRecord _record;
		private bool _isDisposed;

		internal MsiDataReader(MsiCommand command)
		{
			_command = command;
		}

		public bool Read()
		{
			CheckDisposed();
			if (_command.Handle == null)
			{
				throw new InvalidOperationException("Command not ready.");
			}

			if (_record == null)
			{
				_command.Handle.Execute(null);
			}
			else
			{
				_record.Dispose();
			}

			if (!_command.Handle.Fetch(_record))
			{
				return false;
			}
			return true;
		}

		public string GetString(int column)
		{
			CheckDisposed();
			return _record[column].GetString();
		}

		public int GetInteger(int column)
		{
			CheckDisposed();
			return _record[column].GetInt();
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
				if (_record != null)
				{
					_record.Dispose();
					_record = null;
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
