namespace Zen.Trunk.Torrent.Client.Connections
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Net.Sockets;
	using System.Net;

	public static class ConnectionFactory
	{
		private static object locker = new object();
		private static Dictionary<string, Type> trackerTypes = new Dictionary<string, Type>();

		static ConnectionFactory()
		{
			RegisterTypeForProtocol("tcp", typeof(IPV4Connection));
			RegisterTypeForProtocol("ipv6", typeof(IPV6Connection));
			RegisterTypeForProtocol("http", typeof(HttpConnection));
		}

		public static void RegisterTypeForProtocol(string protocol, Type connectionType)
		{
			if (string.IsNullOrEmpty(protocol))
				throw new ArgumentException("cannot be null or empty", "protocol");
			if (connectionType == null)
				throw new ArgumentNullException("connectionType");

			lock (locker)
				trackerTypes.Add(protocol, connectionType);
		}

		public static IConnection Create(Uri connectionUri)
		{
			if (connectionUri == null)
				throw new ArgumentNullException("connectionUrl");

			Type type;
			lock (locker)
			{
				if (!trackerTypes.TryGetValue(connectionUri.Scheme, out type))
				{
					return null;
				}
			}

			try
			{
				return (IConnection)Activator.CreateInstance(type, connectionUri);
			}
			catch
			{
				return null;
			}
		}

		public static IConnection Create(Socket incomingSocket, out Uri connectionUri)
		{
			if (incomingSocket == null)
			{
				throw new ArgumentNullException("incomingSocket");
			}

			IPEndPoint endpoint = incomingSocket.RemoteEndPoint as IPEndPoint;
			if (endpoint == null)
			{
				throw new InvalidOperationException("Only IP sockets are supported.");
			}

			if (incomingSocket.AddressFamily == AddressFamily.InterNetwork)
			{
				connectionUri = new Uri("tcp://" + endpoint.Address.ToString() + ':' + endpoint.Port);
			}
			else if (incomingSocket.AddressFamily == AddressFamily.InterNetworkV6)
			{
				connectionUri = new Uri("ipv6://" + endpoint.Address.ToString() + ':' + endpoint.Port);
			}
			else
			{
				throw new InvalidOperationException("Unsupported socket address family detected.");
			}

			// Lookup the connection type in registry
			Type type;
			lock (locker)
			{
				if (!trackerTypes.TryGetValue(connectionUri.Scheme, out type))
				{
					throw new InvalidOperationException("Unsupported uri scheme encountered.");
				}
			}

			// Create inbound connection object
			try
			{
				return (IConnection)Activator.CreateInstance(type, incomingSocket, true);
			}
			catch(Exception ex)
			{
				throw new InvalidOperationException ("Failed to create connection object.", ex);
			}
		}
	}
}
