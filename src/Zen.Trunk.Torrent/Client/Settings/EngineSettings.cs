namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Net;
	using Zen.Trunk.Torrent.Client.Encryption;

	/// <summary>
	/// Represents the Settings which need to be passed to the engine
	/// </summary>
	[Serializable]
	public class EngineSettings : ICloneable
	{
		#region Private Fields
		private const bool DefaultEnableHaveSupression = false;
		private const string DefaultSavePath = "";
		private const int DefaultMaxConnections = 150;
		private const int DefaultMaxDownloadSpeed = 0;
		private const int DefaultMaxUploadSpeed = 0;
		private const int DefaultMaxHalfOpenConnections = 5;
		private const EncryptionTypes DefaultAllowedEncryption = EncryptionTypes.All;
		private const int DefaultListenPort = 52138;
		#endregion

		#region Public Constructors
		public EngineSettings()
			: this(DefaultSavePath, DefaultListenPort, DefaultMaxConnections, DefaultMaxHalfOpenConnections,
				  DefaultMaxDownloadSpeed, DefaultMaxUploadSpeed, DefaultAllowedEncryption)
		{
		}

		public EngineSettings(string defaultSavePath, int listenPort)
			: this(defaultSavePath, listenPort, DefaultMaxConnections, DefaultMaxHalfOpenConnections, DefaultMaxDownloadSpeed, DefaultMaxUploadSpeed, DefaultAllowedEncryption)
		{

		}

		public EngineSettings(string defaultSavePath, int listenPort, int globalMaxConnections)
			: this(defaultSavePath, listenPort, globalMaxConnections, DefaultMaxHalfOpenConnections, DefaultMaxDownloadSpeed, DefaultMaxUploadSpeed, DefaultAllowedEncryption)
		{

		}

		public EngineSettings(string defaultSavePath, int listenPort, int globalMaxConnections, int globalHalfOpenConnections)
			: this(defaultSavePath, listenPort, globalMaxConnections, globalHalfOpenConnections, DefaultMaxDownloadSpeed, DefaultMaxUploadSpeed, DefaultAllowedEncryption)
		{

		}

		public EngineSettings(string defaultSavePath, int listenPort, int globalMaxConnections, int globalHalfOpenConnections, int globalMaxDownloadSpeed, int globalMaxUploadSpeed, EncryptionTypes allowedEncryption)
		{
			GlobalMaxConnections = globalMaxConnections;
			GlobalMaxDownloadSpeed = globalMaxDownloadSpeed;
			GlobalMaxUploadSpeed = globalMaxUploadSpeed;
			GlobalMaxHalfOpenConnections = globalHalfOpenConnections;
			ListenPort = listenPort;
			ListenAddress = IPAddress.Any;
			AllowedEncryption = allowedEncryption;
			SavePath = defaultSavePath;
			MaxOpenFiles = 15;
		}
		#endregion

		#region Public Properties
		public EncryptionTypes AllowedEncryption
		{
			get;
			set;
		}

		public bool HaveSupressionEnabled
		{
			get;
			set;
		}

		public int GlobalMaxConnections
		{
			get;
			set;
		}

		public int GlobalMaxHalfOpenConnections
		{
			get;
			set;
		}

		public int GlobalMaxDownloadSpeed
		{
			get;
			set;
		}

		public int GlobalMaxUploadSpeed
		{
			get;
			set;
		}

		public int ListenPort
		{
			get;
			set;
		}

		public IPAddress ListenAddress
		{
			get;
			set;
		}

		public int MaxOpenFiles
		{
			get;
			set;
		}

		public int MaxReadRate
		{
			get;
			set;
		}

		public int MaxWriteRate
		{
			get;
			set;
		}

		public IPEndPoint ReportedAddress
		{
			get;
			set;
		}

		public bool PreferEncryption
		{
			get;
			set;
		}

		public string SavePath
		{
			get;
			set;
		}
		#endregion

		#region Methods

		object ICloneable.Clone()
		{
			return Clone();
		}

		public EngineSettings Clone()
		{
			return (EngineSettings)MemberwiseClone();
		}

		public override bool Equals(object obj)
		{
			EngineSettings settings = obj as EngineSettings;
			return (settings == null) ? false : GlobalMaxConnections == settings.GlobalMaxConnections &&
												GlobalMaxDownloadSpeed == settings.GlobalMaxDownloadSpeed &&
												GlobalMaxHalfOpenConnections == settings.GlobalMaxHalfOpenConnections &&
												GlobalMaxUploadSpeed == settings.GlobalMaxUploadSpeed &&
												ListenPort == settings.ListenPort &&
												AllowedEncryption == settings.AllowedEncryption &&
												SavePath == settings.SavePath;
		}

		public override int GetHashCode()
		{
			return GlobalMaxConnections.GetHashCode() +
				   GlobalMaxDownloadSpeed.GetHashCode() +
				   GlobalMaxHalfOpenConnections.GetHashCode() +
				   GlobalMaxUploadSpeed.GetHashCode() +
				   ListenPort.GetHashCode() +
				   AllowedEncryption.GetHashCode() +
				   SavePath.GetHashCode();
		}

		#endregion Methods
	}
}