namespace Zen.Trunk.Torrent.Common
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using System.Text;

	public static class VersionInfo
	{
		/// <summary>
		/// Protocol string for version 1.0 of Bittorrent Protocol
		/// </summary>
		public static readonly string ProtocolStringV100 = "BitTorrent protocol";

		/// <summary>
		/// The current version of the client
		/// </summary>
		public static readonly string ClientVersion = CreateClientVersion ();

		public static readonly string DhtClientVersion = "ZD01";

		static string CreateClientVersion ()
		{
			AssemblyInformationalVersionAttribute version;
			Assembly assembly = Assembly.GetExecutingAssembly ();
			version = (AssemblyInformationalVersionAttribute) assembly.GetCustomAttributes (typeof (AssemblyInformationalVersionAttribute), false)[0];
			Version v = new Version(version.InformationalVersion);

				// 'ZD' for ZenTorrent then four digit version number
			return string.Format ("-ZD{0}{1}{2}{3}-",
								  Math.Max (v.Major, 0),
								  Math.Max (v.Minor, 0), 
								  Math.Max (v.Build, 0),
								  Math.Max (v.Revision, 0));
		}
	}
}
