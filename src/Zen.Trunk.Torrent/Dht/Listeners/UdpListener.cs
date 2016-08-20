namespace Zen.Trunk.Torrent.Dht.Listeners
{
	using System;
	using System.Net;
	using System.Net.Sockets;
	using Zen.Trunk.Torrent.Common;

	public class UdpListener : DhtListener
	{
		private UdpClient client;

		public UdpListener(IPEndPoint endpoint)
			:base(endpoint)
		{
			
		}

		private void EndReceive(IAsyncResult result)
		{
			try
			{
				IPEndPoint e = new IPEndPoint(IPAddress.Any, LocalEndPoint.Port);
				byte[] buffer = client.EndReceive(result, ref e);

				RaiseMessageReceived(buffer, e);
				client.BeginReceive(EndReceive, null);
			}
			catch (ObjectDisposedException)
			{
				// Ignore, we're finished!
			}
			catch (SocketException ex)
			{
				// If the destination computer closes the connection
				// we get error code 10054. We need to keep receiving on
				// the socket until we clear all the error states
				if (ex.ErrorCode == 10054)
				{
					while (true)
					{
						try
						{
							client.BeginReceive(EndReceive, null);
							return;
						}
						catch (ObjectDisposedException)
						{
							return;
						}
						catch (SocketException e)
						{
							if (e.ErrorCode != 10054)
								return;
						}
					}
				}
			}
		}

		public override void Send(byte[] buffer, IPEndPoint endpoint)
		{
			try
			{
			   if (endpoint.Address != IPAddress.Any)
					client.Send(buffer, buffer.Length, endpoint);
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);// FIXME: Shoulnd't need a try/catch
			}
		}

		public override void Start()
		{
			try
			{
				client = new UdpClient(LocalEndPoint);
				client.BeginReceive(EndReceive, null);
				RaiseStatusChanged(ListenerStatus.Listening);
			}
			catch (SocketException)
			{
				RaiseStatusChanged(ListenerStatus.PortNotFree);
			}
			catch (ObjectDisposedException)
			{
				// Do Nothing
			}
		}

		public override void Stop()
		{
			try
			{
				client.Close();
			}
			catch
			{
				// FIXME: Not needed
			}
		}
	}
}
