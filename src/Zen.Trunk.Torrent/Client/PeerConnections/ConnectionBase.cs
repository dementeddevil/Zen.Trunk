namespace Zen.Trunk.Torrent.Client.Connections
{
	using System;
	using System.Net;
	using System.Threading.Tasks;

	public abstract class ConnectionBase : IConnection
	{
		private bool _disposed;

		protected ConnectionBase()
		{
		}

		public abstract byte[] AddressBytes
		{
			get;
		}

		public abstract bool Connected
		{
			get;
		}

		public abstract bool CanReconnect
		{
			get;
		}

		public abstract bool IsIncoming
		{
			get;
		}

		public abstract EndPoint EndPoint
		{
			get;
		}

		public abstract Uri Uri
		{
			get;
		}

		public abstract Task ConnectAsync();

		public abstract Task<int> ReceiveAsync(byte[] buffer, int offset, int count);

		public abstract Task<int> SendAsync(byte[] buffer, int offset, int count);

		public void Dispose()
		{
			DisposeManagedObjects();
		}

		protected void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}

		protected virtual void DisposeManagedObjects()
		{
			_disposed = true;
		}
	}
}
