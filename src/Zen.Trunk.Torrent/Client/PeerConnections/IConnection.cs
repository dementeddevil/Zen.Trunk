namespace Zen.Trunk.Torrent.Client.Connections
{
	using System;
	using System.Net;
	using System.Threading.Tasks;

	public interface IConnection : IDisposable
	{
		byte[] AddressBytes
		{
			get;
		}

		bool Connected
		{
			get;
		}

		bool CanReconnect
		{
			get;
		}

		bool IsIncoming
		{
			get;
		}

		EndPoint EndPoint
		{
			get;
		}

		Uri Uri
		{
			get;
		}

		Task ConnectAsync();

		Task<int> ReceiveAsync(byte[] buffer, int offset, int count);

		Task<int> SendAsync(byte[] buffer, int offset, int count);
	}
}
